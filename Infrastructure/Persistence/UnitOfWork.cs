using Application.Abstractions.Common;
using Application.Abstractions.Repositories;
using Application.Abstractions.Repositories.Common;
using Domain.Common;
using Domain.Entities.Identity;
using Infrastructure.Persistence.DatabaseContext;
using Infrastructure.Persistence.Repositories;
using Infrastructure.Persistence.Repositories.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Ecom.Infrastructure.Persistence
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private IDbContextTransaction? _tx;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly ConcurrentDictionary<Type, object> _repos = new();
        private readonly IMediator _mediator;

        public UnitOfWork(ApplicationDbContext context, IMediator mediator)
        {
            _context = context;
            _mediator = mediator;
            _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            Products = new ProductRepository(_context);
            Orders = new OrderRepository(_context);
        }

        public IProductRepository Products { get; }
        public IOrderRepository Orders { get; }
        public IAuthRepository Auths => GetRepository<User>() as IAuthRepository ?? new AuthRepository(_context);

        public IRepository<T> GetRepository<T>() where T : class
            => (IRepository<T>)_repos.GetOrAdd(typeof(T), _ => Activator.CreateInstance(typeof(Repository<>).MakeGenericType(typeof(T)), _context)!);

        public async Task BeginTransactionAsync()
        {
            if (_tx != null) return;
            _tx = await _context.Database.BeginTransactionAsync();
        }

        public async Task<bool> CommitTransactionAsync()
        {
            try
            {
                if (_tx == null) { _tx = await _context.Database.BeginTransactionAsync(); }

                // gather domain events
                var domainEntities = _context.ChangeTracker
                    .Entries()
                    .Where(e => e.Entity is BaseEntity)
                    .Select(e => e.Entity as BaseEntity)
                    .Where(e => e != null)
                    .ToList();

                var domainEvents = domainEntities.SelectMany(e => e!.DomainEvents).ToList();

                await _context.SaveChangesAsync();

                await _tx.CommitAsync();

                // publish domain events after commit
                foreach (var evt in domainEvents)
                {
                    await _mediator.Publish(evt);
                }

                // clear domain events
                foreach (var e in domainEntities) e!.ClearDomainEvents();
                return true;
            }
            catch
            {
                if (_tx != null) await _tx.RollbackAsync();
                _context.ChangeTracker.Clear();
                return false;
            }
            finally
            {
                if (_tx != null) { await _tx.DisposeAsync(); _tx = null; }
            }
        }

        public async Task<bool> RollbackTransactionAsync()
        {
            if (_tx == null) { _context.ChangeTracker.Clear(); return false; }
            try
            {
                await _tx.RollbackAsync();
                await _tx.DisposeAsync();
                _tx = null;
                _context.ChangeTracker.Clear();
                return true;
            }
            catch { return false; }
        }

        public async Task AddIntegrationEventToOutboxAsync(object integrationEvent)
        {
            if (integrationEvent == null) throw new ArgumentNullException(nameof(integrationEvent));
            var typeName = integrationEvent.GetType().AssemblyQualifiedName ?? integrationEvent.GetType().FullName!;
            var payload = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType(), _jsonOptions);
            var outbox = new OutboxMessage { Type = typeName, Content = payload, OccurredOn = DateTimeOffset.UtcNow };
            await _context.OutboxMessages.AddAsync(outbox);
        }

        public async Task<int> SaveChangesAsync() => await _context.SaveChangesAsync();

        public void Dispose() { _tx?.Dispose(); _context.Dispose(); }
    }
}

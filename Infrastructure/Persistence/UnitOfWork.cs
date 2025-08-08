using Application.Abstractions;
using Application.Abstractions.Common;
using Domain.Common;
using Domain.Entities.Identity;
using Infrastructure.Persistence.DatabaseContext;
using Infrastructure.Persistence.Repositories;
using Infrastructure.Persistence.Repositories.Common;
using MassTransit;
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
        private readonly ConcurrentDictionary<Type, object> _repos = new();

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
            Products = new ProductRepository(_context);
            Orders = new OrderRepository(_context);
        }

        public IProductRepository Products { get; }
        public IOrderRepository Orders { get; }

        public IRepository<T> GetRepository<T>() where T : class
            => (IRepository<T>)_repos.GetOrAdd(typeof(T), _ => Activator.CreateInstance(typeof(Repository<>).MakeGenericType(typeof(T)), _context)!);

        public async Task BeginTransactionAsync()
        {
            if (_tx != null) return;
            _tx = await _context.Database.BeginTransactionAsync();
        }

        // Commit: Save changes, create outbox entries from domain events, commit transaction
        public async Task<bool> CommitTransactionAsync()
        {
            try
            {
                // Collect domain events BEFORE saving, because domain events may have entity Ids pre-generated
                var domainEntities = _context.ChangeTracker
                    .Entries()
                    .Where(e => e.Entity is BaseEntity)
                    .Select(e => e.Entity as BaseEntity)
                    .Where(e => e != null)
                    .ToList();

                var domainEvents = domainEntities.SelectMany(d => d!.DomainEvents).ToList();

                // Map each domain event to outbox message
                foreach (var evt in domainEvents)
                {
                    var typeName = evt.GetType().FullName!;
                    var payload = JsonSerializer.Serialize(evt, evt.GetType(), new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                    var outbox = new OutboxMessage
                    {
                        Type = typeName,
                        Content = payload,
                        OccurredOn = DateTimeOffset.UtcNow
                    };
                    await _context.OutboxMessages.AddAsync(outbox);
                }

                // Persist everything (entities + outbox) in same transaction
                await _context.SaveChangesAsync();

                if (_tx != null)
                    await _tx.CommitAsync();

                // clear domain events
                foreach (var ent in domainEntities) ent!.ClearDomainEvents();

                return true;
            }
            catch
            {
                if (_tx != null) await _tx.RollbackAsync();
                return false;
            }
            finally
            {
                if (_tx != null) { await _tx.DisposeAsync(); _tx = null; }
            }
        }

        public async Task<bool> RollbackTransactionAsync()
        {
            if (_tx == null) return false;
            try { await _tx.RollbackAsync(); return true; }
            catch { return false; }
            finally { await _tx.DisposeAsync(); _tx = null; }
        }

        public async Task<int> SaveChangesAsync() => await _context.SaveChangesAsync();

        public void Dispose() { _tx?.Dispose(); _context.Dispose(); }
    }
}

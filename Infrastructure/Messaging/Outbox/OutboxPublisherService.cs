using Domain.Entities;
using Infrastructure.Messaging.Abstractions;
using Infrastructure.Messaging.Configuration;
using Infrastructure.Persistence.DatabaseContext;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks.Dataflow;

namespace Infrastructure.Messaging.Outbox
{
    /// <summary>
    /// Enhanced outbox publisher with circuit breaker, idempotency, and better error handling.
    /// Processes outbox messages in batches with parallel execution and automatic retry.
    /// </summary>
    public class OutboxPublisherService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OutboxPublisherService> _logger;
        private readonly OutboxSettings _settings;
        private readonly IMessageTypeRegistry _typeRegistry;
        private readonly CircuitBreaker _circuitBreaker;
        private int _consecutiveFailures = 0;

        public OutboxPublisherService(
            IServiceProvider serviceProvider,
            ILogger<OutboxPublisherService> logger,
            IOptions<OutboxSettings> settings,
            IMessageTypeRegistry typeRegistry)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _settings = settings.Value;
            _typeRegistry = typeRegistry;
            _circuitBreaker = new CircuitBreaker(
                _settings.CircuitBreakerFailureThreshold,
                TimeSpan.FromSeconds(_settings.CircuitBreakerResetTimeoutSeconds),
                logger);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "OutboxPublisher started | BatchSize={BatchSize}, Parallelism={Parallelism}, PollInterval={PollInterval}s",
                _settings.BatchSize,
                _settings.MaxDegreeOfParallelism,
                _settings.PollIntervalSeconds);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (_settings.EnableCircuitBreaker && !_circuitBreaker.IsOpen)
                    {
                        await ProcessBatchAsync(stoppingToken);
                        _consecutiveFailures = 0;
                    }
                    else if (_circuitBreaker.IsOpen)
                    {
                        _logger.LogWarning("Circuit breaker is OPEN, skipping batch processing");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Critical error in outbox processing loop");
                    _consecutiveFailures++;

                    if (_settings.EnableCircuitBreaker && _consecutiveFailures >= _settings.CircuitBreakerFailureThreshold)
                    {
                        _circuitBreaker.Trip();
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(_settings.PollIntervalSeconds), stoppingToken);
            }

            _logger.LogInformation("OutboxPublisher stopped");
        }

        private async Task ProcessBatchAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var publisher = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

            var messages = await FetchOutboxMessagesAsync(dbContext, cancellationToken);

            if (messages.Count == 0)
            {
                return;
            }

            _logger.LogInformation("Processing {Count} outbox messages", messages.Count);

            var sw = Stopwatch.StartNew();
            var processedCount = 0;
            var failedCount = 0;

            var options = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = _settings.MaxDegreeOfParallelism,
                CancellationToken = cancellationToken
            };

            var processingBlock = new ActionBlock<OutboxMessage>(async message =>
            {
                var success = await ProcessMessageAsync(message, dbContext, publisher, cancellationToken);
                
                if (success)
                {
                    Interlocked.Increment(ref processedCount);
                }
                else
                {
                    Interlocked.Increment(ref failedCount);
                }
            }, options);

            foreach (var message in messages)
            {
                processingBlock.Post(message);
            }

            processingBlock.Complete();
            await processingBlock.Completion;

            sw.Stop();

            _logger.LogInformation(
                "Batch completed | Processed={Processed}, Failed={Failed}, Duration={Duration}ms",
                processedCount,
                failedCount,
                sw.ElapsedMilliseconds);
        }

        private async Task<List<OutboxMessage>> FetchOutboxMessagesAsync(
            ApplicationDbContext dbContext,
            CancellationToken cancellationToken)
        {
            return await dbContext.OutboxMessages
                .Where(m => !m.Processed && m.RetryCount < _settings.MaxRetryCount)
                .OrderBy(m => m.OccurredOn)
                .Take(_settings.BatchSize)
                .ToListAsync(cancellationToken);
        }

        private async Task<bool> ProcessMessageAsync(
            OutboxMessage message,
            ApplicationDbContext dbContext,
            IPublishEndpoint publisher,
            CancellationToken cancellationToken)
        {
            try
            {
                // Resolve type from registry
                var eventType = _typeRegistry.ResolveType(message.Type);
                
                if (eventType == null)
                {
                    _logger.LogWarning(
                        "Message type not found in registry | MessageId={MessageId}, Type={Type}",
                        message.Id,
                        message.Type);
                    
                    await MarkAsFailedAsync(message, dbContext, "Type not found", cancellationToken);
                    return false;
                }

                // Deserialize integration event
                var integrationEvent = JsonSerializer.Deserialize(
                    message.Content,
                    eventType,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (integrationEvent == null)
                {
                    _logger.LogWarning(
                        "Failed to deserialize message | MessageId={MessageId}",
                        message.Id);
                    
                    await MarkAsFailedAsync(message, dbContext, "Deserialization failed", cancellationToken);
                    return false;
                }

                // Publish to RabbitMQ
                await publisher.Publish(integrationEvent, eventType, cancellationToken);

                // Mark as processed
                await MarkAsProcessedAsync(message, dbContext, cancellationToken);

                _logger.LogDebug(
                    "Message published successfully | MessageId={MessageId}, Type={Type}",
                    message.Id,
                    eventType.Name);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to process message | MessageId={MessageId}, RetryCount={RetryCount}",
                    message.Id,
                    message.RetryCount);

                await IncrementRetryCountAsync(message, dbContext, ex.Message, cancellationToken);
                return false;
            }
        }

        private async Task MarkAsProcessedAsync(
            OutboxMessage message,
            ApplicationDbContext dbContext,
            CancellationToken cancellationToken)
        {
            message.Processed = true;
            message.ProcessedOn = DateTimeOffset.UtcNow;
            message.Error = null;

            dbContext.OutboxMessages.Update(message);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task MarkAsFailedAsync(
            OutboxMessage message,
            ApplicationDbContext dbContext,
            string error,
            CancellationToken cancellationToken)
        {
            message.Error = error;
            message.RetryCount = _settings.MaxRetryCount; // Max out retries to prevent reprocessing

            dbContext.OutboxMessages.Update(message);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task IncrementRetryCountAsync(
            OutboxMessage message,
            ApplicationDbContext dbContext,
            string error,
            CancellationToken cancellationToken)
        {
            message.RetryCount++;
            message.Error = error;

            dbContext.OutboxMessages.Update(message);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Simple circuit breaker implementation.
    /// </summary>
    internal class CircuitBreaker
    {
        private readonly int _failureThreshold;
        private readonly TimeSpan _resetTimeout;
        private readonly ILogger _logger;
        private int _failureCount = 0;
        private DateTimeOffset _lastFailureTime = DateTimeOffset.MinValue;
        private bool _isOpen = false;

        public CircuitBreaker(int failureThreshold, TimeSpan resetTimeout, ILogger logger)
        {
            _failureThreshold = failureThreshold;
            _resetTimeout = resetTimeout;
            _logger = logger;
        }

        public bool IsOpen
        {
            get
            {
                if (_isOpen && DateTimeOffset.UtcNow - _lastFailureTime > _resetTimeout)
                {
                    _logger.LogInformation("Circuit breaker RESET");
                    Reset();
                }
                return _isOpen;
            }
        }

        public void Trip()
        {
            _isOpen = true;
            _lastFailureTime = DateTimeOffset.UtcNow;
            _logger.LogWarning(
                "Circuit breaker TRIPPED | FailureCount={FailureCount}, ResetIn={ResetIn}s",
                _failureCount,
                _resetTimeout.TotalSeconds);
        }

        public void Reset()
        {
            _isOpen = false;
            _failureCount = 0;
        }
    }
}

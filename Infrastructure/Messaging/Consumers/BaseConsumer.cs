using MassTransit;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Infrastructure.Messaging.Consumers
{
    /// <summary>
    /// Enhanced base consumer with structured logging, metrics, and error handling.
    /// Provides template method pattern for consistent message processing.
    /// </summary>
    /// <typeparam name="TMessage">The integration event type to consume.</typeparam>
    public abstract class BaseConsumer<TMessage> : IConsumer<TMessage> where TMessage : class
    {
        protected readonly ILogger Logger;

        protected BaseConsumer(ILogger logger)
        {
            Logger = logger;
        }

        /// <summary>
        /// Main consume method - orchestrates the message processing flow.
        /// Handles logging, metrics, error handling, and retry logic.
        /// </summary>
        public async Task Consume(ConsumeContext<TMessage> context)
        {
            var sw = Stopwatch.StartNew();
            var messageType = typeof(TMessage).Name;
            var messageId = context.MessageId ?? Guid.NewGuid();

            using (Logger.BeginScope(new Dictionary<string, object>
            {
                ["MessageId"] = messageId,
                ["MessageType"] = messageType,
                ["CorrelationId"] = context.CorrelationId ?? Guid.NewGuid()
            }))
            {
                try
                {
                    Logger.LogInformation(
                        "Processing message started | MessageType={MessageType}, MessageId={MessageId}",
                        messageType,
                        messageId);

                    // Validate message
                    var validationResult = await ValidateMessageAsync(context);
                    if (!validationResult.IsValid)
                    {
                        Logger.LogWarning(
                            "Message validation failed | Errors={Errors}",
                            string.Join(", ", validationResult.Errors));

                        await OnValidationFailedAsync(context, validationResult);
                        return;
                    }

                    // Process message
                    await ProcessMessageAsync(context);

                    sw.Stop();

                    Logger.LogInformation(
                        "Processing message completed | MessageType={MessageType}, MessageId={MessageId}, Duration={Duration}ms",
                        messageType,
                        messageId,
                        sw.ElapsedMilliseconds);

                    // Post-processing hook
                    await OnProcessingCompletedAsync(context, sw.Elapsed);
                }
                catch (Exception ex)
                {
                    sw.Stop();

                    Logger.LogError(
                        ex,
                        "Processing message failed | MessageType={MessageType}, MessageId={MessageId}, Duration={Duration}ms, Error={Error}",
                        messageType,
                        messageId,
                        sw.ElapsedMilliseconds,
                        ex.Message);

                    // Error handling hook
                    await OnProcessingFailedAsync(context, ex);

                    // Re-throw for MassTransit retry mechanism
                    throw;
                }
            }
        }

        /// <summary>
        /// Override this method to implement message processing logic.
        /// </summary>
        protected abstract Task ProcessMessageAsync(ConsumeContext<TMessage> context);

        /// <summary>
        /// Override this method to implement custom validation logic.
        /// Default implementation returns valid result.
        /// </summary>
        protected virtual Task<MessageValidationResult> ValidateMessageAsync(ConsumeContext<TMessage> context)
        {
            return Task.FromResult(MessageValidationResult.Valid());
        }

        /// <summary>
        /// Hook called when validation fails.
        /// Override to implement custom behavior (e.g., send to dead letter queue).
        /// </summary>
        protected virtual Task OnValidationFailedAsync(ConsumeContext<TMessage> context, MessageValidationResult validationResult)
        {
            // Default: do nothing, message will be acknowledged
            return Task.CompletedTask;
        }

        /// <summary>
        /// Hook called after successful processing.
        /// Override to implement custom behavior (e.g., metrics, notifications).
        /// </summary>
        protected virtual Task OnProcessingCompletedAsync(ConsumeContext<TMessage> context, TimeSpan duration)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Hook called when processing fails.
        /// Override to implement custom error handling (e.g., alerting, logging).
        /// </summary>
        protected virtual Task OnProcessingFailedAsync(ConsumeContext<TMessage> context, Exception exception)
        {
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Validation result for message validation.
    /// </summary>
    public class MessageValidationResult
    {
        public bool IsValid { get; init; }
        public List<string> Errors { get; init; } = new();

        public static MessageValidationResult Valid() => new() { IsValid = true };

        public static MessageValidationResult Invalid(params string[] errors) =>
            new() { IsValid = false, Errors = errors.ToList() };
    }
}

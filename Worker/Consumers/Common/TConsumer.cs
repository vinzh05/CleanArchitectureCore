using MassTransit;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Infrastructure.Consumers.Common
{
    /// <summary>
    /// Base consumer class với logging, metrics, error handling tối ưu.
    /// Tự động track performance và xử lý lỗi cho tất cả derived consumers.
    /// </summary>
    public abstract class TConsumer<T> : IConsumer<T> where T : class
    {
        protected readonly ILogger Logger;

        protected TConsumer(ILogger logger)
        {
            Logger = logger;
        }

        /// <summary>
        /// Template method pattern: Orchestrate consume flow với logging, metrics và error handling.
        /// Derived classes chỉ cần implement ProcessMessageAsync.
        /// </summary>
        public async Task Consume(ConsumeContext<T> context)
        {
            var sw = Stopwatch.StartNew();
            var eventType = typeof(T).Name;
            
            try
            {
                Logger.LogInformation("[{EventType}] Processing started | MessageId: {MessageId}", 
                    eventType, context.MessageId);

                await ProcessMessageAsync(context);

                sw.Stop();
                Logger.LogInformation("[{EventType}] Processing completed | Duration: {Duration}ms | MessageId: {MessageId}", 
                    eventType, sw.ElapsedMilliseconds, context.MessageId);
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logger.LogError(ex, "[{EventType}] Processing failed | Duration: {Duration}ms | MessageId: {MessageId} | Error: {Error}", 
                    eventType, sw.ElapsedMilliseconds, context.MessageId, ex.Message);
                throw; // Re-throw để MassTransit retry
            }
        }

        /// <summary>
        /// Override method này trong derived class để implement business logic.
        /// </summary>
        protected abstract Task ProcessMessageAsync(ConsumeContext<T> context);
    }
}

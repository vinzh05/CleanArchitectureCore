using Infrastructure.Messaging.Consumers;
using Infrastructure.Search;
using MassTransit;
using Microsoft.Extensions.Logging;
using Shared.IntegrationEvents.Contracts.Product;

namespace Infrastructure.Consumers.Product
{
    /// <summary>
    /// Consumer for ProductCreatedIntegrationEvent.
    /// Indexes product into ElasticSearch for high-performance search.
    /// </summary>
    public class ProductCreatedConsumer : BaseConsumer<ProductCreatedIntegrationEvent>
    {
        private readonly ElasticService _elastic;

        public ProductCreatedConsumer(
            ILogger<ProductCreatedConsumer> logger,
            ElasticService elastic) 
            : base(logger)
        {
            _elastic = elastic;
        }

        protected override async Task ProcessMessageAsync(ConsumeContext<ProductCreatedIntegrationEvent> context)
        {
            var message = context.Message;
            
            Logger.LogInformation(
                "Processing product | ProductId={ProductId}, Name={Name}, Price={Price:C}",
                message.ProductId,
                message.Name,
                message.Price);

            // Index into Elasticsearch for high-performance search
            await _elastic.IndexAsync(new 
            { 
                id = message.ProductId, 
                name = message.Name, 
                price = message.Price,
                indexed_at = DateTimeOffset.UtcNow
            });

            Logger.LogInformation(
                "Product indexed successfully | ProductId={ProductId}",
                message.ProductId);
        }

        protected override Task<MessageValidationResult> ValidateMessageAsync(
            ConsumeContext<ProductCreatedIntegrationEvent> context)
        {
            var message = context.Message;
            var errors = new List<string>();

            if (message.ProductId == Guid.Empty)
            {
                errors.Add("ProductId is required");
            }

            if (string.IsNullOrWhiteSpace(message.Name))
            {
                errors.Add("Product name is required");
            }

            if (message.Price < 0)
            {
                errors.Add("Product price must be non-negative");
            }

            var result = errors.Any()
                ? MessageValidationResult.Invalid(errors.ToArray())
                : MessageValidationResult.Valid();

            return Task.FromResult(result);
        }

        protected override async Task OnProcessingFailedAsync(
            ConsumeContext<ProductCreatedIntegrationEvent> context,
            Exception exception)
        {
            // Custom error handling - could send alert, log to special system, etc.
            Logger.LogError(
                "Failed to index product, may need manual intervention | ProductId={ProductId}",
                context.Message.ProductId);

            await base.OnProcessingFailedAsync(context, exception);
        }
    }
}

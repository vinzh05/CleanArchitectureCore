using Infrastructure.Messaging.Consumers;
using MassTransit;
using Microsoft.Extensions.Logging;
using Shared.IntegrationEvents.Contracts.Order;

namespace Infrastructure.Consumers.Order
{
    /// <summary>
    /// Consumer for OrderCreatedIntegrationEvent.
    /// Processes new orders, syncs with external systems, and updates inventory.
    /// </summary>
    public class OrderCreatedConsumer : BaseConsumer<OrderCreatedIntegrationEvent>
    {
        public OrderCreatedConsumer(ILogger<OrderCreatedConsumer> logger) : base(logger) { }

        protected override async Task ProcessMessageAsync(ConsumeContext<OrderCreatedIntegrationEvent> context)
        {
            var message = context.Message;
            
            Logger.LogInformation(
                "Processing order | OrderId={OrderId}, OrderNumber={OrderNumber}, Total={Total:C}, Items={ItemCount}",
                message.OrderId,
                message.OrderNumber,
                message.TotalPrice,
                message.Items.Count);

            // TODO: Implement business logic
            // - Sync with external systems (ERP, WMS, etc.)
            // - Update inventory
            // - Trigger notifications
            // - Update analytics/reporting
            // - Process payment
            // - Send confirmation email

            await Task.CompletedTask;
        }

        protected override Task<MessageValidationResult> ValidateMessageAsync(
            ConsumeContext<OrderCreatedIntegrationEvent> context)
        {
            var message = context.Message;
            var errors = new List<string>();

            if (message.OrderId == Guid.Empty)
            {
                errors.Add("OrderId is required");
            }

            if (string.IsNullOrWhiteSpace(message.OrderNumber))
            {
                errors.Add("Order number is required");
            }

            if (message.TotalPrice < 0)
            {
                errors.Add("Total price must be non-negative");
            }

            if (message.Items == null || !message.Items.Any())
            {
                errors.Add("Order must have at least one item");
            }

            var result = errors.Any()
                ? MessageValidationResult.Invalid(errors.ToArray())
                : MessageValidationResult.Valid();

            return Task.FromResult(result);
        }
    }
}

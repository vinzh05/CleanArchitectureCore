using Infrastructure.Consumers.Common;
using MassTransit;
using Shared.IntegrationEvents.Contracts.Order;
using System.Threading.Tasks;

namespace Infrastructure.Consumers.Order
{
    /// <summary>
    /// Consumer cho OrderCreatedIntegrationEvent từ RabbitMQ qua MassTransit.
    /// Xử lý event khi đơn hàng được tạo, đồng bộ dữ liệu.
    /// </summary>
    public class OrderCreatedConsumer : IConsumer<OrderCreatedIntegrationEvent>
    {
        private readonly ILogger<OrderCreatedConsumer> _logger;

        public OrderCreatedConsumer(ILogger<OrderCreatedConsumer> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Xử lý message OrderCreatedIntegrationEvent từ RabbitMQ.
        /// Log và xử lý đồng bộ nếu cần.
        /// </summary>
        public async Task Consume(ConsumeContext<OrderCreatedIntegrationEvent> context)
        {
            var msg = context.Message;
            _logger.LogInformation("Processing OrderCreatedIntegrationEvent for Order ID: {Id}, OrderNumber: {Number}, Items: {ItemCount}",
                msg.OrderId, msg.OrderNumber, msg.Items.Count);

            try
            {
                // Logic xử lý (e.g., sync với external system, update inventory)
                foreach (var item in msg.Items)
                {
                    _logger.LogInformation("Item: Product {ProductId}, Quantity {Quantity}, Price {Price}",
                        item.ProductId, item.Quantity, item.Price);
                }

                _logger.LogInformation("Order {Id} processed successfully", msg.OrderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing OrderCreatedIntegrationEvent for Order ID: {Id}", msg.OrderId);
                throw;
            }
        }
    }
}

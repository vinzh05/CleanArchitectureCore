using Application.Abstractions.Common;
using Domain.Events.Order;
using MediatR;
using Shared.IntegrationEvents.Contracts.Order;
using System.Threading;
using System.Threading.Tasks;

namespace Application.EventHandlers
{
    /// <summary>
    /// Handler xử lý sự kiện OrderCreatedDomainEvent.
    /// Thêm integration event vào Outbox để publish qua RabbitMQ và gửi notification.
    /// </summary>
    public class OrderCreatedDomainEventHandler : INotificationHandler<OrderCreatedDomainEvent>
    {
        private readonly IUnitOfWork _uow;

        /// <summary>
        /// Khởi tạo handler với các dependency cần thiết.
        /// </summary>
        public OrderCreatedDomainEventHandler(IUnitOfWork uow)
        {
            _uow = uow;
        }

        /// <summary>
        /// Xử lý sự kiện OrderCreatedDomainEvent.
        /// Tạo và thêm OrderCreatedIntegrationEvent vào Outbox để publish qua RabbitMQ.
        /// Gửi notification real-time về việc tạo đơn hàng.
        /// </summary>
        public async Task Handle(OrderCreatedDomainEvent notification, CancellationToken cancellationToken)
        {
            var integrationEvent = new OrderCreatedIntegrationEvent
            {
                OrderId = notification.OrderId,
                OrderNumber = notification.OrderNumber,
                TotalPrice = notification.Total,
                Items = notification.Items.Select(i => new OrderItemIntegration { ProductId = i.ProductId, Quantity = i.Quantity, Price = i.Price }).ToList()
            };

            await _uow.AddIntegrationEventToOutboxAsync(integrationEvent);
            // Gửi notification nếu cần (e.g., SignalR)
        }
    }
}

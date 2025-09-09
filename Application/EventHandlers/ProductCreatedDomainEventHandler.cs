using Application.Abstractions.Common;
using Application.Abstractions.Services;
using Domain.Events.Product;
using MediatR;
using Shared.IntegrationEvents.Contracts.Product;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.EventHandlers
{
    /// <summary>
    /// Handler xử lý sự kiện ProductCreatedDomainEvent.
    /// Thêm integration event vào Outbox để publish qua RabbitMQ và gửi notification real-time.
    /// </summary>
    public class ProductCreatedDomainEventHandler : INotificationHandler<ProductCreatedDomainEvent>
    {
        private readonly IUnitOfWork _uow;
        private readonly INotificationService _notificationService;

        /// <summary>
        /// Khởi tạo handler với các dependency cần thiết.
        /// </summary>
        /// <param name="uow">Unit of Work để thêm integration event vào Outbox.</param>
        /// <param name="notificationService">Dịch vụ gửi notification real-time.</param>
        public ProductCreatedDomainEventHandler(IUnitOfWork uow, INotificationService notificationService)
        {
            _uow = uow;
            _notificationService = notificationService;
        }

        /// <summary>
        /// Xử lý sự kiện ProductCreatedDomainEvent.
        /// Tạo và thêm ProductCreatedIntegrationEvent vào Outbox để publish qua RabbitMQ.
        /// Gửi notification real-time về việc tạo sản phẩm.
        /// </summary>
        /// <param name="notification">Sự kiện domain chứa thông tin sản phẩm.</param>
        /// <param name="cancellationToken">Token hủy bỏ.</param>
        public async Task Handle(ProductCreatedDomainEvent notification, CancellationToken cancellationToken)
        {
            var integration = new ProductCreatedIntegrationEvent(notification.ProductId, notification.Name, notification.Price);
            await _uow.AddIntegrationEventToOutboxAsync(integration);

            // Không cần publish integration event
            // await _mediator.Publish(new ProductCreatedIntegrationEvent(...)); // Loại bỏ dòng này

            // Send real-time notification
            await _notificationService.SendNotificationAsync($"Product {notification.Name} created with price {notification.Price:C}");
        }
    }
}

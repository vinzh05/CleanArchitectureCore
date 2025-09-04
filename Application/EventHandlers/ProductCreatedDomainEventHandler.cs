using Application.Abstractions.Common;
using Application.Abstractions.Services;
using Domain.Events.Product;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.EventHandlers
{
    public class ProductCreatedDomainEventHandler : INotificationHandler<ProductCreatedDomainEvent>
    {
        private readonly IUnitOfWork _uow;
        private readonly INotificationService _notificationService;

        public ProductCreatedDomainEventHandler(IUnitOfWork uow, INotificationService notificationService)
        {
            _uow = uow;
            _notificationService = notificationService;
        }

        public async Task Handle(ProductCreatedDomainEvent notification, CancellationToken cancellationToken)
        {
            var integration = new ProductCreatedIntegrationEvent(notification.ProductId, notification.Name, notification.Price);
            await _uow.AddIntegrationEventToOutboxAsync(integration);

            // Send real-time notification
            await _notificationService.SendNotificationAsync($"Product {notification.Name} created with price {notification.Price:C}");
        }
    }
}

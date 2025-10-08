using Application.Abstractions.Common;
using Domain.Events.Hikvision;
using MediatR;
using Shared.IntegrationEvents.Contracts.Hikvision;

namespace Application.EventHandlers.Hikvision
{
    /// <summary>
    /// Handler xử lý DeviceStatusChangedDomainEvent và thêm vào Outbox để publish qua RabbitMQ.
    /// </summary>
    public class DeviceStatusChangedDomainEventHandler : INotificationHandler<DeviceStatusChangedDomainEvent>
    {
        private readonly IUnitOfWork _uow;

        public DeviceStatusChangedDomainEventHandler(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task Handle(DeviceStatusChangedDomainEvent notification, CancellationToken cancellationToken)
        {
            var integrationEvent = new DeviceStatusChangedIntegrationEvent
            {
                DeviceId = notification.DeviceId,
                DeviceName = notification.DeviceName,
                PreviousStatus = notification.PreviousStatus,
                CurrentStatus = notification.CurrentStatus,
                StatusChangedAt = notification.StatusChangedAt,
                IpAddress = notification.IpAddress,
                Reason = notification.Reason
            };

            await _uow.AddIntegrationEventToOutboxAsync(integrationEvent);
        }
    }
}

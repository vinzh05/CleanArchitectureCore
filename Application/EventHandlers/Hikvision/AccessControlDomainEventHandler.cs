using Application.Abstractions.Common;
using Domain.Events.Hikvision;
using MediatR;
using Shared.IntegrationEvents.Contracts.Hikvision;

namespace Application.EventHandlers.Hikvision
{
    /// <summary>
    /// Handler xử lý AccessControlDomainEvent và thêm vào Outbox để publish qua RabbitMQ.
    /// </summary>
    public class AccessControlDomainEventHandler : INotificationHandler<AccessControlDomainEvent>
    {
        private readonly IUnitOfWork _uow;

        public AccessControlDomainEventHandler(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task Handle(AccessControlDomainEvent notification, CancellationToken cancellationToken)
        {
            var integrationEvent = new AccessControlIntegrationEvent
            {
                DeviceId = notification.DeviceId,
                PersonId = notification.PersonId,
                DoorId = notification.DoorId,
                AccessTime = notification.AccessTime,
                AccessGranted = notification.AccessGranted,
                AccessType = notification.AccessType,
                CardNo = notification.CardNo
            };

            await _uow.AddIntegrationEventToOutboxAsync(integrationEvent);
        }
    }
}

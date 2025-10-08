using Application.Abstractions.Common;
using Domain.Events.Hikvision;
using MediatR;
using Shared.IntegrationEvents.Contracts.Hikvision;

namespace Application.EventHandlers.Hikvision
{
    /// <summary>
    /// Handler xử lý DoorOpenedDomainEvent và thêm vào Outbox để publish qua RabbitMQ.
    /// </summary>
    public class DoorOpenedDomainEventHandler : INotificationHandler<DoorOpenedDomainEvent>
    {
        private readonly IUnitOfWork _uow;

        public DoorOpenedDomainEventHandler(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task Handle(DoorOpenedDomainEvent notification, CancellationToken cancellationToken)
        {
            var integrationEvent = new DoorOpenedIntegrationEvent
            {
                DoorId = notification.DoorId,
                DoorName = notification.DoorName,
                DeviceId = notification.DeviceId,
                OpenedAt = notification.OpenedAt,
                OpenMethod = notification.OpenMethod,
                PersonId = notification.PersonId,
                CardNo = notification.CardNo,
                DurationSeconds = notification.DurationSeconds
            };

            await _uow.AddIntegrationEventToOutboxAsync(integrationEvent);
        }
    }
}

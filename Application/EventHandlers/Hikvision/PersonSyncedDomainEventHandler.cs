using Application.Abstractions.Common;
using Domain.Events.Hikvision;
using MediatR;
using Shared.IntegrationEvents.Contracts.Hikvision;

namespace Application.EventHandlers.Hikvision
{
    /// <summary>
    /// Handler xử lý PersonSyncedDomainEvent và thêm vào Outbox để publish qua RabbitMQ.
    /// </summary>
    public class PersonSyncedDomainEventHandler : INotificationHandler<PersonSyncedDomainEvent>
    {
        private readonly IUnitOfWork _uow;

        public PersonSyncedDomainEventHandler(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task Handle(PersonSyncedDomainEvent notification, CancellationToken cancellationToken)
        {
            var integrationEvent = new PersonSyncedIntegrationEvent
            {
                PersonId = notification.PersonId,
                PersonName = notification.PersonName,
                CardNo = notification.CardNo,
                Department = notification.Department,
                SyncedAt = notification.SyncedAt,
                SyncAction = notification.SyncAction,
                AccessDoorIds = notification.AccessDoorIds
            };

            await _uow.AddIntegrationEventToOutboxAsync(integrationEvent);
        }
    }
}

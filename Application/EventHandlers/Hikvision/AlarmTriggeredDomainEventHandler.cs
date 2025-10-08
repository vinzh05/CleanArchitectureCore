using Application.Abstractions.Common;
using Domain.Events.Hikvision;
using MediatR;
using Shared.IntegrationEvents.Contracts.Hikvision;

namespace Application.EventHandlers.Hikvision
{
    /// <summary>
    /// Handler xử lý AlarmTriggeredDomainEvent và thêm vào Outbox để publish qua RabbitMQ.
    /// </summary>
    public class AlarmTriggeredDomainEventHandler : INotificationHandler<AlarmTriggeredDomainEvent>
    {
        private readonly IUnitOfWork _uow;

        public AlarmTriggeredDomainEventHandler(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task Handle(AlarmTriggeredDomainEvent notification, CancellationToken cancellationToken)
        {
            var integrationEvent = new AlarmTriggeredIntegrationEvent
            {
                AlarmId = notification.AlarmId,
                DeviceId = notification.DeviceId,
                AlarmType = notification.AlarmType,
                AlarmLevel = notification.AlarmLevel,
                AlarmTime = notification.AlarmTime,
                Description = notification.Description,
                Location = notification.Location,
                MetaData = notification.MetaData
            };

            await _uow.AddIntegrationEventToOutboxAsync(integrationEvent);
        }
    }
}

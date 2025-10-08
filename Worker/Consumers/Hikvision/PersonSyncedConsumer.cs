using Infrastructure.Consumers.Common;
using MassTransit;
using Microsoft.Extensions.Logging;
using Shared.IntegrationEvents.Contracts.Hikvision;

namespace Infrastructure.Consumers.Hikvision
{
    /// <summary>
    /// Consumer tối ưu cho PersonSyncedIntegrationEvent từ RabbitMQ.
    /// Xử lý đồng bộ thông tin người dùng, update access rights, sync với external systems.
    /// </summary>
    public class PersonSyncedConsumer : TConsumer<PersonSyncedIntegrationEvent>
    {
        public PersonSyncedConsumer(ILogger<PersonSyncedConsumer> logger) : base(logger) { }

        protected override async Task ProcessMessageAsync(ConsumeContext<PersonSyncedIntegrationEvent> context)
        {
            var msg = context.Message;
            
            Logger.LogInformation(
                "Person Synced | PersonId: {PersonId}, Name: {Name}, Action: {Action}, Department: {Dept}, Doors: {DoorCount}",
                msg.PersonId, msg.PersonName, msg.SyncAction, msg.Department, msg.AccessDoorIds.Count);

            // Business logic: Update access rights, sync với HR system, update dashboards
            await Task.CompletedTask;
        }
    }
}

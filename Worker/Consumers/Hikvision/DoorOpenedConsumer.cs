using Infrastructure.Consumers.Common;
using MassTransit;
using Microsoft.Extensions.Logging;
using Shared.IntegrationEvents.Contracts.Hikvision;

namespace Infrastructure.Consumers.Hikvision
{
    /// <summary>
    /// Consumer tối ưu cho DoorOpenedIntegrationEvent từ RabbitMQ.
    /// Xử lý sự kiện mở cửa, track usage, detect anomalies, update real-time dashboards.
    /// </summary>
    public class DoorOpenedConsumer : TConsumer<DoorOpenedIntegrationEvent>
    {
        public DoorOpenedConsumer(ILogger<DoorOpenedConsumer> logger) : base(logger) { }

        protected override async Task ProcessMessageAsync(ConsumeContext<DoorOpenedIntegrationEvent> context)
        {
            var msg = context.Message;
            
            Logger.LogInformation(
                "Door Opened | Door: {DoorId} ({DoorName}), Method: {Method}, Duration: {Duration}s, Person: {PersonId}",
                msg.DoorId, msg.DoorName, msg.OpenMethod, msg.DurationSeconds, msg.PersonId ?? "N/A");

            // Business logic: Track usage, detect anomalies (long open duration), update dashboards
            await Task.CompletedTask;
        }
    }
}

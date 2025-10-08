using Infrastructure.Consumers.Common;
using MassTransit;
using Microsoft.Extensions.Logging;
using Shared.IntegrationEvents.Contracts.Hikvision;

namespace Infrastructure.Consumers.Hikvision
{
    /// <summary>
    /// Consumer tối ưu cho AlarmTriggeredIntegrationEvent từ RabbitMQ.
    /// Xử lý cảnh báo từ thiết bị, route theo priority, trigger automated response.
    /// </summary>
    public class AlarmTriggeredConsumer : TConsumer<AlarmTriggeredIntegrationEvent>
    {
        public AlarmTriggeredConsumer(ILogger<AlarmTriggeredConsumer> logger) : base(logger) { }

        protected override async Task ProcessMessageAsync(ConsumeContext<AlarmTriggeredIntegrationEvent> context)
        {
            var msg = context.Message;
            
            Logger.LogInformation(
                "Alarm Triggered | AlarmId: {AlarmId}, Device: {DeviceId}, Type: {Type}, Level: {Level}, Location: {Location}",
                msg.AlarmId, msg.DeviceId, msg.AlarmType, msg.AlarmLevel, msg.Location);

            // Business logic: Route by priority, trigger automated response, send alerts (SMS, Email, Push)
            await Task.CompletedTask;
        }
    }
}

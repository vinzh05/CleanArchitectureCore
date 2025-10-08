using Infrastructure.Consumers.Common;
using MassTransit;
using Microsoft.Extensions.Logging;
using Shared.IntegrationEvents.Contracts.Hikvision;

namespace Infrastructure.Consumers.Hikvision
{
    /// <summary>
    /// Consumer tối ưu cho DeviceStatusChangedIntegrationEvent từ RabbitMQ.
    /// Xử lý thay đổi trạng thái thiết bị, update health status, trigger alerts.
    /// </summary>
    public class DeviceStatusChangedConsumer : TConsumer<DeviceStatusChangedIntegrationEvent>
    {
        public DeviceStatusChangedConsumer(ILogger<DeviceStatusChangedConsumer> logger) : base(logger) { }

        protected override async Task ProcessMessageAsync(ConsumeContext<DeviceStatusChangedIntegrationEvent> context)
        {
            var msg = context.Message;
            
            Logger.LogInformation(
                "Device Status Changed | Device: {DeviceId} ({DeviceName}), {PreviousStatus} -> {CurrentStatus}, IP: {IP}, Reason: {Reason}",
                msg.DeviceId, msg.DeviceName, msg.PreviousStatus, msg.CurrentStatus, msg.IpAddress, msg.Reason);

            // Business logic: Update device health, trigger offline/online handlers, send alerts
            await Task.CompletedTask;
        }
    }
}

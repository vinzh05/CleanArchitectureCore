using Shared.Common;

namespace Shared.IntegrationEvents.Contracts.Hikvision
{
    /// <summary>
    /// Integration event được publish khi trạng thái thiết bị thay đổi qua RabbitMQ.
    /// </summary>
    public class DeviceStatusChangedIntegrationEvent : IntegrationEvent
    {
        public string DeviceId { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public string PreviousStatus { get; set; } = string.Empty;
        public string CurrentStatus { get; set; } = string.Empty;
        public DateTime StatusChangedAt { get; set; }
        public string IpAddress { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }
}

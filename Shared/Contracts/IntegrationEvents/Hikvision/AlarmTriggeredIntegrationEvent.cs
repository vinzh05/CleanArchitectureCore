using Shared.Common;

namespace Shared.IntegrationEvents.Contracts.Hikvision
{
    /// <summary>
    /// Integration event được publish khi có cảnh báo từ thiết bị qua RabbitMQ.
    /// </summary>
    public class AlarmTriggeredIntegrationEvent : IntegrationEvent
    {
        public string AlarmId { get; set; } = string.Empty;
        public string DeviceId { get; set; } = string.Empty;
        public string AlarmType { get; set; } = string.Empty;
        public string AlarmLevel { get; set; } = string.Empty;
        public DateTime AlarmTime { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public Dictionary<string, object> MetaData { get; set; } = new();
    }
}

using Shared.Common;

namespace Shared.IntegrationEvents.Contracts.Hikvision
{
    /// <summary>
    /// Integration event được publish khi cửa được mở qua RabbitMQ.
    /// </summary>
    public class DoorOpenedIntegrationEvent : IntegrationEvent
    {
        public string DoorId { get; set; } = string.Empty;
        public string DoorName { get; set; } = string.Empty;
        public string DeviceId { get; set; } = string.Empty;
        public DateTime OpenedAt { get; set; }
        public string OpenMethod { get; set; } = string.Empty;
        public string? PersonId { get; set; }
        public string? CardNo { get; set; }
        public int DurationSeconds { get; set; }
    }
}

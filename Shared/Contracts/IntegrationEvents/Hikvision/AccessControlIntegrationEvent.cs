using Shared.Common;

namespace Shared.IntegrationEvents.Contracts.Hikvision
{
    /// <summary>
    /// Integration event được publish khi có sự kiện kiểm soát ra vào từ Hikvision qua RabbitMQ.
    /// </summary>
    public class AccessControlIntegrationEvent : IntegrationEvent
    {
        public string DeviceId { get; set; } = string.Empty;
        public string PersonId { get; set; } = string.Empty;
        public string DoorId { get; set; } = string.Empty;
        public DateTime AccessTime { get; set; }
        public bool AccessGranted { get; set; }
        public string AccessType { get; set; } = string.Empty;
        public string CardNo { get; set; } = string.Empty;
    }
}

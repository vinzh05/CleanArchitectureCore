using Shared.Common;

namespace Shared.IntegrationEvents.Contracts.Hikvision
{
    /// <summary>
    /// Integration event được publish khi thông tin người dùng được đồng bộ qua RabbitMQ.
    /// </summary>
    public class PersonSyncedIntegrationEvent : IntegrationEvent
    {
        public string PersonId { get; set; } = string.Empty;
        public string PersonName { get; set; } = string.Empty;
        public string CardNo { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public DateTime SyncedAt { get; set; }
        public string SyncAction { get; set; } = string.Empty;
        public List<string> AccessDoorIds { get; set; } = new();
    }
}

using Domain.Common;
using MediatR;

namespace Domain.Events.Hikvision
{
    /// <summary>
    /// Domain event được raise khi cửa được mở trong hệ thống Hikvision.
    /// </summary>
    public class DoorOpenedDomainEvent : BaseEvent, INotification
    {
        public string DoorId { get; }
        public string DoorName { get; }
        public string DeviceId { get; }
        public DateTime OpenedAt { get; }
        public string OpenMethod { get; } // "Card", "Remote", "Button", "Fingerprint", "Face"
        public string? PersonId { get; }
        public string? CardNo { get; }
        public int DurationSeconds { get; }

        public DoorOpenedDomainEvent(
            string doorId,
            string doorName,
            string deviceId,
            DateTime openedAt,
            string openMethod,
            int durationSeconds,
            string? personId = null,
            string? cardNo = null)
        {
            DoorId = doorId;
            DoorName = doorName;
            DeviceId = deviceId;
            OpenedAt = openedAt;
            OpenMethod = openMethod;
            PersonId = personId;
            CardNo = cardNo;
            DurationSeconds = durationSeconds;
        }
    }
}

using Domain.Common;
using MediatR;

namespace Domain.Events.Hikvision
{
    /// <summary>
    /// Domain event được raise khi trạng thái thiết bị Hikvision thay đổi.
    /// </summary>
    public class DeviceStatusChangedDomainEvent : BaseEvent, INotification
    {
        public string DeviceId { get; }
        public string DeviceName { get; }
        public string PreviousStatus { get; }
        public string CurrentStatus { get; }
        public DateTime StatusChangedAt { get; }
        public string IpAddress { get; }
        public string Reason { get; }

        public DeviceStatusChangedDomainEvent(
            string deviceId,
            string deviceName,
            string previousStatus,
            string currentStatus,
            DateTime statusChangedAt,
            string ipAddress,
            string reason)
        {
            DeviceId = deviceId;
            DeviceName = deviceName;
            PreviousStatus = previousStatus;
            CurrentStatus = currentStatus;
            StatusChangedAt = statusChangedAt;
            IpAddress = ipAddress;
            Reason = reason;
        }
    }
}

using Domain.Common;
using MediatR;

namespace Domain.Events.Hikvision
{
    /// <summary>
    /// Domain event được raise khi có sự kiện kiểm soát ra vào từ Hikvision.
    /// </summary>
    public class AccessControlDomainEvent : BaseEvent, INotification
    {
        public string DeviceId { get; }
        public string PersonId { get; }
        public string DoorId { get; }
        public DateTime AccessTime { get; }
        public bool AccessGranted { get; }
        public string AccessType { get; }
        public string CardNo { get; }

        public AccessControlDomainEvent(
            string deviceId, 
            string personId, 
            string doorId, 
            DateTime accessTime, 
            bool accessGranted,
            string accessType,
            string cardNo)
        {
            DeviceId = deviceId;
            PersonId = personId;
            DoorId = doorId;
            AccessTime = accessTime;
            AccessGranted = accessGranted;
            AccessType = accessType;
            CardNo = cardNo;
        }
    }
}

using Domain.Common;
using MediatR;

namespace Domain.Events.Hikvision
{
    /// <summary>
    /// Domain event được raise khi có cảnh báo từ thiết bị Hikvision.
    /// </summary>
    public class AlarmTriggeredDomainEvent : BaseEvent, INotification
    {
        public string AlarmId { get; }
        public string DeviceId { get; }
        public string AlarmType { get; }
        public string AlarmLevel { get; }
        public DateTime AlarmTime { get; }
        public string Description { get; }
        public string Location { get; }
        public Dictionary<string, object> MetaData { get; }

        public AlarmTriggeredDomainEvent(
            string alarmId,
            string deviceId,
            string alarmType,
            string alarmLevel,
            DateTime alarmTime,
            string description,
            string location,
            Dictionary<string, object>? metaData = null)
        {
            AlarmId = alarmId;
            DeviceId = deviceId;
            AlarmType = alarmType;
            AlarmLevel = alarmLevel;
            AlarmTime = alarmTime;
            Description = description;
            Location = location;
            MetaData = metaData ?? new Dictionary<string, object>();
        }
    }
}

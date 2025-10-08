using Domain.Common;
using MediatR;

namespace Domain.Events.Hikvision
{
    /// <summary>
    /// Domain event được raise khi thông tin người dùng được đồng bộ với hệ thống Hikvision.
    /// </summary>
    public class PersonSyncedDomainEvent : BaseEvent, INotification
    {
        public string PersonId { get; }
        public string PersonName { get; }
        public string CardNo { get; }
        public string Department { get; }
        public DateTime SyncedAt { get; }
        public string SyncAction { get; } // "Created", "Updated", "Deleted"
        public List<string> AccessDoorIds { get; }

        public PersonSyncedDomainEvent(
            string personId,
            string personName,
            string cardNo,
            string department,
            DateTime syncedAt,
            string syncAction,
            List<string>? accessDoorIds = null)
        {
            PersonId = personId;
            PersonName = personName;
            CardNo = cardNo;
            Department = department;
            SyncedAt = syncedAt;
            SyncAction = syncAction;
            AccessDoorIds = accessDoorIds ?? new List<string>();
        }
    }
}

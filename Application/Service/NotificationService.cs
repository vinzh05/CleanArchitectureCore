using Application.Abstractions.Services;
using Application.Abstractions.SignalR;
using System.Threading.Tasks;

namespace Application.Service
{
    /// <summary>
    /// Service layer cho Notification - chịu trách nhiệm business logic
    /// Sử dụng INotificationHub để gửi notification thông qua Infrastructure
    /// Xử lý bất đồng bộ (async/await) để không block thread
    /// </summary>
    public class NotificationService : INotificationService
    {
        private readonly INotificationHub _hub;

        public NotificationService(INotificationHub hub)
        {
            _hub = hub ?? throw new ArgumentNullException(nameof(hub));
        }

        /// <summary>
        /// Gửi notification cho tất cả clients (Broadcast)
        /// Async method để không block thread chính
        /// </summary>
        public async Task SendNotificationAsync(string message)
        {
            // Có thể thêm business logic ở đây: logging, validation, etc.
            await _hub.SendNotification(message);
        }

        /// <summary>
        /// Gửi notification cho client cụ thể
        /// Async để đảm bảo non-blocking I/O
        /// </summary>
        public async Task SendToSpecificClientAsync(string connectionId, string message)
        {
            // Validation connectionId
            if (string.IsNullOrEmpty(connectionId))
                throw new ArgumentException("ConnectionId cannot be null or empty");

            await _hub.SendToClient(connectionId, message);
        }

        /// <summary>
        /// Gửi notification cho nhóm admin
        /// Async pattern để handle multiple concurrent requests
        /// </summary>
        public async Task SendToAdminGroupAsync(string message)
        {
            await _hub.SendToGroup("admins", message);
        }

        /// <summary>
        /// Gửi notification cho user cụ thể
        /// Async để support high-throughput scenarios
        /// </summary>
        public async Task SendToUserAsync(string userId, string message)
        {
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentException("UserId cannot be null or empty");

            await _hub.SendToUser(userId, message);
        }

        /// <summary>
        /// Gửi broadcast notification
        /// Async để không block caller thread
        /// </summary>
        public async Task BroadcastNotificationAsync(string message)
        {
            await _hub.SendNotification(message);
        }
    }
}

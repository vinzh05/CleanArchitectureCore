using Application.Abstractions.Services;
using Application.Abstractions.SignalR;
using Application.Contracts.Notification;
using Shared.Common;
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
        public async Task<Result<string>> SendNotificationAsync(string message)
        {
            await _hub.SendNotification(message);
            return Result<string>.SuccessResult("Notification sent to all clients");
        }

        /// <summary>
        /// Gửi notification cho client cụ thể
        /// Async để đảm bảo non-blocking I/O
        /// </summary>
        public async Task<Result<string>> SendToSpecificClientAsync(string connectionId, string message)
        {
            // Validation connectionId
            if (string.IsNullOrEmpty(connectionId))
                return Result<string>.FailureResult("ConnectionId cannot be null or empty");

            await _hub.SendToClient(connectionId, message);
            return Result<string>.SuccessResult($"Notification sent to connection {connectionId}");
        }

        /// <summary>
        /// Gửi notification cho nhóm admin
        /// Async pattern để handle multiple concurrent requests
        /// </summary>
        public async Task<Result<string>> SendToAdminGroupAsync(string message)
        {
            await _hub.SendToGroup("admins", message);
            return Result<string>.SuccessResult("Notification sent to admin group");
        }

        /// <summary>
        /// Gửi notification cho user cụ thể
        /// Async để support high-throughput scenarios
        /// </summary>
        public async Task<Result<string>> SendToUserAsync(string userId, string message)
        {
            if (string.IsNullOrEmpty(userId))
                Result<string>.FailureResult("UserId cannot be null or empty");
            await _hub.SendToUser(userId, message);
            return Result<string>.SuccessResult($"Notification sent to user {userId}");
        }

        /// <summary>
        /// Gửi broadcast notification
        /// Async để không block caller thread
        /// </summary>
        public async Task<Result<string>> BroadcastNotificationAsync(string message)
        {
            await _hub.SendNotification(message);
            return Result<string>.SuccessResult("Broadcast sent successfully");
        }

        /// <summary>
        /// Gửi notification hàng loạt đến nhiều user
        /// </summary>
        public async Task<Result<string>> SendBulkNotificationAsync(BulkNotificationRequest request)
        {
            var tasks = new List<Task>();

            foreach (var userId in request.UserIds)
                tasks.Add(SendToUserAsync(userId, request.Message));

            await Task.WhenAll(tasks);
            return Result<string>.SuccessResult($"Notifications sent to {request.UserIds.Count} users");
        }
    }
}

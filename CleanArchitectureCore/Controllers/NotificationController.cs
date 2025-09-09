using Application.Abstractions.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CleanArchitectureCore.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        /// <summary>
        /// Gửi notification cho tất cả users (Broadcast)
        /// </summary>
        [HttpPost("broadcast")]
        public async Task<IActionResult> BroadcastNotification([FromBody] string message)
        {
            // Async: Không block thread, có thể handle multiple concurrent requests
            await _notificationService.BroadcastNotificationAsync(message);
            return Ok("Notification sent to all users");
        }

        /// <summary>
        /// Gửi notification cho user cụ thể
        /// </summary>
        [HttpPost("user/{userId}")]
        public async Task<IActionResult> SendToUser(string userId, [FromBody] string message)
        {
            try
            {
                // Async: Validation và SignalR operation không block thread
                await _notificationService.SendToUserAsync(userId, message);
                return Ok($"Notification sent to user {userId}");
            }
            catch (System.ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Gửi notification cho nhóm admin
        /// </summary>
        [HttpPost("admin-group")]
        public async Task<IActionResult> SendToAdminGroup([FromBody] string message)
        {
            // Async: Cho phép controller xử lý request khác song song
            await _notificationService.SendToAdminGroupAsync(message);
            return Ok("Notification sent to admin group");
        }

        /// <summary>
        /// Ví dụ về xử lý bất đồng bộ nâng cao
        /// </summary>
        [HttpPost("bulk-send")]
        public async Task<IActionResult> SendBulkNotifications([FromBody] BulkNotificationRequest request)
        {
            var tasks = new List<Task>();

            // Parallel async operations - không block thread
            foreach (var userId in request.UserIds)
            {
                // Tạo task async cho mỗi user
                tasks.Add(_notificationService.SendToUserAsync(userId, request.Message));
            }

            // Chờ tất cả tasks hoàn thành (non-blocking)
            await Task.WhenAll(tasks);

            return Ok($"Notifications sent to {request.UserIds.Count} users");
        }
    }

    public class BulkNotificationRequest
    {
        public List<string> UserIds { get; set; } = new();
        public string Message { get; set; } = string.Empty;
    }
}

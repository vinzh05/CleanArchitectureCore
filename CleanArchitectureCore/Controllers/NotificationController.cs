using Application.Abstractions.Services;
using Application.Contracts.Notification;
using Application.Service;
using ChatDakenh.Controllers.Common;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using static MassTransit.Monitoring.Performance.BuiltInCounters;

namespace CleanArchitectureCore.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationController : BaseController
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
        public async Task<IActionResult> BroadcastNotification([FromBody] string message) => await HandleAsync(_notificationService.SendNotificationAsync(message));

        /// <summary>
        /// Gửi notification cho user cụ thể
        /// </summary>
        [HttpPost("user/{userId}")]
        public async Task<IActionResult> SendToUser(string userId, [FromBody] string message) => await HandleAsync(_notificationService.SendToUserAsync(userId, message));

        /// <summary>
        /// Gửi notification cho nhóm admin
        /// </summary>
        [HttpPost("admin-group")]
        public async Task<IActionResult> SendToAdminGroup([FromBody] string message) => await HandleAsync(_notificationService.SendToAdminGroupAsync(message));

        /// <summary>
        /// Gửi notification cho client cụ thể theo ConnectionId
        /// </summary>
        [HttpPost("bulk-send")]
        public async Task<IActionResult> SendBulkNotifications([FromBody] BulkNotificationRequest request) => await HandleAsync(_notificationService.SendBulkNotificationAsync(request));
    }
}

using Application.Service;
using Microsoft.AspNetCore.SignalR;
using CleanArchitectureCore.Controllers;

namespace CleanArchitectureCore.Services
{
    public class NotificationHubAdapter : INotificationHub
    {
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationHubAdapter(IHubContext<NotificationHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task SendNotification(string message)
        {
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", message);
        }
    }
}

using Microsoft.AspNetCore.SignalR;
using Application.Abstractions.SignalR;
using System.Threading.Tasks;

namespace CleanArchitectureCore.Controllers
{
    public class NotificationHub : Hub, INotificationHubContext
    {
        public async Task SendNotificationAsync(string message) => await Clients.All.SendAsync("ReceiveNotification", message);

        public async Task SendToClientAsync(string connectionId, string message) => await Clients.Client(connectionId).SendAsync("ReceiveNotification", message);

        public async Task SendToGroupAsync(string groupName, string message) => await Clients.Group(groupName).SendAsync("ReceiveNotification", message);

        public async Task SendToUserAsync(string userId, string message) => await Clients.User(userId).SendAsync("ReceiveNotification", message);
    }
}
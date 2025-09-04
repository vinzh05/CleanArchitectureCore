using Application.Abstractions.Services;
using System.Threading.Tasks;

namespace Application.Service
{
    public interface INotificationHub
    {
        Task SendNotification(string message);
    }

    public class NotificationService : INotificationService
    {
        private readonly INotificationHub _hub;

        public NotificationService(INotificationHub hub)
        {
            _hub = hub;
        }

        public async Task SendNotificationAsync(string message)
        {
            await _hub.SendNotification(message);
        }
    }
}

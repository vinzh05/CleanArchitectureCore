using System.Threading.Tasks;

namespace Application.Abstractions.Services
{
    public interface INotificationService
    {
        Task SendNotificationAsync(string message);
        Task SendToSpecificClientAsync(string connectionId, string message);
        Task SendToAdminGroupAsync(string message);
        Task SendToUserAsync(string userId, string message);
        Task BroadcastNotificationAsync(string message);
    }
}

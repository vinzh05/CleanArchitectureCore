using Application.Contracts.Notification;
using Shared.Common;
using System.Threading.Tasks;

namespace Application.Abstractions.Services
{
    public interface INotificationService
    {
        Task<Result<string>> SendNotificationAsync(string message);
        Task<Result<string>> SendToSpecificClientAsync(string connectionId, string message);
        Task<Result<string>> SendToAdminGroupAsync(string message);
        Task<Result<string>> SendToUserAsync(string userId, string message);
        Task<Result<string>> BroadcastNotificationAsync(string message);
        Task<Result<string>> SendBulkNotificationAsync(BulkNotificationRequest request);
    }
}

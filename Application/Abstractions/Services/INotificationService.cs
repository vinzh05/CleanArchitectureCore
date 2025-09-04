using System.Threading.Tasks;

namespace Application.Abstractions.Services
{
    public interface INotificationService
    {
        Task SendNotificationAsync(string message);
    }
}

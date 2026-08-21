using System.Threading.Tasks;

namespace EduLearn.Services
{
    public interface INotificationService
    {
        Task NotifyAsync(string userId, string message, string? link = null);
    }
}

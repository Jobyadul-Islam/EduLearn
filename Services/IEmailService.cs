using System.Threading.Tasks;

namespace EduLearn.Services
{
    public interface IEmailService
    {
        bool IsConfigured { get; }
        Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody);
    }
}

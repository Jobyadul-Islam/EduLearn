using System;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace EduLearn.Services
{
    public class SmtpEmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SmtpEmailService> _logger;

        public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        private string Host => _configuration["Email:SmtpHost"] ?? "smtp.gmail.com";
        private int Port => int.TryParse(_configuration["Email:SmtpPort"], out var p) ? p : 587;
        private string Username => _configuration["Email:Username"] ?? "";
        private string Password => _configuration["Email:Password"] ?? "";
        private string FromName => _configuration["Email:FromName"] ?? "EduLearn";

        public bool IsConfigured => !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            if (!IsConfigured)
            {
                _logger.LogWarning("Email service not configured — skipped sending '{Subject}' to {ToEmail}", subject, toEmail);
                return false;
            }

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(FromName, Username));
                message.To.Add(MailboxAddress.Parse(toEmail));
                message.Subject = subject;
                message.Body = new TextPart("html") { Text = htmlBody };

                using var client = new SmtpClient();
                await client.ConnectAsync(Host, Port, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(Username, Password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email '{Subject}' to {ToEmail}", subject, toEmail);
                return false;
            }
        }
    }
}

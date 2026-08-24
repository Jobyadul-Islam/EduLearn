using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EduLearn.Services
{
    // Runs a check immediately at startup (so a demo run doesn't have to wait), then
    // every 6 hours after — frequent enough that the 24-hour reminder window is never
    // missed by more than a few hours, without hammering the database or mail server.
    public class DeadlineReminderBackgroundService : BackgroundService
    {
        private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(6);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DeadlineReminderBackgroundService> _logger;

        public DeadlineReminderBackgroundService(IServiceScopeFactory scopeFactory, ILogger<DeadlineReminderBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var reminderService = scope.ServiceProvider.GetRequiredService<IDeadlineReminderService>();
                    var sent = await reminderService.SendDueRemindersAsync();
                    if (sent > 0)
                    {
                        _logger.LogInformation("Deadline reminder check sent {Count} reminder email(s).", sent);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Deadline reminder check failed.");
                }

                try
                {
                    await Task.Delay(CheckInterval, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    // Expected on shutdown
                }
            }
        }
    }
}

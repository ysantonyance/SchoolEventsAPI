using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace SchoolEvents.Worker.Services
{
    public class NotificationWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<NotificationWorker> _logger;

        public NotificationWorker(
            IServiceScopeFactory scopeFactory,
            ILogger<NotificationWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Notification worker started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var processor = scope.ServiceProvider.GetRequiredService<NotificationJobProcessor>();
                    var processedCount = await processor.ProcessPendingJobsAsync(stoppingToken);

                    if (processedCount == 0)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected notification worker error.");
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                }
            }

            _logger.LogInformation("Notification worker stopped.");
        }
    }
}

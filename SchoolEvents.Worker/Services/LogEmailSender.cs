using Microsoft.Extensions.Logging;

namespace SchoolEvents.Worker.Services
{
    public class LogEmailSender : IEmailSender
    {
        private readonly ILogger<LogEmailSender> _logger;

        public LogEmailSender(ILogger<LogEmailSender> logger)
        {
            _logger = logger;
        }

        public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("EMAIL LOG ONLY");
            _logger.LogInformation("To: {To}", to);
            _logger.LogInformation("Subject: {Subject}", subject);
            _logger.LogInformation("{Body}", body);

            return Task.CompletedTask;
        }
    }
}

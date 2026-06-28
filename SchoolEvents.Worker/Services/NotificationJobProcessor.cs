using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchoolEventsAPI.Data;
using SchoolEventsAPI.Models;

namespace SchoolEvents.Worker.Services
{
    public class NotificationJobProcessor
    {
        private const int MaxAttempts = 3;

        private readonly AppDbContext _db;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<NotificationJobProcessor> _logger;

        public NotificationJobProcessor(
            AppDbContext db,
            IEmailSender emailSender,
            ILogger<NotificationJobProcessor> logger)
        {
            _db = db;
            _emailSender = emailSender;
            _logger = logger;
        }

        public async Task<int> ProcessPendingJobsAsync(CancellationToken cancellationToken = default)
        {
            var jobs = await _db.NotificationJobs
                .Where(j => j.Status == "pending" && j.AttemptCount < MaxAttempts)
                .OrderBy(j => j.CreatedAt)
                .Take(10)
                .ToListAsync(cancellationToken);

            foreach (var job in jobs)
            {
                await ProcessJobAsync(job, cancellationToken);
            }

            return jobs.Count;
        }

        private async Task ProcessJobAsync(NotificationJob job, CancellationToken cancellationToken)
        {
            job.Status = "processing";
            job.AttemptCount++;
            job.ErrorMessage = string.Empty;
            await _db.SaveChangesAsync(cancellationToken);

            try
            {
                var payload = ParsePayload(job);
                var user = await _db.Users.FindAsync([payload.UserId], cancellationToken);
                var ev = await _db.Events.FindAsync([payload.EventId], cancellationToken);

                if (user == null || ev == null)
                {
                    throw new InvalidOperationException(
                        $"Missing related data for job {job.Id}. UserId={payload.UserId}, EventId={payload.EventId}");
                }

                var (subject, body) = BuildMessage(job.Type, user, ev);
                await _emailSender.SendAsync(user.Email, subject, body, cancellationToken);

                job.Status = "sent";
                job.ErrorMessage = string.Empty;
                await _db.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Notification job {JobId} processed successfully.", job.Id);
            }
            catch (Exception ex)
            {
                job.Status = job.AttemptCount >= MaxAttempts ? "failed" : "pending";
                job.ErrorMessage = ex.Message;
                await _db.SaveChangesAsync(cancellationToken);

                _logger.LogError(ex, "Notification job {JobId} failed on attempt {AttemptCount}.", job.Id, job.AttemptCount);
            }
        }

        private static NotificationPayload ParsePayload(NotificationJob job)
        {
            var payload = JsonSerializer.Deserialize<NotificationPayload>(
                job.PayLoad,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (payload == null || payload.UserId <= 0 || payload.EventId <= 0)
            {
                throw new InvalidOperationException($"Invalid payload for notification job {job.Id}.");
            }

            return payload;
        }

        private static (string Subject, string Body) BuildMessage(string type, User user, Event ev)
        {
            return type switch
            {
                "RegistrationConfirmed" => (
                    $"Registration confirmed: {ev.Title}",
                    $"Hello {user.DisplayName}, your registration for \"{ev.Title}\" is confirmed."),

                "RegistrationWaitlisted" => (
                    $"You are on the waitlist: {ev.Title}",
                    $"Hello {user.DisplayName}, the event \"{ev.Title}\" is full, so you were added to the waitlist."),

                "RegistrationCancelled" => (
                    $"Registration cancelled: {ev.Title}",
                    $"Hello {user.DisplayName}, your registration for \"{ev.Title}\" was cancelled."),

                "WaitlistPromoted" => (
                    $"You got a seat: {ev.Title}",
                    $"Hello {user.DisplayName}, you were promoted from the waitlist and now have a confirmed seat for \"{ev.Title}\"."),

                _ => (
                    $"School event notification: {ev.Title}",
                    $"Hello {user.DisplayName}, there is an update for \"{ev.Title}\".")
            };
        }

        private sealed class NotificationPayload
        {
            public int Id { get; set; }
            public int EventId { get; set; }
            public int UserId { get; set; }
        }
    }
}

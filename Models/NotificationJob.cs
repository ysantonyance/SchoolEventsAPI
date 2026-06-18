namespace SchoolEventsAPI.Models
{
    public class NotificationJob
    {

        public int Id { get; set; }
        public string Type { get; set; } // RegistrationConfirmed, WaitlistPromoted, etc.
        public string PayLoad { get; set; } //JSON
        public string Status { get; set; } = "pending"; // pending, processing, sent, failed
        public int AttemptCount { get; set; } = 0;
        public string IdempotencyKey { get; set; } // registrationId: type
        public string ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
 }

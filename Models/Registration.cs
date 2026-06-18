namespace SchoolEventsAPI.Models
{
    public class Registration
    {
        public int Id { get; set; }
        public string Status { get; set; } = "CONFIRMED"; // CONFIRMED, WAITLISTED, CANCELLED

        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

        public int UserId { get; set; }
        public User User { get; set; } = null;

        public int EventId { get; set; }
        public Event Event { get; set; } = null;
    }
}

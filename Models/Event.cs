namespace SchoolEventsAPI.Models
{
    public class Event
    {

        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int Capacity { get; set; }
        public string Location { get; set; }
        public string Status { get; set; } = "DRAFT"; // DRAFT, PUBLISHED, CANCELLED
        public DateTime DateTime { get; set; } = DateTime.UtcNow;

        public int OrganizerId { get; set; }
        public User Organizer { get; set; }

        public ICollection<Registration> Registrations { get; set; } = new List<Registration>();
    }
}

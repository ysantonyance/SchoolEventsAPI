namespace SchoolEventsAPI.DTOs
{
    public class CreateEventDTO
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int Capacity { get; set; }
        public string Location { get; set; }
    }

    public class  UpdateEventDTO
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int Capacity { get; set; }
        public string Location { get; set; }
    }

    public class EventResponseDTO
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int Capacity { get; set; }
        public string Location { get; set; }
        public string Status { get; set; }
        public int OrganizerId { get; set; }
        public int CofirmedCount { get; set; }
        public int WaitlistCount { get; set; }
    }
}

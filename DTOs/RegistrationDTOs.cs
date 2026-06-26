namespace SchoolEventsAPI.DTOs
{
    public class RegistrationResponseDTO
    {
        public int Id { get; set; }
        public int EventId { get; set; }
        public string Status { get; set; }
        public int? WaitlistPosition { get; set; }
        public DateTime RegisteredAt { get; set; }
    }
}

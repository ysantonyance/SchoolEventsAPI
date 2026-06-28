using System.ComponentModel.DataAnnotations;

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

    public class WaitlistEntryDTO
    {
        public int RegistrationId { get; set; }
        public int UserId { get; set; }
        public string DisplayName { get; set; }
        public string Email { get; set; }
        public int Position { get; set; }
        public DateTime RegisteredAt { get; set; }
    }
}

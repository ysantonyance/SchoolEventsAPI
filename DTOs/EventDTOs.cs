using System.ComponentModel.DataAnnotations;

namespace SchoolEventsAPI.DTOs
{
    public class CreateEventDTO : IValidatableObject
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Title must be between 3 and 100 characters")]
        public string Title { get; set; }

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Start time is required")]
        public DateTime StartTime { get; set; }

        [Required(ErrorMessage = "End time is required")]
        public DateTime EndTime { get; set; }

        [Range(1, 5, ErrorMessage = "Capacity must be between 1 and 5")]
        public int Capacity { get; set; }

        [Required(ErrorMessage = "Location is required")]
        [StringLength(200, ErrorMessage = "Location cannot exceed 200 characters")]
        public string Location { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext context)
        {
            if (EndTime <= StartTime)
                yield return new ValidationResult(
                    "End time must be after start time",
                    new[] { nameof(EndTime) });

            if (StartTime < DateTime.UtcNow)
                yield return new ValidationResult(
                    "Start time cannot be in the past",
                    new[] { nameof(StartTime) });
        }
    }

    public class UpdateEventDTO : IValidatableObject
    {
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Title must be between 3 and 100 characters")]
        public string Title { get; set; }

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string Description { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        [Range(1, 5, ErrorMessage = "Capacity must be between 1 and 5")]
        public int Capacity { get; set; }

        [StringLength(200, ErrorMessage = "Location cannot exceed 200 characters")]
        public string Location { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext context)
        {
            if (EndTime <= StartTime)
                yield return new ValidationResult(
                    "End time must be after start time",
                    new[] { nameof(EndTime) });
        }
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

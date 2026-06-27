using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolEventsAPI.Data;
using SchoolEventsAPI.DTOs;
using SchoolEventsAPI.Models;
using System.Security.Claims;

namespace SchoolEventsAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EventsController : ControllerBase
    {
        private readonly AppDbContext _db;
        public EventsController(AppDbContext context)
        {
            _db = context;
        }

        private int CurrentUserId =>
            int.Parse(User.FindFirstValue("id")!);

        private string CurrentUserRole =>
            User.FindFirstValue(ClaimTypes.Role)!;

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateEvent(CreateEventDTO dto)
        {
            if (CurrentUserRole != "organizer")
            {
                return Forbid("Only administrators can create events.");
            }

            var newEvent = new Event
            {
                Title = dto.Title,
                Description = dto.Description,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                Capacity = dto.Capacity,
                Location = dto.Location,
                Status = "DRAFT",
                OrganizerId = CurrentUserId
            };
            _db.Events.Add(newEvent);
            await _db.SaveChangesAsync();
            return Ok(new { message = "Event created successfully.", eventId = newEvent.Id });
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAll()
        {
            IQueryable<Event> query = _db.Events;

            if (CurrentUserRole == "student")
            {
                query = query.Where(e => e.Status == "PUBLISHED");
            }
            else if (CurrentUserRole == "organizer")
            {
                query = query.Where(e => e.OrganizerId == CurrentUserId);
            }

            var events = await query.ToListAsync();

            var result = new List<EventResponseDTO>();
            foreach (var e in events)
            {
                var confirmed = await _db.Registrations.CountAsync(r => r.EventId == e.Id && r.Status == "CONFIRMED");
                var waitlist = await _db.Registrations.CountAsync(r => r.EventId == e.Id && r.Status == "WAITLISTED");
                result.Add(ToResponseDTO(e, confirmed, waitlist));
            }

            return Ok(result);
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetById(int id)
        {
            var ev = await _db.Events.FindAsync(id);
            if (ev == null)
            {
                return NotFound();
            }

            if (CurrentUserRole == "student" && ev.Status != "PUBLISHED")
            {
                return NotFound();
            }

            if (CurrentUserRole == "organizer" && ev.OrganizerId != CurrentUserId && ev.Status != "PUBLISHED")
            {
                return Forbid();
            }

            var confirmed = await _db.Registrations.CountAsync(r => r.EventId == ev.Id && r.Status == "CONFIRMED");
            var waitlist = await _db.Registrations.CountAsync(r => r.EventId == ev.Id && r.Status == "WAITLISTED");

            return Ok(ToResponseDTO(ev, confirmed, waitlist));
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> Update(int id, UpdateEventDTO dto)
        {
            if (CurrentUserRole != "organizer")
                return Forbid();

            var ev = await _db.Events.FindAsync(id);
            if (ev == null)
                return NotFound();

            if (ev.OrganizerId != CurrentUserId)
                return Forbid();

            if (ev.Status != "DRAFT")
                return BadRequest(new { error = "Only DRAFT events can be edited" });

            ev.Title = dto.Title;
            ev.Description = dto.Description;
            ev.StartTime = dto.StartTime;
            ev.EndTime = dto.EndTime;
            ev.Capacity = dto.Capacity;
            ev.Location = dto.Location;

            await _db.SaveChangesAsync();

            return Ok(ToResponseDTO(ev, 0, 0));
        }

        [HttpPost("{id}/publish")]
        [Authorize]
        public async Task<IActionResult> Publish(int id)
        {
            if (CurrentUserRole != "organizer")
                return Forbid();

            var ev = await _db.Events.FindAsync(id);
            if (ev == null)
                return NotFound();

            if (ev.OrganizerId != CurrentUserId)
                return Forbid();

            if (ev.Status != "DRAFT")
                return BadRequest(new { error = "Only DRAFT events can be published" });

            ev.Status = "PUBLISHED";
            await _db.SaveChangesAsync();

            // Worker

            return Ok(ToResponseDTO(ev, 0, 0));
        }

        [HttpPost("{id}/cancel")]
        [Authorize]
        public async Task<IActionResult> Cancel(int id)
        {
            if (CurrentUserRole != "organizer")
                return Forbid();

            var ev = await _db.Events.FindAsync(id);
            if (ev == null)
                return NotFound();

            if (ev.OrganizerId != CurrentUserId)
                return Forbid();

            ev.Status = "CANCELLED";
            await _db.SaveChangesAsync();

            // Worker

            return Ok(ToResponseDTO(ev, 0, 0));
        }

        [HttpGet("{id}/registrations")]
        [Authorize]
        public async Task<IActionResult> GetRegistrations(int id)
        {
            if (CurrentUserRole != "organizer")
                return Forbid();

            var ev = await _db.Events.FindAsync(id);
            if (ev == null)
                return NotFound();

            if (ev.OrganizerId != CurrentUserId)
                return Forbid();

            var registrations = await _db.Registrations
                .Where(r => r.EventId == id && r.Status == "CONFIRMED")
                .Include(r => r.User)
                .OrderBy(r => r.RegisteredAt)
                .Select(r => new
                {
                    RegistrationId = r.Id,
                    r.UserId,
                    r.User.DisplayName,
                    r.User.Email,
                    r.RegisteredAt
                })
                .ToListAsync();

            return Ok(registrations);
        }

        [HttpGet("{id}/waitlist")]
        [Authorize]
        public async Task<IActionResult> GetWaitlist(int id)
        {
            if (CurrentUserRole != "organizer")
                return Forbid();

            var ev = await _db.Events.FindAsync(id);
            if (ev == null)
                return NotFound();

            if (ev.OrganizerId != CurrentUserId)
                return Forbid();

            var waitlist = await _db.Registrations
                .Where(r => r.EventId == id && r.Status == "WAITLISTED")
                .Include(r => r.User)
                .OrderBy(r => r.RegisteredAt)
                .ToListAsync();

            var result = waitlist.Select((r, index) => new WaitlistEntryDTO
            {
                RegistrationId = r.Id,
                UserId = r.UserId,
                DisplayName = r.User.DisplayName,
                Email = r.User.Email,
                Position = index + 1,
                RegisteredAt = r.RegisteredAt
            });

            return Ok(result);
        }

        private static EventResponseDTO ToResponseDTO(Event ev, int confirmed, int waitlisted) => new()
        {
            Id = ev.Id,
            Title = ev.Title,
            Description = ev.Description,
            StartTime = ev.StartTime,
            EndTime = ev.EndTime,
            Capacity = ev.Capacity,
            Location = ev.Location,
            Status = ev.Status,
            OrganizerId = ev.OrganizerId,
            CofirmedCount = confirmed,
            WaitlistCount = waitlisted
        };
    }
}

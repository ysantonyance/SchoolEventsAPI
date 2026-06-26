using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolEventsAPI.Data;
using SchoolEventsAPI.DTOs;
using SchoolEventsAPI.Models;
using System.Security.Claims;
using System.Text.Json;

namespace SchoolEventsAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class RegistrationsController : ControllerBase
    {
        private readonly AppDbContext _db;
        public RegistrationsController(AppDbContext db)
        {
            _db = db;
        }
        private int CurrentUserId =>
            int.Parse(User.FindFirstValue("id")!);
        private string CurrentUserRole =>
            User.FindFirstValue(ClaimTypes.Role)!;


        [HttpPost("/events/{eventId}/registrations")]
        [Authorize]
        public async Task<IActionResult> Register(int eventId)
        {
            if (CurrentUserRole != "student")
            {
                return Forbid();
            }

            using var tx = await _db.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable);

            var ev = await _db.Events.FirstOrDefaultAsync(e =>
            e.Id == eventId && e.Status == "PUBLISHED");
            if (ev == null)
                return NotFound(new { error = "Event not found or not published" });

            var existing = await _db.Registrations.FirstOrDefaultAsync(r =>
            r.EventId == eventId && r.UserId == CurrentUserId && r.Status != "CANCELLED");
            if (existing != null)
                return BadRequest(new { error = "You have already registered for this event" });

            var confirmedCount = await _db.Registrations.CountAsync(r =>
            r.EventId == eventId && r.Status == "CONFIRMED");

            var status = confirmedCount < ev.Capacity ? "CONFIRMED" : "WAITLISTED";

            var reg = new Registration
            {
                EventId = eventId,
                UserId = CurrentUserId,
                Status = status,
                RegisteredAt = DateTime.UtcNow
            };

            _db.Registrations.Add(reg);
            await _db.SaveChangesAsync();

            _db.NotificationJobs.Add(new NotificationJob
            {
                Type = status == "CONFIRMED" ? "RegistrationConfirmed" : "RegistrationWaitlisted",
                PayLoad = JsonSerializer.Serialize(new { Id = reg.Id, reg.EventId, reg.UserId }),
                Status = "pending",
                AttemptCount = 0,
                IdempotencyKey = $"{reg.Id}:{(status == "CONFIRMED" ? "RegistrationConfirmed" : "RegistrationWaitlisted")}",
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return Ok(new RegistrationResponseDTO
            {
                Id = reg.Id,
                EventId = eventId,
                Status = status,
                WaitlistPosition = status == "WAITLISTED" ? confirmedCount + 1 : null,
                RegisteredAt = reg.RegisteredAt
            });
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> Cancel(int id)
        {
            if (CurrentUserRole != "student")
            {
                return Forbid();
            }

            using var tx = await _db.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable);

            var reg = await _db.Registrations.FirstOrDefaultAsync(r =>
            r.Id == id && r.UserId == CurrentUserId);
            if (reg == null)
                return NotFound(new { error = "Registration not found" });

            if (reg.Status == "CANCELLED")
                return BadRequest(new { error = "Already cancelled"});

            bool wasConfirmed = reg.Status == "CONFIRMED";
            reg.Status = "CANCELLED";

            if (wasConfirmed)
            {
                var next = await _db.Registrations
                    .Where(r => r.EventId == reg.EventId && r.Status == "WAITLISTED")
                    .OrderBy(r => r.RegisteredAt)
                    .FirstOrDefaultAsync();

                if (next != null)
                {
                    next.Status = "CONFIRMED";

                    _db.NotificationJobs.Add(new NotificationJob
                    {
                        Type = "WaitlistPromoted",
                        PayLoad = JsonSerializer.Serialize(new { Id = next.Id, next.EventId, next.UserId }),
                        Status = "pending",
                        AttemptCount = 0,
                        IdempotencyKey = $"{next.Id}:WaitlistPromoted",
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            _db.NotificationJobs.Add(new NotificationJob
            {
                Type = "RegistrationCancelled",
                PayLoad = JsonSerializer.Serialize(new { Id = reg.Id, reg.EventId, reg.UserId }),
                Status = "pending",
                AttemptCount = 0,
                IdempotencyKey = $"{reg.Id}:RegistrationCancelled",
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return Ok(new { message = "Registration cancelled successfully" });
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> MyRegistrations()
        {
            if (CurrentUserRole != "student")
            {
                return Forbid();
            }

            var myRegs = await _db.Registrations
                .Where(r => r.UserId == CurrentUserId && r.Status != "CANCELLED")
                .OrderBy(r => r.RegisteredAt)
                .ToListAsync();

            var result = new List<RegistrationResponseDTO>();
            foreach (var r in myRegs)
            {
                int? position = null;
                if (r.Status == "WAITLISTED")
                {
                    position = 1 + await _db.Registrations.CountAsync(x => 
                    x.EventId == r.EventId &&
                    x.Status == "WAITLISTED" &&
                    x.RegisteredAt < r.RegisteredAt);
                }
                result.Add(new RegistrationResponseDTO
                {
                    Id = r.Id,
                    EventId = r.EventId,
                    Status = r.Status,
                    WaitlistPosition = position,
                    RegisteredAt = r.RegisteredAt
                });
            }
            return Ok(result);
        }
    }
}

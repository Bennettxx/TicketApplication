using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TicketApplication.Data;
using TicketApplication.DTOs;
using TicketApplication.Models;

namespace TicketApplication.Controllers
{
    // zeiterfassung, route api/ticket/{ticketId}/time, nur admin/support
    [Route("api/ticket/{ticketId}/time")]
    [ApiController]
    [Authorize(Roles = "Admin,Support")]
    public class TimeEntryController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TimeEntryController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int CurrentUserId =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // GET — alle zeiteinträge eines tickets, neueste zuerst
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TimeEntryResponseDto>>> Get(int ticketId)
        {
            if (!await _context.Tickets.AnyAsync(t => t.Id == ticketId))
                return NotFound("Ticket nicht gefunden.");

            var entries = await (from e in _context.TicketTimeEntries
                                 where e.TicketId == ticketId
                                 join u in _context.Users on e.UserId equals u.Id into uj
                                 from u in uj.DefaultIfEmpty()
                                 orderby e.WorkedAt descending, e.Id descending
                                 select new TimeEntryResponseDto
                                 {
                                     Id = e.Id,
                                     TicketId = e.TicketId,
                                     UserId = e.UserId,
                                     UserEmail = u != null ? u.Email : string.Empty,
                                     Minutes = e.Minutes,
                                     Note = e.Note,
                                     WorkedAt = e.WorkedAt,
                                     CreatedAt = e.CreatedAt
                                 }).ToListAsync();

            return Ok(entries);
        }

        // POST — zeiteintrag anlegen, userid aus jwt, validierung im dto
        [HttpPost]
        public async Task<ActionResult<TimeEntryResponseDto>> Post(int ticketId, CreateTimeEntryDto dto)
        {
            if (!await _context.Tickets.AnyAsync(t => t.Id == ticketId))
                return NotFound("Ticket nicht gefunden.");

            var userId = CurrentUserId;

            var entry = new TicketTimeEntry
            {
                TicketId = ticketId,
                UserId = userId,
                Minutes = dto.Minutes,
                Note = dto.Note?.Trim() ?? string.Empty,
                WorkedAt = dto.WorkedAt.Date,
                CreatedAt = DateTime.UtcNow
            };

            _context.TicketTimeEntries.Add(entry);
            await _context.SaveChangesAsync();

            var email = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => u.Email)
                .FirstOrDefaultAsync() ?? string.Empty;

            return Ok(new TimeEntryResponseDto
            {
                Id = entry.Id,
                TicketId = entry.TicketId,
                UserId = entry.UserId,
                UserEmail = email,
                Minutes = entry.Minutes,
                Note = entry.Note,
                WorkedAt = entry.WorkedAt,
                CreatedAt = entry.CreatedAt
            });
        }

        // DELETE — eintrag löschen; support nur eigene, admin alle
        [HttpDelete("{entryId}")]
        public async Task<IActionResult> Delete(int ticketId, int entryId)
        {
            var entry = await _context.TicketTimeEntries
                .FirstOrDefaultAsync(e => e.Id == entryId && e.TicketId == ticketId);
            if (entry == null) return NotFound();

            if (!User.IsInRole("Admin") && entry.UserId != CurrentUserId)
                return Forbid();

            _context.TicketTimeEntries.Remove(entry);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TicketApplication.Data;
using TicketApplication.DTOs;
using TicketApplication.Models;

namespace TicketApplication.Controllers
{
    // ticket-chat, route api/ticket/{ticketId}/dialogue
    [Route("api/ticket/{ticketId}/dialogue")]
    [ApiController]
    [Authorize]
    public class DialogueController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DialogueController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int CurrentUserId =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        private bool IsStaff =>
            User.IsInRole("Admin") || User.IsInRole("Support");

        // GET — alle nachrichten chronologisch
        // user: nur eigenes ticket, keine internen notizen; staff: alles
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DialogueResponseDto>>> Get(int ticketId)
        {
            var ticket = await _context.Tickets.FindAsync(ticketId);
            if (ticket == null) return NotFound("Ticket nicht gefunden.");

            if (!IsStaff && ticket.CreatedByUserId != CurrentUserId)
                return Forbid();

            var query = from d in _context.TicketDialogue
                        where d.TicketId == ticketId
                        join u in _context.Users on d.AuthorUserId equals u.Id into uj
                        from u in uj.DefaultIfEmpty()
                        orderby d.CreatedAt
                        select new DialogueResponseDto
                        {
                            Id = d.Id,
                            TicketId = d.TicketId,
                            AuthorUserId = d.AuthorUserId,
                            AuthorEmail = u != null ? u.Email : string.Empty,
                            Text = d.Text,
                            IsInternal = d.IsInternal,
                            CreatedAt = d.CreatedAt
                        };

            var messages = await query.ToListAsync();

            // sicherheitsnetz: interne notizen für normale user rausfiltern
            if (!IsStaff)
                messages = messages.Where(m => !m.IsInternal).ToList();

            return Ok(messages);
        }

        // POST — neue nachricht, IsInternal darf nur staff setzen
        [HttpPost]
        public async Task<ActionResult<DialogueResponseDto>> Post(int ticketId, CreateDialogueDto dto)
        {
            var ticket = await _context.Tickets.FindAsync(ticketId);
            if (ticket == null) return NotFound("Ticket nicht gefunden.");

            var userId = CurrentUserId;
            if (!IsStaff && ticket.CreatedByUserId != userId)
                return Forbid();

            // geschlossene tickets: nur staff darf noch schreiben
            if (ticket.Status == TicketStatus.Closed && !IsStaff)
                return BadRequest("Dieses Ticket ist geschlossen. Bitte ein neues Ticket erstellen.");

            var message = new TicketDialogue
            {
                TicketId = ticketId,
                AuthorUserId = userId,
                Text = dto.Text.Trim(),
                IsInternal = IsStaff && dto.IsInternal,
                CreatedAt = DateTime.UtcNow
            };

            _context.TicketDialogue.Add(message);

            ticket.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var email = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => u.Email)
                .FirstOrDefaultAsync() ?? string.Empty;

            return Ok(new DialogueResponseDto
            {
                Id = message.Id,
                TicketId = message.TicketId,
                AuthorUserId = message.AuthorUserId,
                AuthorEmail = email,
                Text = message.Text,
                IsInternal = message.IsInternal,
                CreatedAt = message.CreatedAt
            });
        }
    }
}

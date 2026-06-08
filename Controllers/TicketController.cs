using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TicketApplication.Data;
using TicketApplication.DTOs;
using TicketApplication.Models;
using Microsoft.EntityFrameworkCore;

namespace TicketApplication.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TicketController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TicketController(ApplicationDbContext context)
        {
            _context = context;
        }

        // POST /api/ticket  -> Neues Ticket anlegen
        // Jeder eingeloggte User darf das. CreatedByUserId wird aus dem JWT
        // gelesen, NICHT vom Client geschickt - sonst koennte man Tickets im
        // Namen anderer User anlegen.
        [HttpPost]
        public async Task<ActionResult<TicketResponseDto>> Create(CreateTicketDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            int? additionalUserId1 = null;
            int? additionalUserId2 = null;
            int? additionalUserId3 = null;
            if (dto.AssignedUserMail1 != null)
            {
                additionalUserId1 = await _context.Users
                    .Where(u => u.Email == dto.AssignedUserMail1)
                    .Select(u => u.Id)
                    .FirstAsync();
            }
            if (dto.AssignedUserMail2 != null)
            {
                additionalUserId2 = await _context.Users
                    .Where(u => u.Email == dto.AssignedUserMail2)
                    .Select(u => u.Id)
                    .FirstAsync();
            }
            if (dto.AssignedUserMail3 != null)
            {
                additionalUserId3 = await _context.Users
                    .Where(u => u.Email == dto.AssignedUserMail3)
                    .Select(u => u.Id)
                    .FirstAsync();
            }

            int departmentId;
            departmentId = await _context.Departments
                .Where(d => d.Name == dto.DepartmentName)
                .Select(d => d.Id)
                .FirstAsync();
            var subjectExists = await _context.Subjects
                .AnyAsync(s => s.Title == dto.SubjectName);
            int subjectId;
            if (subjectExists)
            {
                subjectId = await _context.Subjects
                    .Where(s => s.Title == dto.SubjectName)
                    .Select(s => s.Id)
                    .FirstAsync();
            }
            else 
            {
                var subject = new Subject
                {
                    Title = dto.SubjectName,
                    DepartmentId = departmentId,
                    IsVerified = false
                };
                _context.Subjects.Add(subject);
                await _context.SaveChangesAsync();
                subjectId = subject.Id;
            }

            var ticket = new Ticket
            {
                Priority = dto.Priority,
                Title = dto.Title,
                Description = dto.Description,
                ExpectedResult = dto.ExpectedResult,
                ActualResult = dto.ActualResult,
                AgreedBilling = dto.AgreedBilling,
                AgreedAGB = dto.AgreedAGB,
                Status = TicketStatus.Open,
                AdditionalUserId1 = additionalUserId1,
                AdditionalUserId2 = additionalUserId2,
                AdditionalUserId3 = additionalUserId3,
                CreatedByUserId = userId,
                DepartmentId = departmentId,
                SubjectId = subjectId,
                AssignedToId = null,       // Zuweisung kommt in Stufe 3 ueber Tags
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow

            };

            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetOne), new { id = ticket.Id }, ToDto(ticket));
        }

        // GET /api/ticket  -> Liste der Tickets
        // User: sieht nur eigene. Support/Admin: sieht alle.
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TicketResponseDto>>> GetAll()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var role = User.FindFirstValue(ClaimTypes.Role);

            IQueryable<Ticket> query = _context.Tickets;

            if (role != "Admin" && role != "Support")
            {
                query = query.Where(t => t.CreatedByUserId == userId);
            }

            var tickets = await query
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return Ok(tickets.Select(ToDto));
        }

        // GET /api/ticket/{id}  -> Ein einzelnes Ticket
        [HttpGet("{id}")]
        public async Task<ActionResult<TicketResponseDto>> GetOne(int id)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null) return NotFound();

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var role = User.FindFirstValue(ClaimTypes.Role);

            // Normale User duerfen nur eigene Tickets sehen.
            if (role != "Admin" && role != "Support" && ticket.CreatedByUserId != userId)
                return Forbid();

            return Ok(ToDto(ticket));
        }

        // PUT /api/ticket/{id}  -> Workflow-Felder aendern
        // Nur Support/Admin. Title und Description sind nicht im DTO -
        // also unveraenderlich.
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin, Support")]
        public async Task<IActionResult> Update(int id, UpdateTicketDto dto)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null) return NotFound();

            bool changed = false;

            if (dto.Status.HasValue && ticket.Status != dto.Status.Value)
            {
                ticket.Status = dto.Status.Value;
                // Wenn Ticket geschlossen wird, ClosedAt setzen.
                // Bei Reopen (Stufe 4) wird ClosedAt wieder geleert.
                ticket.ClosedAt = dto.Status.Value == TicketStatus.Closed
                    ? DateTime.UtcNow
                    : null;
                changed = true;
            }

            if (dto.Priority.HasValue && ticket.Priority != dto.Priority.Value)
            {
                ticket.Priority = dto.Priority.Value;
                changed = true;
            }

            if (dto.AssignedToUserId.HasValue)
            {
                // Pruefen ob der Zuzuweisende existiert und Support/Admin ist.
                var assignee = await _context.Users.FindAsync(dto.AssignedToUserId.Value);
                if (assignee == null || !assignee.IsActive || !assignee.IsActivated)
                    return BadRequest("Zugewiesener User existiert nicht oder ist nicht aktiv.");
                if (assignee.Role != UserRole.Admin && assignee.Role != UserRole.Support)
                    return BadRequest("Nur Admin- oder Support-User koennen zugewiesen werden.");

                ticket.AssignedToId = dto.AssignedToUserId.Value;
                changed = true;
            }

            if (changed)
            {
                ticket.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return NoContent();
        }

        // Hilfsmethode: Ticket-Entity -> Response-DTO
        private static TicketResponseDto ToDto(Ticket t) => new TicketResponseDto
        {
            Id = t.Id,
            Title = t.Title,
            Description = t.Description,
            Status = t.Status.ToString(),
            Priority = t.Priority.ToString(),
            CreatedByUserId = t.CreatedByUserId,
            AssignedToUserId = t.AssignedToId,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt,
            ClosedAt = t.ClosedAt
        };
    }
}

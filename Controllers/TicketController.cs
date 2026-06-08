using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Sockets;
using System.Security.Claims;
using TicketApplication.Data;
using TicketApplication.DTOs;
using TicketApplication.Models;

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

            // TicketTransactions schreiben (nur später veränderbare Ticket Infos)
            var transaction = new TicketTransaction
            {
                TicketId = ticket.Id,
                ResponsibleUserId = userId,
                AdditionalUserId1 = additionalUserId1,
                AdditionalUserId2 = additionalUserId2,
                AdditionalUserId3 = additionalUserId3,
                DepartmentId = departmentId,
                SubjectId = subjectId,
                AssignedToId = null,
                UpdatedAt = DateTime.UtcNow,
                ClosedAt = null,
                OpenedAt = null,

                TransactionId = await _context.TicketTransactions
                    .Where(t => t.TicketId == ticket.Id)
                    .CountAsync()
            };
            _context.TicketTransactions.Add(transaction);
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

        // PATCH /api/ticket/{id}  -> Ticket aktualisieren (was leer ist, wird beibehalten).
        // Komplett überarbeiten!!!!!
        // Rolle entscheidet darüber, was geändert werden darf
        [HttpPatch("{id}")]
        public async Task<IActionResult> Update(int id, UpdateTicketDto dto)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var role = User.FindFirstValue(ClaimTypes.Role);
            if (ticket == null) return NotFound();
            // Darf das Ticket geupdated werden?
            if (role != "Admin" && role != "Support" && ticket.CreatedByUserId != userId)
                return Forbid();

            bool changed = false;

            if (dto.Priority != null)
            {
                ticket.Priority = dto.Priority.Value;
                changed = true;
            }

            if ((role == "Admin" || role == "Support") && dto.AssignedToUserMail != null)
            {
                ticket.AssignedToId = await _context.Users
                    .Where(u => u.Email == dto.AssignedToUserMail)
                    .Select(u => u.Id)
                    .FirstAsync();
                changed = true;
            }

            int? additionalUserId1 = null;
            int? additionalUserId2 = null;
            int? additionalUserId3 = null;
            if (dto.AssignedUserMail1 != null)
            {
                additionalUserId1 = await _context.Users
                    .Where(u => u.Email == dto.AssignedUserMail1)
                    .Select(u => u.Id)
                    .FirstAsync();
                ticket.AdditionalUserId1 = additionalUserId1;
                changed = true;
            }
            if (dto.AssignedUserMail2 != null)
            {
                additionalUserId2 = await _context.Users
                    .Where(u => u.Email == dto.AssignedUserMail2)
                    .Select(u => u.Id)
                    .FirstAsync();
                ticket.AdditionalUserId2 = additionalUserId2;
                changed = true;
            }
            if (dto.AssignedUserMail3 != null)
            {
                additionalUserId3 = await _context.Users
                    .Where(u => u.Email == dto.AssignedUserMail3)
                    .Select(u => u.Id)
                    .FirstAsync();
                ticket.AdditionalUserId3 = additionalUserId3;
                changed = true;
            }
            int? departmentId = null;
            if (dto.DepartmentName != null)
            {
                departmentId = await _context.Departments
                    .Where(d => d.Name == dto.DepartmentName)
                    .Select(d => d.Id)
                    .FirstAsync();
                ticket.DepartmentId = departmentId.Value;
                changed = true;
            }
            int? subjectId = null;
            if (dto.SubjectName != null)
            {
                var subjectExists = await _context.Subjects
                    .AnyAsync(s => s.Title == dto.SubjectName);
                
                if (subjectExists)
                {
                    subjectId = await _context.Subjects
                        .Where(s => s.Title == dto.SubjectName)
                        .Select(s => s.Id)
                        .FirstAsync();
                    ticket.SubjectId = subjectId.Value;
                }
                else
                {
                    var subject = new Subject
                    {
                        Title = dto.SubjectName,
                        DepartmentId = await _context.Departments
                            .Where(d => d.Name == dto.DepartmentName)
                            .Select(d => d.Id)
                            .FirstAsync(),
                        IsVerified = false
                    };
                    _context.Subjects.Add(subject);
                    ticket.SubjectId = subject.Id;
                    subjectId = subject.Id;
                }
                changed = true;
            }


            if (changed)
            {
                ticket.UpdatedAt = DateTime.UtcNow;

                // TicketTransactions schreiben (nur später veränderbare Ticket Infos)
                var transaction = new TicketTransaction
                {
                    TicketId = ticket.Id,
                    ResponsibleUserId = userId,
                    AssignedToId = ticket.AssignedToId,
                    UpdatedAt = DateTime.UtcNow,
                    ClosedAt = null,
                    OpenedAt = null,

                    TransactionId = await _context.TicketTransactions
                        .Where(t => t.TicketId == ticket.Id)
                        .CountAsync()
                };
                if (departmentId != null)
                {
                    transaction.DepartmentId = departmentId.Value;
                }
                if (subjectId != null)
                {
                    transaction.SubjectId = subjectId.Value;
                }

                if (additionalUserId1 != null)
                {
                    transaction.AdditionalUserId1 = additionalUserId1;
                }
                if (additionalUserId2 != null)
                {
                    transaction.AdditionalUserId2 = additionalUserId2;
                }
                if (additionalUserId3 != null)
                {
                    transaction.AdditionalUserId3 = additionalUserId3;
                }

                _context.TicketTransactions.Add(transaction);
                await _context.SaveChangesAsync();
                return Ok();
                ;
            }
        return NoContent();
        }

        // PATCH /api/ticket/{id}/status
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, UpdateTicketStatusDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var role = User.FindFirstValue(ClaimTypes.Role);
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null) return NotFound();

            if (role != "Admin" && role != "Support" && ticket.CreatedByUserId != userId)
            {
                return Forbid();
            }

            if (ticket.Status == dto.Status)
                return Ok();
                        
            ticket.Status = dto.Status;
            if (dto.Status == TicketStatus.Open)
            { 
                ticket.OpenedAt =  DateTime.UtcNow;
                ticket.UpdatedAt = DateTime.UtcNow;
            }
            else if (dto.Status == TicketStatus.Closed)
            {
                ticket.ClosedAt = DateTime.UtcNow;
                ticket.UpdatedAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();
            return Ok();
            
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

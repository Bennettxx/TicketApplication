using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        // -----------------------------------------------------------------
        // Hilfsmethoden für die Rolle/Identität aus dem JWT.
        // Niemals vom Client glauben – immer aus dem Token lesen.
        // -----------------------------------------------------------------
        private int CurrentUserId =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        private bool IsStaff =>
            User.IsInRole("Admin") || User.IsInRole("Support");

        // -----------------------------------------------------------------
        // POST /api/ticket  -> Neues Ticket anlegen
        // NUR die Rolle "User" darf Tickets erstellen (Admin/Support nicht).
        // CreatedByUserId kommt aus dem JWT, NICHT vom Client.
        // -----------------------------------------------------------------
        [HttpPost]
        [Authorize(Roles = "User")]
        public async Task<ActionResult<TicketResponseDto>> Create(CreateTicketDto dto)
        {
            var userId = CurrentUserId;

            // Optionale Zusatz-Kontakte (per E-Mail) in IDs auflösen.
            // Existenz wurde bereits im DTO via [ExistsInColumn] geprüft.
            int? additionalUserId1 = await ResolveUserIdOrNull(dto.AssignedUserMail1);
            int? additionalUserId2 = await ResolveUserIdOrNull(dto.AssignedUserMail2);
            int? additionalUserId3 = await ResolveUserIdOrNull(dto.AssignedUserMail3);

            // Abteilung -> Id (Existenz im DTO geprüft).
            int departmentId = await _context.Departments
                .Where(d => d.Name == dto.DepartmentName)
                .Select(d => d.Id)
                .FirstAsync();

            // Subject: existiert es schon? Sonst neu (unverifiziert) anlegen.
            int subjectId = await _context.Subjects
                .Where(s => s.Title == dto.SubjectName)
                .Select(s => (int?)s.Id)
                .FirstOrDefaultAsync() ?? 0;

            if (subjectId == 0)
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
                AssignedToId = null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync(); // erst speichern, damit ticket.Id existiert

            // Erste Transaktion (Audit-Eintrag) schreiben.
            await WriteTransaction(ticket, userId);
            await _context.SaveChangesAsync();

            var result = await ProjectTickets(_context.Tickets.Where(t => t.Id == ticket.Id)).FirstAsync();
            return CreatedAtAction(nameof(GetOne), new { id = ticket.Id }, result);
        }

        // -----------------------------------------------------------------
        // GET /api/ticket  -> Liste der Tickets (mit Suche & Filter)
        // User: nur eigene. Support/Admin: alle (für Kanban/Übersicht).
        //
        // Optionale Query-Parameter (alle kombinierbar):
        //   q            Volltext in Titel/Beschreibung
        //   status       0=Offen, 1=In Bearbeitung, 2=Geschlossen
        //   priority     0=Low, 1=Medium, 2=High
        //   departmentId Abteilungs-Id
        //   assignedTo   "me" (mir zugewiesen) oder "none" (nicht zugewiesen) – nur Staff
        // -----------------------------------------------------------------
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TicketResponseDto>>> GetAll(
            [FromQuery] string? q,
            [FromQuery] int? status,
            [FromQuery] int? priority,
            [FromQuery] int? departmentId,
            [FromQuery] string? assignedTo)
        {
            IQueryable<Ticket> query = _context.Tickets;

            // Sichtbarkeit: normale User sehen nur eigene Tickets.
            if (!IsStaff)
                query = query.Where(t => t.CreatedByUserId == CurrentUserId);

            // --- Filter ---
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(t => t.Title.Contains(term) || t.Description.Contains(term));
            }
            if (status is >= 0 and <= 2)
                query = query.Where(t => (int)t.Status == status);
            if (priority is >= 0 and <= 2)
                query = query.Where(t => (int)t.Priority == priority);
            if (departmentId is > 0)
                query = query.Where(t => t.DepartmentId == departmentId);

            // Zuweisungsfilter nur für Staff sinnvoll.
            if (IsStaff && !string.IsNullOrWhiteSpace(assignedTo))
            {
                if (assignedTo == "me")
                    query = query.Where(t => t.AssignedToId == CurrentUserId);
                else if (assignedTo == "none")
                    query = query.Where(t => t.AssignedToId == null);
            }

            var tickets = await ProjectTickets(query.OrderByDescending(t => t.CreatedAt))
                .ToListAsync();

            return Ok(tickets);
        }

        // -----------------------------------------------------------------
        // GET /api/ticket/{id}  -> Ein einzelnes Ticket
        // -----------------------------------------------------------------
        [HttpGet("{id}")]
        public async Task<ActionResult<TicketResponseDto>> GetOne(int id)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null) return NotFound();

            // Normale User dürfen nur eigene Tickets sehen.
            if (!IsStaff && ticket.CreatedByUserId != CurrentUserId)
                return Forbid();

            var dto = await ProjectTickets(_context.Tickets.Where(t => t.Id == id)).FirstAsync();
            return Ok(dto);
        }

        // -----------------------------------------------------------------
        // PATCH /api/ticket/{id}  -> Ticket aktualisieren (Felder optional).
        // Rolle entscheidet, was geändert werden darf.
        // -----------------------------------------------------------------
        [HttpPatch("{id}")]
        public async Task<IActionResult> Update(int id, UpdateTicketDto dto)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null) return NotFound();

            var userId = CurrentUserId;
            if (!IsStaff && ticket.CreatedByUserId != userId)
                return Forbid();

            bool changed = false;

            if (dto.Priority != null)
            {
                ticket.Priority = dto.Priority.Value;
                changed = true;
            }

            // Zuweisung darf nur Staff ändern.
            if (IsStaff && dto.AssignedToUserMail != null)
            {
                ticket.AssignedToId = await ResolveUserIdOrNull(dto.AssignedToUserMail);
                changed = true;
            }

            var add1 = await ResolveUserIdOrNull(dto.AssignedUserMail1);
            if (add1 != null) { ticket.AdditionalUserId1 = add1; changed = true; }
            var add2 = await ResolveUserIdOrNull(dto.AssignedUserMail2);
            if (add2 != null) { ticket.AdditionalUserId2 = add2; changed = true; }
            var add3 = await ResolveUserIdOrNull(dto.AssignedUserMail3);
            if (add3 != null) { ticket.AdditionalUserId3 = add3; changed = true; }

            if (dto.DepartmentName != null)
            {
                var depId = await _context.Departments
                    .Where(d => d.Name == dto.DepartmentName)
                    .Select(d => (int?)d.Id)
                    .FirstOrDefaultAsync();
                if (depId == null) return BadRequest("Abteilung nicht gefunden.");
                ticket.DepartmentId = depId.Value;
                changed = true;
            }

            if (dto.SubjectName != null)
            {
                var subId = await _context.Subjects
                    .Where(s => s.Title == dto.SubjectName)
                    .Select(s => (int?)s.Id)
                    .FirstOrDefaultAsync();
                if (subId == null)
                {
                    var subject = new Subject
                    {
                        Title = dto.SubjectName,
                        DepartmentId = ticket.DepartmentId,
                        IsVerified = false
                    };
                    _context.Subjects.Add(subject);
                    await _context.SaveChangesAsync();
                    subId = subject.Id;
                }
                ticket.SubjectId = subId.Value;
                changed = true;
            }

            if (!changed) return NoContent();

            ticket.UpdatedAt = DateTime.UtcNow;
            await WriteTransaction(ticket, userId);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // -----------------------------------------------------------------
        // PATCH /api/ticket/{id}/status  -> Status ändern (Kanban Drag&Drop).
        // -----------------------------------------------------------------
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, UpdateTicketStatusDto dto)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null) return NotFound();

            var userId = CurrentUserId;
            if (!IsStaff && ticket.CreatedByUserId != userId)
                return Forbid();

            if (ticket.Status == dto.Status)
                return NoContent(); // nichts zu tun

            ticket.Status = dto.Status;
            ticket.UpdatedAt = DateTime.UtcNow;

            switch (dto.Status)
            {
                case TicketStatus.InProgress:
                    // Erstes Mal in Bearbeitung -> Startzeitpunkt festhalten.
                    ticket.OpenedAt ??= DateTime.UtcNow;
                    ticket.ClosedAt = null;
                    break;
                case TicketStatus.Closed:
                    ticket.ClosedAt = DateTime.UtcNow;
                    break;
                case TicketStatus.Open:
                    // Wieder geöffnet -> Schließzeit zurücksetzen.
                    ticket.ClosedAt = null;
                    break;
            }

            await WriteTransaction(ticket, userId);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // -----------------------------------------------------------------
        // PATCH /api/ticket/{id}/assign  -> Ticket einem Bearbeiter zuweisen.
        // Nur Staff. AssignToEmail = null entfernt die Zuweisung.
        // -----------------------------------------------------------------
        [HttpPatch("{id}/assign")]
        [Authorize(Roles = "Admin,Support")]
        public async Task<IActionResult> Assign(int id, AssignTicketDto dto)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null) return NotFound();

            if (string.IsNullOrWhiteSpace(dto.AssignToEmail))
            {
                ticket.AssignedToId = null;
            }
            else
            {
                var uid = await _context.Users
                    .Where(u => u.Email == dto.AssignToEmail && u.IsActive)
                    .Select(u => (int?)u.Id)
                    .FirstOrDefaultAsync();
                if (uid == null) return BadRequest("Bearbeiter nicht gefunden oder inaktiv.");
                ticket.AssignedToId = uid;
            }

            ticket.UpdatedAt = DateTime.UtcNow;
            await WriteTransaction(ticket, CurrentUserId);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // =================================================================
        // PRIVATE HILFSMETHODEN
        // =================================================================

        // E-Mail -> User.Id, oder null wenn E-Mail leer/null.
        private async Task<int?> ResolveUserIdOrNull(string? email)
        {
            if (string.IsNullOrWhiteSpace(email)) return null;
            return await _context.Users
                .Where(u => u.Email == email)
                .Select(u => (int?)u.Id)
                .FirstOrDefaultAsync();
        }

        // Schreibt einen Audit-Eintrag (TicketTransaction) für den aktuellen
        // Stand des Tickets. TransactionId ist fortlaufend pro Ticket.
        private async Task WriteTransaction(Ticket ticket, int responsibleUserId)
        {
            var nextTransactionId = await _context.TicketTransactions
                .Where(t => t.TicketId == ticket.Id)
                .CountAsync();

            _context.TicketTransactions.Add(new TicketTransaction
            {
                TicketId = ticket.Id,
                TransactionId = nextTransactionId,
                ResponsibleUserId = responsibleUserId,
                AssignedToId = ticket.AssignedToId,
                Status = ticket.Status,
                AdditionalUserId1 = ticket.AdditionalUserId1,
                AdditionalUserId2 = ticket.AdditionalUserId2,
                AdditionalUserId3 = ticket.AdditionalUserId3,
                DepartmentId = ticket.DepartmentId,
                SubjectId = ticket.SubjectId,
                UpdatedAt = ticket.UpdatedAt,
                ClosedAt = ticket.ClosedAt,
                OpenedAt = ticket.OpenedAt
            });
        }

        // Projiziert Ticket-Entities auf das angereicherte Response-DTO
        // (inkl. Namen, E-Mails und Summe der erfassten Minuten).
        private IQueryable<TicketResponseDto> ProjectTickets(IQueryable<Ticket> source)
        {
            return from t in source
                   join dep in _context.Departments on t.DepartmentId equals dep.Id into depJoin
                   from dep in depJoin.DefaultIfEmpty()
                   join sub in _context.Subjects on t.SubjectId equals sub.Id into subJoin
                   from sub in subJoin.DefaultIfEmpty()
                   join cu in _context.Users on t.CreatedByUserId equals cu.Id into cuJoin
                   from cu in cuJoin.DefaultIfEmpty()
                   join au in _context.Users on t.AssignedToId equals au.Id into auJoin
                   from au in auJoin.DefaultIfEmpty()
                   select new TicketResponseDto
                   {
                       Id = t.Id,
                       Title = t.Title,
                       Description = t.Description,
                       ExpectedResult = t.ExpectedResult,
                       ActualResult = t.ActualResult,
                       Status = t.Status.ToString(),
                       StatusCode = (int)t.Status,
                       Priority = t.Priority.ToString(),
                       PriorityCode = (int)t.Priority,
                       CreatedByUserId = t.CreatedByUserId,
                       CreatedByEmail = cu != null ? cu.Email : string.Empty,
                       AssignedToUserId = t.AssignedToId,
                       AssignedToEmail = au != null ? au.Email : null,
                       DepartmentId = t.DepartmentId,
                       DepartmentName = dep != null ? dep.Name : string.Empty,
                       SubjectId = t.SubjectId,
                       SubjectName = sub != null ? sub.Title : string.Empty,
                       TotalMinutes = _context.TicketTimeEntries
                           .Where(e => e.TicketId == t.Id)
                           .Sum(e => (int?)e.Minutes) ?? 0,
                       CreatedAt = t.CreatedAt,
                       UpdatedAt = t.UpdatedAt,
                       ClosedAt = t.ClosedAt
                   };
        }
    }
}

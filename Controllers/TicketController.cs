using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TicketApplication.Data;
using TicketApplication.DTOs;
using TicketApplication.Models;
using TicketApplication.Services;

namespace TicketApplication.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TicketController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly MailService _mail;
        private readonly MailQueue _mailQueue;
        private readonly LogService _log;

        public TicketController(ApplicationDbContext context, MailService mail, MailQueue mailQueue, LogService log)
        {
            _context = context;
            _mail = mail;
            _mailQueue = mailQueue;
            _log = log;
        }

        // user-id aus dem jwt, nie vom client
        private int CurrentUserId =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // admin oder support
        private bool IsStaff =>
            User.IsInRole("Admin") || User.IsInRole("Support");

        // POST api/ticket — neues ticket, nur rolle user
        [HttpPost]
        [Authorize(Roles = "User")]
        public async Task<ActionResult<TicketResponseDto>> Create(CreateTicketDto dto)
        {
            var userId = CurrentUserId;

            // zusatzkontakte per mail auflösen, existenz bereits im dto geprüft
            int? additionalUserId1 = await ResolveUserIdOrNull(dto.AssignedUserMail1);
            int? additionalUserId2 = await ResolveUserIdOrNull(dto.AssignedUserMail2);
            int? additionalUserId3 = await ResolveUserIdOrNull(dto.AssignedUserMail3);

            int departmentId = await _context.Departments
                .Where(d => d.Name == dto.DepartmentName)
                .Select(d => d.Id)
                .FirstAsync();

            // subject nachschlagen, sonst unverifiziert neu anlegen
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
                ReferenceTicketId = dto.ReferenceTicketId,
                ReferenceComment = dto.ReferenceComment?.Trim() ?? string.Empty,
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
            await _context.SaveChangesAsync(); // erst jetzt gibt es ticket.Id

            await WriteTransaction(ticket, userId);
            await _context.SaveChangesAsync();

            _log.Info(LogBereich.Tickets, $"Ticket #{ticket.Id} erstellt von User {userId}: {ticket.Title}");

            var result = await ProjectTickets(_context.Tickets.Where(t => t.Id == ticket.Id)).FirstAsync();
            return CreatedAtAction(nameof(GetOne), new { id = ticket.Id }, result);
        }

        // GET api/ticket — liste mit suche/filter
        // user sieht nur eigene, staff alles
        // filter: q, status, activeOnly, priority, departmentId, assignedTo (me/none), createdFrom, createdTo
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TicketResponseDto>>> GetAll(
            [FromQuery] string? q,
            [FromQuery] int? status,
            [FromQuery] bool activeOnly,
            [FromQuery] int? priority,
            [FromQuery] int? departmentId,
            [FromQuery] string? assignedTo,
            [FromQuery] DateTime? createdFrom,
            [FromQuery] DateTime? createdTo)
        {
            IQueryable<Ticket> query = _context.Tickets;

            if (!IsStaff)
                query = query.Where(t => t.CreatedByUserId == CurrentUserId);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(t => t.Title.Contains(term) || t.Description.Contains(term));
            }
            if (activeOnly)
                query = query.Where(t => t.Status != TicketStatus.Closed);
            if (status is >= 0 and <= 2)
                query = query.Where(t => (int)t.Status == status);
            if (priority is >= 0 and <= 2)
                query = query.Where(t => (int)t.Priority == priority);
            if (departmentId is > 0)
                query = query.Where(t => t.DepartmentId == departmentId);

            if (createdFrom.HasValue)
                query = query.Where(t => t.CreatedAt >= createdFrom.Value.Date);
            if (createdTo.HasValue)
                query = query.Where(t => t.CreatedAt < createdTo.Value.Date.AddDays(1));

            // zuweisungsfilter nur für staff
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

        // GET api/ticket/{id} — einzelnes ticket
        [HttpGet("{id}")]
        public async Task<ActionResult<TicketResponseDto>> GetOne(int id)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null) return NotFound();

            if (!IsStaff && ticket.CreatedByUserId != CurrentUserId)
                return Forbid();

            var dto = await ProjectTickets(_context.Tickets.Where(t => t.Id == id)).FirstAsync();
            return Ok(dto);
        }

        // PATCH api/ticket/{id} — teilupdate, rolle bestimmt was erlaubt ist
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

            // zuweisung nur staff
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

        // PATCH api/ticket/{id}/status — statuswechsel (kanban drag&drop)
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, UpdateTicketStatusDto dto)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null) return NotFound();

            var userId = CurrentUserId;
            if (!IsStaff && ticket.CreatedByUserId != userId)
                return Forbid();

            if (ticket.Status == dto.Status)
                return NoContent();

            // reopen läuft nur über den reopen-endpunkt (pflicht-nachricht)
            if (ticket.Status == TicketStatus.Closed && dto.Status != TicketStatus.Closed)
                return BadRequest("Zum Wiedereröffnen bitte den Wiedereröffnen-Vorgang mit Pflicht-Nachricht nutzen.");

            ticket.Status = dto.Status;
            ticket.UpdatedAt = DateTime.UtcNow;

            switch (dto.Status)
            {
                case TicketStatus.InProgress:
                    ticket.OpenedAt ??= DateTime.UtcNow;
                    ticket.ClosedAt = null;
                    break;
                case TicketStatus.Closed:
                    ticket.ClosedAt = DateTime.UtcNow;
                    break;
                case TicketStatus.Open:
                    ticket.ClosedAt = null;
                    break;
            }

            await WriteTransaction(ticket, userId);
            await _context.SaveChangesAsync();

            _log.Info(LogBereich.Tickets, $"Ticket #{ticket.Id} Status -> {ticket.Status} durch User {userId}");
            return NoContent();
        }

        // POST api/ticket/{id}/reopen — geschlossenes ticket wieder öffnen, nachricht ist pflicht
        [HttpPost("{id}/reopen")]
        public async Task<IActionResult> Reopen(int id, ReopenTicketDto dto)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null) return NotFound();

            var userId = CurrentUserId;
            if (!IsStaff && ticket.CreatedByUserId != userId)
                return Forbid();

            if (ticket.Status != TicketStatus.Closed)
                return BadRequest("Nur geschlossene Tickets können wiedereröffnet werden.");

            ticket.Status = TicketStatus.Open;
            ticket.ClosedAt = null;
            ticket.OpenedAt = DateTime.UtcNow;
            ticket.UpdatedAt = DateTime.UtcNow;

            _context.TicketDialogue.Add(new TicketDialogue
            {
                TicketId = ticket.Id,
                AuthorUserId = userId,
                Text = dto.Message.Trim(),
                IsInternal = false,
                CreatedAt = DateTime.UtcNow
            });

            await WriteTransaction(ticket, userId);
            await _context.SaveChangesAsync();

            _log.Info(LogBereich.Tickets, $"Ticket #{ticket.Id} wiedereröffnet durch User {userId}");

            // beteiligte per mail informieren (außer dem auslöser)
            if (await _mail.IsConfiguredAsync())
            {
                var empfaengerIds = new List<int?> { ticket.CreatedByUserId, ticket.AssignedToId }
                    .Where(x => x != null && x != userId)
                    .Select(x => x!.Value)
                    .Distinct()
                    .ToList();
                if (empfaengerIds.Count > 0)
                {
                    var mails = await _context.Users
                        .Where(u => empfaengerIds.Contains(u.Id) && u.IsActive)
                        .Select(u => u.Email)
                        .ToListAsync();
                    var link = $"{Request.Scheme}://{Request.Host}/ticket.html?id={ticket.Id}";
                    foreach (var m in mails)
                    {
                        _mailQueue.Enqueue(m,
                            $"Ticket #{ticket.Id}: Wiedereröffnet - {ticket.Title}",
                            $"Das Ticket #{ticket.Id} \"{ticket.Title}\" wurde wiedereröffnet.\n\n" +
                            $"Begründung: {dto.Message.Trim()}\n\nZum Ticket: {link}");
                    }
                }
            }

            return NoContent();
        }

        // PATCH api/ticket/{id}/assign — bearbeiter setzen/entfernen, nur staff
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

            _log.Info(LogBereich.Tickets, $"Ticket #{ticket.Id} Zuweisung -> {(ticket.AssignedToId?.ToString() ?? "niemand")} durch User {CurrentUserId}");
            return NoContent();
        }

        // POST api/ticket/{id}/read — lesezeitpunkt setzen, entfernt "neue antwort"-markierung
        [HttpPost("{id}/read")]
        public async Task<IActionResult> MarkRead(int id)
        {
            if (!await _context.Tickets.AnyAsync(t => t.Id == id))
                return NotFound();

            var userId = CurrentUserId;
            var eintrag = await _context.TicketReads
                .FirstOrDefaultAsync(r => r.TicketId == id && r.UserId == userId);

            if (eintrag == null)
            {
                _context.TicketReads.Add(new TicketRead
                {
                    TicketId = id,
                    UserId = userId,
                    LastReadAt = DateTime.UtcNow
                });
            }
            else
            {
                eintrag.LastReadAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // mail -> user-id, null wenn leer oder unbekannt
        private async Task<int?> ResolveUserIdOrNull(string? email)
        {
            if (string.IsNullOrWhiteSpace(email)) return null;
            return await _context.Users
                .Where(u => u.Email == email)
                .Select(u => (int?)u.Id)
                .FirstOrDefaultAsync();
        }

        // audit-eintrag mit aktuellem ticketstand, transactionid fortlaufend pro ticket
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

        // tickets -> response-dto inkl. namen, mails, minuten und ungelesen-flag
        private IQueryable<TicketResponseDto> ProjectTickets(IQueryable<Ticket> source)
        {
            int currentUserId = CurrentUserId;
            bool isStaff = IsStaff;

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
                       ReferenceTicketId = t.ReferenceTicketId,
                       ReferenceComment = t.ReferenceComment,
                       TotalMinutes = _context.TicketTimeEntries
                           .Where(e => e.TicketId == t.Id)
                           .Sum(e => (int?)e.Minutes) ?? 0,
                       // ungelesene fremde antwort, nur für beteiligte; interne notizen zählen nur für staff
                       HasUnreadReply =
                           (t.CreatedByUserId == currentUserId || t.AssignedToId == currentUserId) &&
                           _context.TicketDialogue.Any(d =>
                               d.TicketId == t.Id &&
                               d.AuthorUserId != currentUserId &&
                               (isStaff || !d.IsInternal) &&
                               d.CreatedAt > (_context.TicketReads
                                   .Where(r => r.TicketId == t.Id && r.UserId == currentUserId)
                                   .Select(r => (DateTime?)r.LastReadAt)
                                   .FirstOrDefault() ?? DateTime.MinValue)),
                       CreatedAt = t.CreatedAt,
                       UpdatedAt = t.UpdatedAt,
                       ClosedAt = t.ClosedAt
                   };
        }
    }
}

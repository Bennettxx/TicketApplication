using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketApplication.Data;
using TicketApplication.DTOs;

namespace TicketApplication.Controllers
{
    // Statistische Auswertungen – nur für Admins.
    // Beantwortet: Wer hat wie viele Tickets bearbeitet? Welcher Kunde hat
    // wie viel Bearbeitungszeit verursacht?
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class StatisticsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public StatisticsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET /api/statistics/agents
        // Pro Bearbeiter (Admin/Support): zugewiesene Tickets, geschlossene
        // Tickets und insgesamt erfasste Arbeitszeit (Minuten).
        [HttpGet("agents")]
        public async Task<ActionResult<IEnumerable<AgentStatsDto>>> Agents()
        {
            // Erfasste Minuten je Bearbeiter (aus der Zeiterfassung).
            var minutesByUser = await _context.TicketTimeEntries
                .GroupBy(e => e.UserId)
                .Select(g => new { UserId = g.Key, Minutes = g.Sum(x => x.Minutes) })
                .ToDictionaryAsync(x => x.UserId, x => x.Minutes);

            // Zugewiesene Tickets (alle, unabhängig vom Status).
            var assignedByUser = await _context.Tickets
                .Where(t => t.AssignedToId != null)
                .GroupBy(t => t.AssignedToId!.Value)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.UserId, x => x.Count);

            // Geschlossene Tickets je zugewiesenem Bearbeiter.
            var closedByUser = await _context.Tickets
                .Where(t => t.AssignedToId != null && t.Status == TicketStatus.Closed)
                .GroupBy(t => t.AssignedToId!.Value)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.UserId, x => x.Count);

            // Alle Mitarbeiter (Staff) als Basis, auch wenn sie 0 Tickets haben.
            var staff = await _context.Users
                .Where(u => u.IsActive && (u.Role == UserRole.Admin || u.Role == UserRole.Support))
                .Select(u => new { u.Id, u.FirstName, u.SecondName, u.Email, u.Role })
                .ToListAsync();

            var result = staff.Select(u => new AgentStatsDto
            {
                UserId = u.Id,
                Email = u.Email,
                Name = $"{u.FirstName} {u.SecondName}".Trim(),
                Role = u.Role.ToString(),
                AssignedTicketCount = assignedByUser.GetValueOrDefault(u.Id),
                ClosedTicketCount = closedByUser.GetValueOrDefault(u.Id),
                TotalMinutes = minutesByUser.GetValueOrDefault(u.Id)
            })
            .OrderByDescending(a => a.TotalMinutes)
            .ToList();

            return Ok(result);
        }

        // GET /api/statistics/customers
        // Pro Kunde: Anzahl erstellter Tickets, davon offen, und die gesamte
        // Bearbeitungszeit aller seiner Tickets (Summe der Zeiteinträge).
        [HttpGet("customers")]
        public async Task<ActionResult<IEnumerable<CustomerStatsDto>>> Customers()
        {
            // Tickets je Kunde (Ersteller).
            var ticketsByCustomer = await _context.Tickets
                .GroupBy(t => t.CreatedByUserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    Total = g.Count(),
                    Open = g.Count(t => t.Status != TicketStatus.Closed)
                })
                .ToListAsync();

            // Bearbeitungszeit je Kunde = Summe der Zeiteinträge über alle
            // Tickets, die der Kunde erstellt hat. Wir verknüpfen Zeiteinträge
            // mit dem Ersteller des jeweiligen Tickets.
            var minutesByCustomer = await (from e in _context.TicketTimeEntries
                                           join t in _context.Tickets on e.TicketId equals t.Id
                                           group e by t.CreatedByUserId into g
                                           select new { UserId = g.Key, Minutes = g.Sum(x => x.Minutes) })
                                          .ToDictionaryAsync(x => x.UserId, x => x.Minutes);

            var userIds = ticketsByCustomer.Select(x => x.UserId).ToList();
            var users = await (from u in _context.Users
                               where userIds.Contains(u.Id)
                               join d in _context.Departments on u.DepartmentId equals d.Id into dj
                               from d in dj.DefaultIfEmpty()
                               select new { u.Id, u.FirstName, u.SecondName, u.Email, DepartmentName = d != null ? d.Name : null })
                              .ToListAsync();
            var userMap = users.ToDictionary(u => u.Id);

            var result = ticketsByCustomer.Select(c =>
            {
                userMap.TryGetValue(c.UserId, out var u);
                return new CustomerStatsDto
                {
                    UserId = c.UserId,
                    Email = u?.Email ?? $"(User {c.UserId})",
                    Name = u != null ? $"{u.FirstName} {u.SecondName}".Trim() : string.Empty,
                    DepartmentName = u?.DepartmentName ?? "(ohne Abteilung)",
                    TicketCount = c.Total,
                    OpenTicketCount = c.Open,
                    TotalMinutes = minutesByCustomer.GetValueOrDefault(c.UserId)
                };
            })
            .OrderByDescending(c => c.TotalMinutes)
            .ToList();

            return Ok(result);
        }

        // GET /api/statistics/departments
        // Kategorisiert die Bearbeitungszeit nach der Abteilung des Erstellers
        // (Kunden). Antwort: pro Abteilung Anzahl Benutzer, erstellte Tickets,
        // davon offen und gesamte Bearbeitungszeit.
        [HttpGet("departments")]
        public async Task<ActionResult<IEnumerable<DepartmentStatsDto>>> Departments()
        {
            // Stammdaten laden (Datenmengen sind überschaubar -> im Speicher
            // aggregieren ist robust und gut lesbar).
            var departments = await _context.Departments
                .Select(d => new { d.Id, d.Name })
                .ToListAsync();
            var users = await _context.Users
                .Select(u => new { u.Id, u.DepartmentId })
                .ToListAsync();
            var tickets = await _context.Tickets
                .Select(t => new { t.Id, t.CreatedByUserId, t.Status })
                .ToListAsync();
            var times = await _context.TicketTimeEntries
                .Select(e => new { e.TicketId, e.Minutes })
                .ToListAsync();

            // Zuordnungen
            var userDept = users.ToDictionary(u => u.Id, u => u.DepartmentId);
            var ticketDept = tickets.ToDictionary(
                t => t.Id,
                t => userDept.TryGetValue(t.CreatedByUserId, out var dep) ? dep : (int?)null);

            // Aggregation je Abteilung.
            // WICHTIG: Ein Dictionary erlaubt KEINEN null-Schlüssel (auch nicht
            // bei Schlüsseltyp int?). Deshalb wird "ohne Abteilung" (z.B.
            // Admin/Support ohne Abteilung) in einem SEPARATEN Bucket gehalten,
            // und das Dictionary verwendet nur echte (nicht-null) Abteilungs-Ids.
            var stats = new Dictionary<int, DepartmentStatsDto>();
            var ohneAbteilung = new DepartmentStatsDto
            {
                DepartmentId = null,
                DepartmentName = "(ohne Abteilung)"
            };

            DepartmentStatsDto Bucket(int? depId)
            {
                if (depId == null) return ohneAbteilung;

                if (!stats.TryGetValue(depId.Value, out var s))
                {
                    s = new DepartmentStatsDto
                    {
                        DepartmentId = depId.Value,
                        DepartmentName = departments.FirstOrDefault(d => d.Id == depId.Value)?.Name
                                         ?? $"#{depId.Value}"
                    };
                    stats[depId.Value] = s;
                }
                return s;
            }

            // Alle Abteilungen vorab anlegen (auch ohne Tickets/User).
            foreach (var d in departments) Bucket(d.Id);

            foreach (var u in users) Bucket(u.DepartmentId).UserCount++;

            foreach (var t in tickets)
            {
                var b = Bucket(ticketDept[t.Id]);
                b.TicketCount++;
                if (t.Status != TicketStatus.Closed) b.OpenTicketCount++;
            }

            foreach (var e in times)
            {
                if (!ticketDept.TryGetValue(e.TicketId, out var dep)) continue;
                Bucket(dep).TotalMinutes += e.Minutes;
            }

            // Ergebnis: echte Abteilungen + ("ohne Abteilung" nur wenn dort etwas anfällt).
            var result = stats.Values.ToList();
            if (ohneAbteilung.UserCount > 0 || ohneAbteilung.TicketCount > 0 || ohneAbteilung.TotalMinutes > 0)
                result.Add(ohneAbteilung);

            result = result
                .OrderByDescending(s => s.TotalMinutes)
                .ThenByDescending(s => s.TicketCount)
                .ToList();

            return Ok(result);
        }
    }
}

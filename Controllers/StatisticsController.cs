using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketApplication.Data;
using TicketApplication.DTOs;

namespace TicketApplication.Controllers
{
    // auswertungen für die statistik-seite, nur admin
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

        // zeitraum-filter: days = 30/60/... , null oder 0 = gesamt
        private static DateTime? Cutoff(int? days) =>
            days is > 0 ? DateTime.UtcNow.AddDays(-days.Value) : null;

        // GET api/statistics/agents — pro bearbeiter: zugewiesen, geschlossen, erfasste minuten
        // optional ?days=30/60 (tickets nach erstelldatum, zeiten nach arbeitsdatum)
        [HttpGet("agents")]
        public async Task<ActionResult<IEnumerable<AgentStatsDto>>> Agents([FromQuery] int? days)
        {
            var cutoff = Cutoff(days);
            var entries = _context.TicketTimeEntries.AsQueryable();
            var tickets = _context.Tickets.AsQueryable();
            if (cutoff != null)
            {
                entries = entries.Where(e => e.WorkedAt >= cutoff.Value);
                tickets = tickets.Where(t => t.CreatedAt >= cutoff.Value);
            }

            var minutesByUser = await entries
                .GroupBy(e => e.UserId)
                .Select(g => new { UserId = g.Key, Minutes = g.Sum(x => x.Minutes) })
                .ToDictionaryAsync(x => x.UserId, x => x.Minutes);

            var assignedByUser = await tickets
                .Where(t => t.AssignedToId != null)
                .GroupBy(t => t.AssignedToId!.Value)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.UserId, x => x.Count);

            var closedByUser = await tickets
                .Where(t => t.AssignedToId != null && t.Status == TicketStatus.Closed)
                .GroupBy(t => t.AssignedToId!.Value)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.UserId, x => x.Count);

            // alle staff-user als basis, auch ohne tickets
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

        // GET api/statistics/customers — pro kunde: tickets, davon offen, verursachte minuten
        // optional ?days=30/60
        [HttpGet("customers")]
        public async Task<ActionResult<IEnumerable<CustomerStatsDto>>> Customers([FromQuery] int? days)
        {
            var cutoff = Cutoff(days);
            var tickets = _context.Tickets.AsQueryable();
            var entries = _context.TicketTimeEntries.AsQueryable();
            if (cutoff != null)
            {
                tickets = tickets.Where(t => t.CreatedAt >= cutoff.Value);
                entries = entries.Where(e => e.WorkedAt >= cutoff.Value);
            }

            var ticketsByCustomer = await tickets
                .GroupBy(t => t.CreatedByUserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    Total = g.Count(),
                    Open = g.Count(t => t.Status != TicketStatus.Closed)
                })
                .ToListAsync();

            // minuten je kunde = zeiteinträge über alle seine tickets
            var minutesByCustomer = await (from e in entries
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

        // GET api/statistics/departments — bearbeitungszeit nach abteilung des erstellers
        // optional ?days=30/60
        [HttpGet("departments")]
        public async Task<ActionResult<IEnumerable<DepartmentStatsDto>>> Departments([FromQuery] int? days)
        {
            var cutoff = Cutoff(days);
            var ticketQuery = _context.Tickets.AsQueryable();
            var timeQuery = _context.TicketTimeEntries.AsQueryable();
            if (cutoff != null)
            {
                ticketQuery = ticketQuery.Where(t => t.CreatedAt >= cutoff.Value);
                timeQuery = timeQuery.Where(e => e.WorkedAt >= cutoff.Value);
            }

            // datenmengen klein, aggregation im speicher
            var departments = await _context.Departments
                .Select(d => new { d.Id, d.Name })
                .ToListAsync();
            var users = await _context.Users
                .Select(u => new { u.Id, u.DepartmentId })
                .ToListAsync();
            var tickets = await ticketQuery
                .Select(t => new { t.Id, t.CreatedByUserId, t.Status })
                .ToListAsync();
            var times = await timeQuery
                .Select(e => new { e.TicketId, e.Minutes })
                .ToListAsync();

            // zuordnung ticket -> abteilung über ALLE tickets,
            // damit zeiten im zeitraum auch auf älteren tickets zählen
            var alleTickets = await _context.Tickets
                .Select(t => new { t.Id, t.CreatedByUserId })
                .ToListAsync();

            var userDept = users.ToDictionary(u => u.Id, u => u.DepartmentId);
            var ticketDept = alleTickets.ToDictionary(
                t => t.Id,
                t => userDept.TryGetValue(t.CreatedByUserId, out var dep) ? dep : (int?)null);

            // "ohne abteilung" als eigener bucket, dictionary kann keinen null-key
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

            // alle abteilungen anlegen, auch leere
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

            // "ohne abteilung" nur ausgeben wenn dort etwas anfällt
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

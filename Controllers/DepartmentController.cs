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
    // abteilungsverwaltung (stammdaten), nur admin
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class DepartmentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly LogService _log;

        public DepartmentController(ApplicationDbContext context, LogService log)
        {
            _context = context;
            _log = log;
        }

        private string CurrentUserEmail =>
            User.FindFirstValue(ClaimTypes.Email) ?? "unbekannt";

        // GET api/department — alle abteilungen mit nutzungszählern
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DepartmentAdminDto>>> GetAll()
        {
            var list = await _context.Departments
                .OrderBy(d => d.Name)
                .Select(d => new DepartmentAdminDto
                {
                    Id = d.Id,
                    Name = d.Name,
                    UserCount = _context.Users.Count(u => u.DepartmentId == d.Id && u.IsActive),
                    TicketCount = _context.Tickets.Count(t => t.DepartmentId == d.Id),
                    SubjectCount = _context.Subjects.Count(s => s.DepartmentId == d.Id)
                })
                .ToListAsync();
            return Ok(list);
        }

        // POST api/department — neue abteilung anlegen
        [HttpPost]
        public async Task<ActionResult<DepartmentAdminDto>> Create(SaveDepartmentDto dto)
        {
            var name = dto.Name.Trim();
            bool existiert = await _context.Departments.AnyAsync(d => d.Name == name);
            if (existiert)
                return BadRequest("Eine Abteilung mit diesem Namen existiert bereits.");

            var dep = new Department { Name = name };
            _context.Departments.Add(dep);
            await _context.SaveChangesAsync();

            _log.Info(LogBereich.Einstellungen, $"Abteilung angelegt: '{name}' durch {CurrentUserEmail}.");
            return Ok(new DepartmentAdminDto { Id = dep.Id, Name = dep.Name });
        }

        // PUT api/department/{id} — abteilung umbenennen
        [HttpPut("{id}")]
        public async Task<IActionResult> Rename(int id, SaveDepartmentDto dto)
        {
            var dep = await _context.Departments.FindAsync(id);
            if (dep == null) return NotFound();

            var name = dto.Name.Trim();
            bool vergeben = await _context.Departments.AnyAsync(d => d.Name == name && d.Id != id);
            if (vergeben)
                return BadRequest("Eine Abteilung mit diesem Namen existiert bereits.");

            var alt = dep.Name;
            dep.Name = name;
            await _context.SaveChangesAsync();

            _log.Info(LogBereich.Einstellungen, $"Abteilung umbenannt: '{alt}' -> '{name}' durch {CurrentUserEmail}.");
            return NoContent();
        }

        // DELETE api/department/{id} — löschen nur wenn nichts zugeordnet ist
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var dep = await _context.Departments.FindAsync(id);
            if (dep == null) return NotFound();

            int userCount = await _context.Users.CountAsync(u => u.DepartmentId == id);
            int ticketCount = await _context.Tickets.CountAsync(t => t.DepartmentId == id);
            int subjectCount = await _context.Subjects.CountAsync(s => s.DepartmentId == id);
            int artikelCount = await _context.KnowledgeArticles.CountAsync(a => a.DepartmentId == id);

            if (userCount + ticketCount + subjectCount + artikelCount > 0)
            {
                return BadRequest(
                    $"Abteilung kann nicht gelöscht werden: {userCount} Benutzer, {ticketCount} Tickets, " +
                    $"{subjectCount} Themen und {artikelCount} Wissensartikel sind noch zugeordnet.");
            }

            _context.Departments.Remove(dep);
            await _context.SaveChangesAsync();

            _log.Info(LogBereich.Einstellungen, $"Abteilung gelöscht: '{dep.Name}' durch {CurrentUserEmail}.");
            return NoContent();
        }
    }
}

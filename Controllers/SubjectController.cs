using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketApplication.Data;
using TicketApplication.DTOs;

namespace TicketApplication.Controllers
{
    // Verwaltung der Themen (Subjects).
    // Neue Themen entstehen unverifiziert beim Ticket-Erstellen; Support/Admin
    // können sie verifizieren. Verifizierte Themen werden beim Ticket-Erstellen
    // als Auswahl (Dropdown) angeboten.
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,Support")]
    public class SubjectController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SubjectController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET /api/subject -> alle Themen (für die Verwaltung), inkl. Abteilung.
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SubjectDto>>> GetAll()
        {
            var list = await _context.Subjects
                .OrderByDescending(s => s.IsVerified ? 0 : 1) // unverifizierte oben
                .ThenBy(s => s.Title)
                .Select(s => new SubjectDto
                {
                    Id = s.Id,
                    Title = s.Title,
                    DepartmentId = s.DepartmentId,
                    IsVerified = s.IsVerified
                })
                .ToListAsync();
            return Ok(list);
        }

        // PATCH /api/subject/{id}/verify -> Thema verifizieren.
        [HttpPatch("{id}/verify")]
        public async Task<IActionResult> Verify(int id)
        {
            var subject = await _context.Subjects.FindAsync(id);
            if (subject == null) return NotFound();
            subject.IsVerified = true;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // PATCH /api/subject/{id}/unverify -> Verifizierung zurücknehmen.
        [HttpPatch("{id}/unverify")]
        public async Task<IActionResult> Unverify(int id)
        {
            var subject = await _context.Subjects.FindAsync(id);
            if (subject == null) return NotFound();
            subject.IsVerified = false;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE /api/subject/{id} -> Thema löschen (nur Admin).
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var subject = await _context.Subjects.FindAsync(id);
            if (subject == null) return NotFound();
            _context.Subjects.Remove(subject);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}

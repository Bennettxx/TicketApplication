using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketApplication.Data;
using TicketApplication.DTOs;

namespace TicketApplication.Controllers
{
    // Stammdaten/Auswahllisten für das Frontend (Abteilungen, Themen, Bearbeiter).
    // Jeder eingeloggte User darf diese Listen lesen, damit die Formulare
    // gültige Werte anbieten können (z.B. Abteilung beim Ticket-Erstellen).
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MetadataController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public MetadataController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET /api/metadata/departments -> Liste aller Abteilungen.
        // [AllowAnonymous]: wird auch auf der Registrierungsseite (ohne Login)
        // gebraucht, damit der neue User seine Abteilung wählen kann.
        // Abteilungsnamen sind nicht sensibel.
        [HttpGet("departments")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<DepartmentDto>>> Departments()
        {
            var list = await _context.Departments
                .OrderBy(d => d.Name)
                .Select(d => new DepartmentDto { Id = d.Id, Name = d.Name })
                .ToListAsync();
            return Ok(list);
        }

        // GET /api/metadata/subjects -> Liste der Themen, optional pro Abteilung.
        [HttpGet("subjects")]
        public async Task<ActionResult<IEnumerable<SubjectDto>>> Subjects([FromQuery] int? departmentId)
        {
            var query = _context.Subjects.AsQueryable();
            if (departmentId.HasValue)
                query = query.Where(s => s.DepartmentId == departmentId.Value);

            var list = await query
                .OrderBy(s => s.Title)
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

        // GET /api/metadata/agents -> Liste der Bearbeiter (Admin/Support).
        // Wird im Frontend für die Zuweisung eines Tickets gebraucht.
        // Nur Staff darf diese Liste sehen.
        [HttpGet("agents")]
        [Authorize(Roles = "Admin,Support")]
        public async Task<ActionResult<IEnumerable<UserResponseDto>>> Agents()
        {
            var list = await _context.Users
                .Where(u => u.IsActive && u.IsActivated &&
                            (u.Role == UserRole.Admin || u.Role == UserRole.Support))
                .OrderBy(u => u.Email)
                .Select(u => new UserResponseDto
                {
                    Id = u.Id,
                    FirstName = u.FirstName,
                    SecondName = u.SecondName,
                    Email = u.Email,
                    Role = u.Role.ToString(),
                    IsActivated = u.IsActivated,
                    DepartmentId = u.DepartmentId,
                    DepartmentName = _context.Departments
                        .Where(d => d.Id == u.DepartmentId)
                        .Select(d => d.Name)
                        .FirstOrDefault()
                })
                .ToListAsync();
            return Ok(list);
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TicketApplication.Data;
using TicketApplication.DTOs;
using TicketApplication.Models;

namespace TicketApplication.Controllers
{
    // Dieser Controller ist für die Verwaltung der User zuständig
    // Er bietet Endpunkte für CRUD-Operationen (Create, Read, Update, Delete) auf User-Objekten
    // Alle Endpunkte in diesem Controller haben die Basis-URL "api/user"


    // Mal gucken was wir mit den Controller machen ... er ist irgendwie zwecklos
    
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UserController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET /api/user/me  ->  Eigene Identität (Id, E-Mail, Rolle).
        // Wird vom Frontend gebraucht, um Benutzername anzuzeigen und die
        // Navigation rollenabhängig (Kanban/Statistik nur für Staff) aufzubauen.
        [HttpGet("me")]
        public async Task<ActionResult<UserResponseDto>> GetMe()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var user = await _context.Users.FindAsync(userId);
            if (user == null || !user.IsActive)
                return NotFound();

            return Ok(new UserResponseDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                SecondName = user.SecondName,
                Email = user.Email,
                Role = user.Role.ToString(),
                IsActivated = user.IsActivated,
                DepartmentId = user.DepartmentId,
                DepartmentName = await DepartmentName(user.DepartmentId)
            });
        }


        [HttpGet(Name = "GetUsers")]
        [Authorize(Roles = "Admin, Support")]
        public async Task<ActionResult<IEnumerable<UserResponseDto>>> Get()
        {
            var users = await _context.Users
                .Where(u => u.IsActive)
                // Select() projiziert jedes User-Objekt in ein UserResponseDto
                // So verlässt das PasswordHash niemals den Server
                .Select(u => new UserResponseDto
                {
                    Id = u.Id,
                    FirstName = u.FirstName,
                    SecondName = u.SecondName,
                    Email = u.Email,
                    Role = u.Role.ToString(),  // Enum → lesbarer String (z.B. "Admin")
                    IsActivated = u.IsActivated,
                    DepartmentId = u.DepartmentId,
                    DepartmentName = _context.Departments
                        .Where(d => d.Id == u.DepartmentId)
                        .Select(d => d.Name)
                        .FirstOrDefault()
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin, Support")]
        public async Task<ActionResult<UserResponseDto>> Get(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null || !user.IsActive)
                return NotFound();

            return Ok(new UserResponseDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                SecondName = user.SecondName,
                Email = user.Email,
                Role = user.Role.ToString(),
                IsActivated = user.IsActivated,
                DepartmentId = user.DepartmentId,
                DepartmentName = await DepartmentName(user.DepartmentId)
            });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<UserResponseDto>> Post(CreateUserDto dto)
        {
            bool emailExists = await _context.Users
                .AnyAsync(u => u.Email == dto.Email && u.IsActive);

            if (emailExists)
                return BadRequest("Diese Email-Adresse wird bereits verwendet.");

            // Abteilung auflösen (Existenz im DTO geprüft).
            int? departmentId = await ResolveDepartmentId(dto.DepartmentName);

            // Wir bauen den User selbst zusammen — der Caller hat keine Kontrolle über Id, IsActive etc.
            var user = new User
            {
                FirstName = dto.FirstName,
                SecondName = dto.SecondName,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = dto.Role,
                DepartmentId = departmentId,
                IsActive = true,
                IsActivated = true  // Vom Admin angelegte User sind sofort freigeschaltet
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Wir geben ein UserResponseDto zurück, nicht den rohen User
            return CreatedAtAction(nameof(Get), new { id = user.Id }, new UserResponseDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                SecondName = user.SecondName,
                Email = user.Email,
                Role = user.Role.ToString(),
                IsActivated = user.IsActivated,
                DepartmentId = user.DepartmentId,
                DepartmentName = await DepartmentName(user.DepartmentId)
            });
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Put(int id, UpdateUserDto dto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            // Wir überschreiben nur die Felder die der Admin ändern darf
            if (dto.FirstName != null)
            {
                if (dto.FirstName.Trim() == string.Empty)
                    return BadRequest("Vorname darf nicht leer sein.");
                user.FirstName = dto.FirstName;
            }
            if (dto.SecondName != null)
            {
                if (dto.SecondName.Trim() == string.Empty)
                    return BadRequest("Nachname darf nicht leer sein.");
                user.SecondName = dto.SecondName;
            }
            if (dto.Email != null)
            {
                if (dto.Email.Trim() == string.Empty)
                    return BadRequest("E-Mail darf nicht leer sein.");

                bool emailTaken = await _context.Users
                    .AnyAsync(u => u.Email == dto.Email && u.Id != id && u.IsActive);
                if (emailTaken)
                    return BadRequest("Diese E-Mail-Adresse wird bereits verwendet.");

                user.Email = dto.Email;
            }
            if (dto.Role.HasValue) user.Role = dto.Role.Value;
            if (dto.DepartmentName != null)
                user.DepartmentId = await ResolveDepartmentId(dto.DepartmentName);
            if (dto.IsActivated.HasValue) user.IsActivated = dto.IsActivated.Value;
            if (dto.IsActive.HasValue) user.IsActive = dto.IsActive.Value;
            // user.PasswordHash bleibt unberührt!

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(id))
                    return NotFound();
                throw;
            }

            return NoContent();
        }

        // Soft-Delete: User werden NIE physisch aus der DB entfernt, nur deaktiviert (IsActive=false).
        // Genutzt für: Ablehnung einer Registrierung ODER nachträgliche Sperrung eines aktiven Users.
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            user.IsActive = false;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET /api/user/pending  →  Liste aller offenen Registrierungs-Anträge
        // Sichtbar für Admin und Support; nur Admin darf tatsächlich freischalten (siehe Approve unten).
        [HttpGet("pending")]
        [Authorize(Roles = "Admin, Support")]
        public async Task<ActionResult<IEnumerable<UserResponseDto>>> GetPending()
        {
            var pending = await _context.Users
                .Where(u => u.IsActive && !u.IsActivated)
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

            return Ok(pending);
        }

        // POST /api/user/{id}/approve  →  Registrierung freischalten
        // Nur Admin darf das.
        [HttpPost("{id}/approve")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Approve(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null || !user.IsActive)
                return NotFound();

            if (user.IsActivated)
                return BadRequest("Dieser User ist bereits freigeschaltet.");

            user.IsActivated = true;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.Id == id);
        }

        // E-Mail-/Namens-unabhängige Helfer für die Abteilung.
        private async Task<int?> ResolveDepartmentId(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            return await _context.Departments
                .Where(d => d.Name == name)
                .Select(d => (int?)d.Id)
                .FirstOrDefaultAsync();
        }

        private async Task<string?> DepartmentName(int? departmentId)
        {
            if (departmentId == null) return null;
            return await _context.Departments
                .Where(d => d.Id == departmentId)
                .Select(d => d.Name)
                .FirstOrDefaultAsync();
        }
    }
}
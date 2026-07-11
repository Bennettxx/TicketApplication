using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TicketApplication.Data;
using TicketApplication.DTOs;
using TicketApplication.Models;

namespace TicketApplication.Controllers
{
    // benutzerverwaltung, basis-url api/user
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

        // GET api/user/me — eigene identität fürs frontend (name, rolle, navigation)
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
                IsActive = user.IsActive,
                DepartmentId = user.DepartmentId,
                DepartmentName = await DepartmentName(user.DepartmentId)
            });
        }

        // GET api/user — alle user inkl. gesperrter, passworthash bleibt im haus
        [HttpGet(Name = "GetUsers")]
        [Authorize(Roles = "Admin, Support")]
        public async Task<ActionResult<IEnumerable<UserResponseDto>>> Get()
        {
            var users = await _context.Users
                .Select(u => new UserResponseDto
                {
                    Id = u.Id,
                    FirstName = u.FirstName,
                    SecondName = u.SecondName,
                    Email = u.Email,
                    Role = u.Role.ToString(),
                    IsActivated = u.IsActivated,
                    IsActive = u.IsActive,
                    DepartmentId = u.DepartmentId,
                    DepartmentName = _context.Departments
                        .Where(d => d.Id == u.DepartmentId)
                        .Select(d => d.Name)
                        .FirstOrDefault()
                })
                .ToListAsync();

            return Ok(users);
        }

        // GET api/user/{id} — einzelner user
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin, Support")]
        public async Task<ActionResult<UserResponseDto>> Get(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            return Ok(new UserResponseDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                SecondName = user.SecondName,
                Email = user.Email,
                Role = user.Role.ToString(),
                IsActivated = user.IsActivated,
                IsActive = user.IsActive,
                DepartmentId = user.DepartmentId,
                DepartmentName = await DepartmentName(user.DepartmentId)
            });
        }

        // POST api/user — user anlegen (admin), sofort freigeschaltet
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<UserResponseDto>> Post(CreateUserDto dto)
        {
            bool emailExists = await _context.Users
                .AnyAsync(u => u.Email == dto.Email && u.IsActive);

            if (emailExists)
                return BadRequest("Diese Email-Adresse wird bereits verwendet.");

            // abteilung nur für rolle user
            int? departmentId = dto.Role == UserRole.User
                ? await ResolveDepartmentId(dto.DepartmentName)
                : null;

            // objekt wird serverseitig gebaut, client hat keine kontrolle über id/flags
            var user = new User
            {
                FirstName = dto.FirstName,
                SecondName = dto.SecondName,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = dto.Role,
                DepartmentId = departmentId,
                IsActive = true,
                IsActivated = true
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(Get), new { id = user.Id }, new UserResponseDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                SecondName = user.SecondName,
                Email = user.Email,
                Role = user.Role.ToString(),
                IsActivated = user.IsActivated,
                IsActive = user.IsActive,
                DepartmentId = user.DepartmentId,
                DepartmentName = await DepartmentName(user.DepartmentId)
            });
        }

        // PUT api/user/{id} — teilupdate durch admin, nur gesetzte felder
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Put(int id, UpdateUserDto dto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

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

            // abteilung nur für rolle user, bei staff wird sie entfernt
            if (user.Role == UserRole.User)
            {
                if (dto.DepartmentName != null)
                    user.DepartmentId = await ResolveDepartmentId(dto.DepartmentName);
            }
            else
            {
                user.DepartmentId = null;
            }

            if (dto.IsActivated.HasValue) user.IsActivated = dto.IsActivated.Value;
            if (dto.IsActive.HasValue) user.IsActive = dto.IsActive.Value;
            // passworthash bleibt unberührt

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

        // DELETE api/user/{id} — soft-delete, setzt nur IsActive=false
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

        // GET api/user/pending — offene registrierungen (aktiv, aber nicht freigeschaltet)
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
                    IsActive = u.IsActive,
                    DepartmentId = u.DepartmentId,
                    DepartmentName = _context.Departments
                        .Where(d => d.Id == u.DepartmentId)
                        .Select(d => d.Name)
                        .FirstOrDefault()
                })
                .ToListAsync();

            return Ok(pending);
        }

        // POST api/user/{id}/approve — registrierung freischalten, nur admin
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

        // abteilungsname -> id, null wenn leer/unbekannt
        private async Task<int?> ResolveDepartmentId(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            return await _context.Departments
                .Where(d => d.Name == name)
                .Select(d => (int?)d.Id)
                .FirstOrDefaultAsync();
        }

        // abteilungs-id -> name
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

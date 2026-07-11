using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TicketApplication.Data;
using TicketApplication.DTOs;

namespace TicketApplication.Controllers
{
    // eigenes profil: anzeigen, bearbeiten, passwort ändern
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AccountController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET api/account/me — eigenes profil, id kommt aus dem jwt
        [HttpGet("me")]
        public async Task<ActionResult<UserResponseDto>> GetMe()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var user = await _context.Users.FindAsync(userId);
            if (user == null || !user.IsActive)
                return NotFound();

            string? departmentName = user.DepartmentId == null ? null : await _context.Departments
                .Where(d => d.Id == user.DepartmentId).Select(d => d.Name).FirstOrDefaultAsync();

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
                DepartmentName = departmentName
            });
        }

        // PUT api/account/me — eigenes profil ändern, e-mail bewusst nicht änderbar
        [HttpPut("me")]
        public async Task<IActionResult> UpdateMe(UpdateProfileDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var user = await _context.Users.FindAsync(userId);
            if (user == null || !user.IsActive)
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

            // abteilung nur für rolle user, bei staff wird ein gesetzter wert ignoriert
            if (dto.DepartmentName != null && user.Role == UserRole.User)
            {
                var depId = await _context.Departments
                    .Where(d => d.Name == dto.DepartmentName)
                    .Select(d => (int?)d.Id)
                    .FirstOrDefaultAsync();
                if (depId == null) return BadRequest("Abteilung nicht gefunden.");
                user.DepartmentId = depId;
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // PUT api/account/me/password — passwort ändern, altes muss stimmen
        [HttpPut("me/password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var user = await _context.Users.FindAsync(userId);
            if (user == null || !user.IsActive)
                return NotFound();

            bool isValid = BCrypt.Net.BCrypt.Verify(dto.OldPassword, user.PasswordHash);
            if (!isValid)
                return BadRequest("Das alte Passwort ist falsch.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}

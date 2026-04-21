using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TicketApplication.Data;
using TicketApplication.DTOs;

namespace TicketApplication.Controllers
{
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

        // GET /api/account/me  →  Eigenes Profil abrufen
        [HttpGet("me")]
        public async Task<ActionResult<UserResponseDto>> GetMe()
        {
            // Wir lesen die ID aus dem JWT Token — nicht aus der URL!
            // ClaimTypes.NameIdentifier = das was wir beim Login in CreateToken() gesetzt haben
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
                IsEmailConfirmed = user.IsEmailConfirmed
            });
        }

        // PUT /api/account/me  →  Eigenes Profil bearbeiten
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

            if (dto.Email != null)
            {
                if (dto.Email.Trim() == string.Empty)
                    return BadRequest("E-Mail darf nicht leer sein.");

                bool emailTaken = await _context.Users
                    .AnyAsync(u => u.Email == dto.Email && u.Id != userId && u.IsActive);
                if (emailTaken)
                    return BadRequest("Diese E-Mail-Adresse wird bereits verwendet.");

                user.Email = dto.Email;
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpPut("me/password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var user = await _context.Users.FindAsync(userId);
            if (user == null || !user.IsActive)
                return NotFound();

            // Altes Passwort prüfen — der User muss sein aktuelles Passwort kennen
            bool isValid = BCrypt.Net.BCrypt.Verify(dto.OldPassword, user.PasswordHash);
            if (!isValid)
                return BadRequest("Das alte Passwort ist falsch.");

            // Neues Passwort hashen und speichern
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}

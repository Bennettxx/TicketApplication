using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TicketApplication.Data;
using TicketApplication.Models;
using TicketApplication.DTOs;

namespace TicketApplication.Controllers
{
    // login + registrierung, gibt jwt zurück
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;

        public AuthController(ApplicationDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // POST api/auth/login — prüft zugangsdaten, liefert token
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            // aktiven user bevorzugen, sonst inaktiven für die "abgelehnt"-meldung
            var user = await _context.Users
                .Where(u => u.Email == loginDto.Email)
                .OrderByDescending(u => u.IsActive)
                .FirstOrDefaultAsync();

            if (user == null) return Unauthorized("Ungültige E-Mail oder Passwort.");

            // passwort zuerst prüfen, kontostatus nicht an fremde verraten
            bool isValid = BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash);
            if (!isValid) return Unauthorized("Ungültige E-Mail oder Passwort.");

            if (!user.IsActive)
            {
                return Unauthorized("Dein Konto wurde abgelehnt oder deaktiviert. Bitte wende dich an einen Administrator.");
            }

            if (!user.IsActivated)
            {
                return Unauthorized("Konto wurde noch nicht durch einen Admin freigeschaltet.");
            }

            var token = CreateToken(user);

            return Ok(new { token = token });
        }

        // POST api/auth/register — legt user an, freischaltung macht der admin
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            // nur rolle user, admins/support werden manuell angelegt
            bool emailExists = await _context.Users.AnyAsync(u => u.Email == registerDto.Email && u.IsActive);

            if (emailExists)
            {
                return BadRequest("Ein Benutzer mit dieser E-Mail-Adresse existiert bereits.");
            }

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password);

            var departmentId = await _context.Departments
                .Where(d => d.Name == registerDto.DepartmentName)
                .Select(d => (int?)d.Id)
                .FirstOrDefaultAsync();

            var user = new User
            {
                FirstName = registerDto.FirstName,
                SecondName = registerDto.SecondName,
                Email = registerDto.Email,
                PasswordHash = passwordHash,
                Role = UserRole.User,
                DepartmentId = departmentId,
                IsActive = true,
                IsActivated = false // erst nach admin-freischaltung nutzbar
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Registrierung eingegangen. Ein Admin wird dein Konto in Kürze freischalten." });
        }

        // jwt bauen: id, mail und rolle als claims
        private string CreateToken(User user)
        {
            var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(double.Parse(_config["Jwt:DurationInMinutes"]!)),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}

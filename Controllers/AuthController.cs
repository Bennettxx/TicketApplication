using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TicketApplication.Data;
using TicketApplication.Models;
using BCrypt.Net;
using TicketApplication.DTOs;

namespace TicketApplication.Controllers
{
    // Controller für die Authentifizierung
    // Route: api/auth/login
    // POST Body: { "email": "", "password": "" }
    // Antwort: { "token": "..." } oder 401 Unauthorized

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

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            // User suchen (muss aktiv sein!)
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == loginDto.Email && u.IsActive);

            if (user == null) return Unauthorized("Ungültige E-Mail oder Passwort.");

            // Freischaltung durch Admin prüfen
            if (!user.IsActivated)
            {
                return Unauthorized("Konto wurde noch nicht durch einen Admin freigeschaltet.");
            }

            // Passwort prüfen (BCrypt vergleicht den Hash)
            bool isValid = BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash);
            if (!isValid) return Unauthorized("Ungültige E-Mail oder Passwort.");

            // Token erstellen
            var token = CreateToken(user);

            return Ok(new { token = token });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            // Es können sich nur USER registrieren, Admins und Support müssen manuell angelegt werden
            bool emailExists = await _context.Users.AnyAsync(u => u.Email == registerDto.Email && u.IsActive);

            if (emailExists)
            {
                return BadRequest("Ein Benutzer mit dieser E-Mail-Adresse existiert bereits.");
            }
            
            // Passwort hashen
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password);

            var user = new User
            {
                Email = registerDto.Email,
                PasswordHash = passwordHash,
                Role = UserRole.User,
                IsActive = true,
                IsActivated = false   // muss erst durch einen Admin freigeschaltet werden
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Registrierung eingegangen. Ein Admin wird dein Konto in Kürze freischalten." });
        }

        private string CreateToken(User user)
        {
            // "Claims" sind die Infos die im Ausweis stehen (Id, Email, Rolle)
            var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(double.Parse(_config["Jwt:DurationInMinutes"])),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}

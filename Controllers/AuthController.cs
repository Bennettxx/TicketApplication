using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TicketApplication.Data;
using TicketApplication.Models;
using TicketApplication.DTOs;
using TicketApplication.Services;

namespace TicketApplication.Controllers
{
    // login, registrierung (mit mailbestätigung) und jwt-erstellung
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;
        private readonly LogService _log;
        private readonly MailService _mail;
        private readonly MailQueue _mailQueue;

        public AuthController(ApplicationDbContext context, IConfiguration config,
            LogService log, MailService mail, MailQueue mailQueue)
        {
            _context = context;
            _config = config;
            _log = log;
            _mail = mail;
            _mailQueue = mailQueue;
        }

        // POST api/auth/login — prüft zugangsdaten, liefert token + zwangswechsel-flag
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            // aktiven user bevorzugen, sonst inaktiven für die "abgelehnt"-meldung
            var user = await _context.Users
                .Where(u => u.Email == loginDto.Email)
                .OrderByDescending(u => u.IsActive)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                _log.Warn(LogBereich.Auth, $"Login fehlgeschlagen (unbekannte E-Mail): {loginDto.Email}");
                return Unauthorized("Ungültige E-Mail oder Passwort.");
            }

            // passwort zuerst prüfen, kontostatus nicht an fremde verraten
            bool isValid = BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash);
            if (!isValid)
            {
                _log.Warn(LogBereich.Auth, $"Login fehlgeschlagen (falsches Passwort): {loginDto.Email}");
                return Unauthorized("Ungültige E-Mail oder Passwort.");
            }

            if (!user.IsActive)
            {
                _log.Warn(LogBereich.Auth, $"Login abgelehnt (Konto deaktiviert): {loginDto.Email}");
                return Unauthorized("Dein Konto wurde abgelehnt oder deaktiviert. Bitte wende dich an einen Administrator.");
            }

            if (!user.EmailConfirmed)
            {
                _log.Warn(LogBereich.Auth, $"Login abgelehnt (E-Mail unbestätigt): {loginDto.Email}");
                return Unauthorized("Bitte bestätige zuerst deine E-Mail-Adresse (Link in der Bestätigungsmail).");
            }

            if (!user.IsActivated)
            {
                _log.Warn(LogBereich.Auth, $"Login abgelehnt (nicht freigeschaltet): {loginDto.Email}");
                return Unauthorized("Konto wurde noch nicht durch einen Admin freigeschaltet.");
            }

            var token = CreateToken(user);
            _log.Info(LogBereich.Auth, $"Login erfolgreich: {user.Email} (Rolle {user.Role})");

            return Ok(new { token = token, mustChangePassword = user.MustChangePassword });
        }

        // POST api/auth/register — user anlegen; mailbestätigung nur wenn smtp konfiguriert
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

            bool mailAktiv = await _mail.IsConfiguredAsync();
            string? confirmToken = mailAktiv ? Guid.NewGuid().ToString("N") : null;

            var user = new User
            {
                FirstName = registerDto.FirstName,
                SecondName = registerDto.SecondName,
                Email = registerDto.Email,
                PasswordHash = passwordHash,
                Role = UserRole.User,
                DepartmentId = departmentId,
                IsActive = true,
                IsActivated = false, // erst nach admin-freischaltung nutzbar
                EmailConfirmed = !mailAktiv, // ohne smtp keine bestätigung möglich
                EmailConfirmToken = confirmToken
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _log.Info(LogBereich.Auth, $"Registrierung eingegangen: {user.Email} (Mailbestätigung: {(mailAktiv ? "angefordert" : "übersprungen, SMTP inaktiv")})");

            if (mailAktiv)
            {
                var link = $"{Request.Scheme}://{Request.Host}/api/auth/confirm?token={confirmToken}";
                _mailQueue.Enqueue(user.Email,
                    "Ticket System - E-Mail bestätigen",
                    $"Hallo {user.FirstName},\n\n" +
                    "bitte bestätige deine E-Mail-Adresse über folgenden Link:\n" +
                    link + "\n\n" +
                    "Danach muss dein Konto noch von einem Administrator freigeschaltet werden.");
                return Ok(new { message = "Registrierung eingegangen. Bitte bestätige deine E-Mail-Adresse (Link per Mail). Danach schaltet ein Admin dein Konto frei." });
            }

            return Ok(new { message = "Registrierung eingegangen. Ein Admin wird dein Konto in Kürze freischalten." });
        }

        // GET api/auth/confirm?token=... — bestätigungslink aus der mail
        [HttpGet("confirm")]
        public async Task<IActionResult> Confirm([FromQuery] string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return BadRequest("Kein Token angegeben.");

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.EmailConfirmToken == token && !u.EmailConfirmed);
            if (user == null)
                return BadRequest("Ungültiger oder bereits verwendeter Bestätigungslink.");

            user.EmailConfirmed = true;
            user.EmailConfirmToken = null;
            await _context.SaveChangesAsync();

            _log.Info(LogBereich.Auth, $"E-Mail bestätigt: {user.Email}");
            return Redirect("/index.html?confirmed=1");
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

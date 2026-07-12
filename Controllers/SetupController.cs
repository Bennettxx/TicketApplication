using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketApplication.Data;
using TicketApplication.DTOs;
using TicketApplication.Services;

namespace TicketApplication.Controllers
{
    // erststart-einrichtung: db-verbindung + log-pfad, nur solange nichts konfiguriert ist
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class SetupController : ControllerBase
    {
        public const string DefaultAdminEmail = "admin@ticket.local";
        public const string DefaultAdminPassword = "admin";

        private readonly AppConfigService _config;
        private readonly LogService _log;
        private readonly IWebHostEnvironment _env;

        public SetupController(AppConfigService config, LogService log, IWebHostEnvironment env)
        {
            _config = config;
            _log = log;
            _env = env;
        }

        // GET api/setup/status — läuft die app schon konfiguriert?
        [HttpGet("status")]
        public IActionResult Status()
        {
            return Ok(new { configured = _config.IsConfigured });
        }

        // POST api/setup/test — verbindungstest mit den eingegebenen daten
        [HttpPost("test")]
        public IActionResult Test(SetupDbDto dto)
        {
            if (_config.IsConfigured) return Forbid();

            var (ok, message) = AppConfigService.TestConnection(ToDbConfig(dto));
            return Ok(new { ok, message });
        }

        // POST api/setup/complete — config speichern, db anlegen, startdaten einspielen
        [HttpPost("complete")]
        public IActionResult Complete(SetupDbDto dto)
        {
            if (_config.IsConfigured) return Forbid();

            var dbConfig = ToDbConfig(dto);

            var (ok, message) = AppConfigService.TestConnection(dbConfig);
            if (!ok) return BadRequest(message);

            // log-pfad prüfen (ordner anlegen + probeschreiben)
            var logPath = string.IsNullOrWhiteSpace(dto.LogPath)
                ? Path.Combine(_env.ContentRootPath, "Logs")
                : dto.LogPath.Trim();
            try
            {
                Directory.CreateDirectory(logPath);
                var probe = Path.Combine(logPath, ".schreibtest");
                System.IO.File.WriteAllText(probe, "ok");
                System.IO.File.Delete(probe);
            }
            catch (Exception ex)
            {
                return BadRequest("Log-Pfad nicht beschreibbar: " + ex.Message);
            }

            // db anlegen und initialisieren (eigener kontext, di ist noch nicht konfiguriert)
            try
            {
                var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseSqlServer(AppConfigService.BuildConnectionString(dbConfig))
                    .Options;
                using var context = new ApplicationDbContext(options);
                // bewusst ohne dev-testuser, damit die angezeigten
                // standard-admin-zugangsdaten immer stimmen
                DbInitializer.Initialize(context, isDevelopment: false);
            }
            catch (Exception ex)
            {
                _log.Error(LogBereich.App, "Setup: DB-Initialisierung fehlgeschlagen.", ex);
                return BadRequest("Datenbank konnte nicht angelegt werden: " + ex.Message);
            }

            // erst nach erfolgreicher initialisierung speichern
            _config.SaveDb(dbConfig);
            _config.SaveLogPath(logPath);

            _log.Info(LogBereich.App, $"Setup abgeschlossen. Server={dbConfig.Server}, DB={dbConfig.Database}, LogPath={logPath}");

            return Ok(new SetupResultDto
            {
                Message = "Einrichtung abgeschlossen. Bitte mit dem Standard-Admin einloggen und sofort das Passwort ändern.",
                AdminEmail = DefaultAdminEmail,
                AdminPassword = DefaultAdminPassword
            });
        }

        private static DbConfig ToDbConfig(SetupDbDto dto) => new()
        {
            Server = dto.Server.Trim(),
            Database = dto.Database.Trim(),
            UseWindowsAuth = dto.UseWindowsAuth,
            User = dto.UseWindowsAuth ? null : dto.User?.Trim(),
            Password = dto.UseWindowsAuth ? null : dto.Password
        };
    }
}

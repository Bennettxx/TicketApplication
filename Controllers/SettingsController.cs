using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TicketApplication.DTOs;
using TicketApplication.Services;

namespace TicketApplication.Controllers
{
    // systemeinstellungen (smtp, db-verbindung, log-pfad), nur admin
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class SettingsController : ControllerBase
    {
        private readonly MailService _mail;
        private readonly AppConfigService _config;
        private readonly LogService _log;

        public SettingsController(MailService mail, AppConfigService config, LogService log)
        {
            _mail = mail;
            _config = config;
            _log = log;
        }

        private string CurrentUserEmail =>
            User.FindFirstValue(ClaimTypes.Email) ?? "unbekannt";

        // GET api/settings/smtp — aktuelle smtp-einstellungen, ohne passwort
        [HttpGet("smtp")]
        public async Task<ActionResult<SmtpSettingsDto>> GetSmtp()
        {
            var s = await _mail.GetSettingsAsync();
            return Ok(new SmtpSettingsDto
            {
                Enabled = s.Enabled,
                Host = s.Host,
                Port = s.Port,
                UseSsl = s.UseSsl,
                User = s.User,
                Password = null,
                FromAddress = s.FromAddress,
                FromName = s.FromName,
                HasPassword = !string.IsNullOrEmpty(s.Password)
            });
        }

        // PUT api/settings/smtp — einstellungen speichern; password null = altes behalten
        [HttpPut("smtp")]
        public async Task<IActionResult> SaveSmtp(SmtpSettingsDto dto)
        {
            var s = new SmtpSettings
            {
                Enabled = dto.Enabled,
                Host = dto.Host.Trim(),
                Port = dto.Port,
                UseSsl = dto.UseSsl,
                User = dto.User.Trim(),
                FromAddress = dto.FromAddress?.Trim() ?? string.Empty,
                FromName = string.IsNullOrWhiteSpace(dto.FromName) ? "Ticket System" : dto.FromName.Trim()
            };
            await _mail.SaveSettingsAsync(s, dto.Password);

            _log.Info(LogBereich.Einstellungen,
                $"SMTP-Einstellungen geändert durch {CurrentUserEmail} (Host={s.Host}:{s.Port}, Enabled={s.Enabled}).");
            return NoContent();
        }

        // POST api/settings/smtp/test — testmail an die hinterlegte absenderadresse
        [HttpPost("smtp/test")]
        public async Task<IActionResult> TestSmtp()
        {
            var s = await _mail.GetSettingsAsync();
            if (string.IsNullOrWhiteSpace(s.Host) || string.IsNullOrWhiteSpace(s.FromAddress))
                return BadRequest("Bitte zuerst Host und Absenderadresse speichern.");

            try
            {
                await _mail.SendAsync(s.FromAddress, "Ticket System - Testmail",
                    "Diese Testmail bestätigt, dass die SMTP-Einstellungen funktionieren.\n" +
                    $"Gesendet am {DateTime.Now:dd.MM.yyyy HH:mm:ss}.");
                _log.Info(LogBereich.Mail, $"Testmail an {s.FromAddress} erfolgreich (ausgelöst von {CurrentUserEmail}).");
                return Ok(new { ok = true, message = $"Testmail an {s.FromAddress} gesendet." });
            }
            catch (Exception ex)
            {
                _log.Error(LogBereich.Mail, $"Testmail an {s.FromAddress} fehlgeschlagen (ausgelöst von {CurrentUserEmail}).", ex);
                return Ok(new { ok = false, message = "Versand fehlgeschlagen: " + ex.Message });
            }
        }

        // GET api/settings/system — log-pfad + db-verbindung (ohne passwort)
        [HttpGet("system")]
        public IActionResult GetSystem()
        {
            var db = _config.Current?.Db;
            return Ok(new SystemSettingsDto
            {
                LogPath = _config.LogPath,
                DebugLogging = _config.DebugLogging,
                DbServer = db?.Server ?? string.Empty,
                DbDatabase = db?.Database ?? string.Empty,
                DbUseWindowsAuth = db?.UseWindowsAuth ?? true,
                DbUser = db?.User ?? string.Empty,
                LegacyConfig = _config.IsLegacyFallback
            });
        }

        // PUT api/settings/logpath — log-verzeichnis + debug-modus ändern (mit schreibtest)
        [HttpPut("logpath")]
        public IActionResult SaveLogPath(LogPathDto dto)
        {
            var pfad = dto.LogPath.Trim();
            try
            {
                Directory.CreateDirectory(pfad);
                var probe = Path.Combine(pfad, ".schreibtest");
                System.IO.File.WriteAllText(probe, "ok");
                System.IO.File.Delete(probe);
            }
            catch (Exception ex)
            {
                return BadRequest("Pfad nicht beschreibbar: " + ex.Message);
            }

            _config.SaveLogPath(pfad);
            _config.SaveDebugLogging(dto.DebugLogging);
            _log.Info(LogBereich.Einstellungen,
                $"Logging geändert durch {CurrentUserEmail}: Pfad='{pfad}', Debug={(dto.DebugLogging ? "an" : "aus")}.");
            return NoContent();
        }

        // POST api/settings/database/test — verbindungstest ohne zu speichern
        [HttpPost("database/test")]
        public IActionResult TestDatabase(SetupDbDto dto)
        {
            var (ok, message) = AppConfigService.TestConnection(ToDbConfig(dto));
            return Ok(new { ok, message });
        }

        // PUT api/settings/database — verbindung nach erfolgreichem test speichern
        [HttpPut("database")]
        public IActionResult SaveDatabase(SetupDbDto dto)
        {
            var db = ToDbConfig(dto);
            var (ok, message) = AppConfigService.TestConnection(db);
            if (!ok) return BadRequest(message);

            _config.SaveDb(db);
            _log.Info(LogBereich.Einstellungen,
                $"DB-Verbindung geändert durch {CurrentUserEmail} (Server={db.Server}, DB={db.Database}).");
            return Ok(new { message = "Gespeichert. Die neue Verbindung gilt für alle neuen Anfragen." });
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

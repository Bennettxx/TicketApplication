using System.ComponentModel.DataAnnotations;

namespace TicketApplication.DTOs
{
    // smtp-einstellungen; passwort ist write-only (antwort enthält nur HasPassword)
    public class SmtpSettingsDto
    {
        public bool Enabled { get; set; }

        [MaxLength(255)]
        public string Host { get; set; } = string.Empty;

        [Range(1, 65535, ErrorMessage = "Port muss zwischen 1 und 65535 liegen.")]
        public int Port { get; set; } = 587;

        public bool UseSsl { get; set; }

        [MaxLength(255)]
        public string User { get; set; } = string.Empty;

        // null = passwort behalten, leer = löschen, sonst neu setzen
        public string? Password { get; set; }

        // nullable, damit auch eine unvollständige konfiguration gespeichert werden kann
        [EmailAddress(ErrorMessage = "Absenderadresse ist keine gültige E-Mail.")]
        public string? FromAddress { get; set; }

        [MaxLength(100)]
        public string FromName { get; set; } = string.Empty;

        public bool HasPassword { get; set; }
    }

    // systeminfo für die einstellungen-seite
    public class SystemSettingsDto
    {
        public string LogPath { get; set; } = string.Empty;
        public bool DebugLogging { get; set; }
        public string DbServer { get; set; } = string.Empty;
        public string DbDatabase { get; set; } = string.Empty;
        public bool DbUseWindowsAuth { get; set; } = true;
        public string DbUser { get; set; } = string.Empty;

        // true = verbindung kommt noch aus appsettings/umgebungsvariablen
        public bool LegacyConfig { get; set; }
    }

    // eingang für logging-einstellungen (pfad + debug-modus)
    public class LogPathDto
    {
        [Required(ErrorMessage = "Pfad ist erforderlich.")]
        public string LogPath { get; set; } = string.Empty;

        public bool DebugLogging { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace TicketApplication.DTOs
{
    // eingang für verbindungstest und setup-abschluss
    public class SetupDbDto
    {
        [Required(ErrorMessage = "Server ist erforderlich.")]
        public string Server { get; set; } = string.Empty;

        [Required(ErrorMessage = "Datenbankname ist erforderlich.")]
        public string Database { get; set; } = string.Empty;

        public bool UseWindowsAuth { get; set; } = true;

        public string? User { get; set; }
        public string? Password { get; set; }

        // nur beim setup-abschluss relevant
        public string? LogPath { get; set; }
    }

    // ausgang nach erfolgreichem setup
    public class SetupResultDto
    {
        public string Message { get; set; } = string.Empty;
        public string AdminEmail { get; set; } = string.Empty;
        public string AdminPassword { get; set; } = string.Empty;
    }
}

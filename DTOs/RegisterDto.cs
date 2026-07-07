using System.ComponentModel.DataAnnotations;
using TicketApplication.Functions;
using TicketApplication.Models;

namespace TicketApplication.DTOs
{
    public class RegisterDto
    {
        // Vor- und Nachname werden bei der Registrierung erfasst, damit sie
        // später automatisch in Tickets übernommen werden können.
        [Required(ErrorMessage = "Vorname ist erforderlich.")]
        [MaxLength(50, ErrorMessage = "Vorname darf maximal 50 Zeichen lang sein.")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nachname ist erforderlich.")]
        [MaxLength(50, ErrorMessage = "Nachname darf maximal 50 Zeichen lang sein.")]
        public string SecondName { get; set; } = string.Empty;

        [Required(ErrorMessage = "E-Mail ist erforderlich.")]
        [EmailAddress(ErrorMessage = "Keine gültige E-Mail-Adresse.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Passwort ist erforderlich.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$",
            ErrorMessage = "Passwort braucht min. 8 Zeichen, einen Großbuchstaben, einen Kleinbuchstaben und eine Zahl.")]
        public string Password { get; set; } = string.Empty;

        // Jeder Benutzer gehört zu einer Abteilung. Diese wird bereits bei der
        // Registrierung gewählt und dient später als Kategorie in der Statistik
        // (Bearbeitungszeit pro Abteilung). Muss eine existierende Abteilung sein.
        [Required(ErrorMessage = "Abteilung ist erforderlich.")]
        [ExistsInColumn(typeof(Department), "Name", ErrorMessage = "Abteilung nicht gefunden.")]
        public string DepartmentName { get; set; } = string.Empty;
    }
}

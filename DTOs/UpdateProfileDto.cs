using System.ComponentModel.DataAnnotations;

namespace TicketApplication.DTOs
{
    // Partielles Update des eigenen Profils: Jedes Feld ist OPTIONAL.
    // Nur gesetzte Felder werden übernommen (siehe AccountController.UpdateMe).
    // Daher KEIN [Required] – ein leeres Feld bedeutet "nicht ändern".
    public class UpdateProfileDto
    {
        [MaxLength(50, ErrorMessage = "Vorname darf maximal 50 Zeichen lang sein.")]
        public string? FirstName { get; set; }

        [MaxLength(50, ErrorMessage = "Nachname darf maximal 50 Zeichen lang sein.")]
        public string? SecondName { get; set; }

        [EmailAddress(ErrorMessage = "Keine gültige E-Mail-Adresse.")]
        public string? Email { get; set; }
    }
}

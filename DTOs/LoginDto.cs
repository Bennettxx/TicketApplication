using System.ComponentModel.DataAnnotations;

namespace TicketApplication.DTOs
{
    public class LoginDto
    {
        // DTO nur für den Einlogg-Prozess, damit nicht die ganze User-Klasse mitgeschickt werden muss
        // Einzige Stelle wo Password im Klartext übergeben wird, noch nicht gehasht
        [Required(ErrorMessage = "E-Mail ist erforderlich.")]
        [EmailAddress(ErrorMessage = "Keine gültige E-Mail-Adresse.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Passwort ist erforderlich.")]
        public string Password { get; set; } = string.Empty;
    }
}

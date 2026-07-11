using System.ComponentModel.DataAnnotations;

namespace TicketApplication.DTOs
{
    // eingang für den login, passwort hier noch im klartext
    public class LoginDto
    {
        [Required(ErrorMessage = "E-Mail ist erforderlich.")]
        [EmailAddress(ErrorMessage = "Keine gültige E-Mail-Adresse.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Passwort ist erforderlich.")]
        public string Password { get; set; } = string.Empty;
    }
}

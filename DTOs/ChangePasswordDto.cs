using System.ComponentModel.DataAnnotations;

namespace TicketApplication.DTOs
{
    // eingang für passwortwechsel im eigenen profil
    public class ChangePasswordDto
    {
        [Required(ErrorMessage = "Altes Passwort ist erforderlich.")]
        public string OldPassword { get; set; } = string.Empty;

        [Required]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$",
            ErrorMessage = "Passwort braucht min. 8 Zeichen, einen Großbuchstaben, einen Kleinbuchstaben und eine Zahl.")]
        public string NewPassword { get; set; } = string.Empty;
    }
}

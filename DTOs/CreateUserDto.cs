using System.ComponentModel.DataAnnotations;
using TicketApplication.Data;

namespace TicketApplication.DTOs
{
    public class CreateUserDto
    {
        [Required]
        [MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string SecondName { get; set; } = string.Empty;

        [Required]
        [EmailAddress(ErrorMessage = "Keine gültige E-Mail-Adresse.")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$",
            ErrorMessage = "Passwort braucht min. 8 Zeichen, einen Großbuchstaben, einen Kleinbuchstaben und eine Zahl.")]
        public string Password { get; set; } = string.Empty;

        public UserRole Role { get; set; } = UserRole.User;
    }
}

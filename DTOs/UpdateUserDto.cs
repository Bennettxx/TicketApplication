using System.ComponentModel.DataAnnotations;
using TicketApplication.Data;

namespace TicketApplication.DTOs
{
    public class UpdateUserDto
    {
        [Required]
        [MaxLength(50)]
        public string? FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string? SecondName { get; set; } = string.Empty;

        [Required]
        [EmailAddress(ErrorMessage = "Keine gültige E-Mail-Adresse.")]
        public string? Email { get; set; } = string.Empty;

        public UserRole? Role { get; set; } = UserRole.User;
        public bool? IsActivated { get; set; }
        public bool? IsActive { get; set; }
    }
}

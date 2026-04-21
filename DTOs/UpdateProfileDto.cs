using System.ComponentModel.DataAnnotations;

namespace TicketApplication.DTOs
{
    public class UpdateProfileDto
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
    }
}

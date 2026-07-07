using System.ComponentModel.DataAnnotations;
using TicketApplication.Data;

namespace TicketApplication.DTOs
{
    // Eingangs-DTO für eine neue Problemmeldung (anonym oder eingeloggt).
    public class CreateProblemDto
    {
        [Required(ErrorMessage = "Titel ist erforderlich.")]
        [MinLength(3, ErrorMessage = "Titel muss mindestens 3 Zeichen lang sein.")]
        [MaxLength(200, ErrorMessage = "Titel darf maximal 200 Zeichen lang sein.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Beschreibung ist erforderlich.")]
        [MaxLength(2000, ErrorMessage = "Beschreibung darf maximal 2000 Zeichen lang sein.")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Priorität ist erforderlich.")]
        [EnumDataType(typeof(TicketPriority), ErrorMessage = "Ungültige Priorität.")]
        public TicketPriority Priority { get; set; } = TicketPriority.Low;

        // Kontakt-E-Mail ist PFLICHT (auch eingeloggt – dort vorbefüllt, aber änderbar).
        [Required(ErrorMessage = "Kontakt-E-Mail ist erforderlich.")]
        [EmailAddress(ErrorMessage = "Keine gültige E-Mail-Adresse.")]
        public string ContactEmail { get; set; } = string.Empty;
    }

    // Eingangs-DTO für die Statusänderung (nur Admin).
    public class UpdateProblemStatusDto
    {
        [Required]
        [EnumDataType(typeof(TicketStatus), ErrorMessage = "Ungültiger Status.")]
        public TicketStatus Status { get; set; }
    }

    // Ausgangs-DTO für eine Problemmeldung.
    public class ProblemResponseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public int PriorityCode { get; set; }
        public string Status { get; set; } = string.Empty;
        public int StatusCode { get; set; }
        public string ContactEmail { get; set; } = string.Empty;
        public int? CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
    }
}

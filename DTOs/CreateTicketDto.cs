using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using TicketApplication.Data;
using TicketApplication.Functions;
using TicketApplication.Models;

namespace TicketApplication.DTOs
{
    public class CreateTicketDto
    {
        [Required]
        [EnumDataType(typeof(TicketPriority), ErrorMessage = "Ungültige Priorität.")]
        public TicketPriority Priority { get; set; } = TicketPriority.Low;

        [Required(ErrorMessage = "Titel ist erforderlich.")]
        [MaxLength(200, ErrorMessage = "Titel darf maximal 200 Zeichen lang sein.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Beschreibung ist erforderlich.")]
        [MaxLength(2000, ErrorMessage = "Beschreibung darf maximal 2000 Zeichen lang sein.")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Inhalt ist erforderlich.")]
        [MaxLength(2000, ErrorMessage = "Inhalt darf maximal 2000 Zeichen lang sein.")]
        public string ExpectedResult { get; set; } = string.Empty;

        [Required(ErrorMessage = "Inhalt ist erforderlich.")]
        [MaxLength(2000, ErrorMessage = "Inhalt darf maximal 2000 Zeichen lang sein.")]
        public string ActualResult { get; set; } = string.Empty;

        [Range(typeof(bool), "true", "true", ErrorMessage = "Muss akzzeptiert werden.")]
        public bool AgreedBilling { get; set; } = false;

        [Range(typeof(bool), "true", "true", ErrorMessage = "Muss akzzeptiert werden.")]
        public bool AgreedAGB { get; set; } = false;

        // Status ist immer Open bei Erstellung, daher nicht im DTO enthalten

        [ExistsInColumn(typeof(User), "Email", ErrorMessage = "User nicht gefunden.")]
        public string? AssignedUserMail1 { get; set; } // Wird im Controler in UserId umgewandelt (ID Safety)

        [RequiresField("AssignedUserMail1", ErrorMessage = "Zusätzlicher User 1 fehlt.")]
        [ExistsInColumn(typeof(User), "Email", ErrorMessage = "User nicht gefunden.")]
        public string? AssignedUserMail2 { get; set; } // Wird im Controler in UserId umgewandelt (ID Safety)

        [RequiresField("AssignedUserMail2", ErrorMessage = "Zusätzlicher User 2 fehlt.")]
        [ExistsInColumn(typeof(User), "Email", ErrorMessage = "User nicht gefunden.")]
        public string? AssignedUserMail3 { get; set; } // Wird im Controler in UserId umgewandelt (ID Safety)

        [Required(ErrorMessage = "Abteilungsname ist erforderlich.")]
        [ExistsInColumn(typeof(Department), "Name", ErrorMessage = "Abteilung nicht gefunden.")]
        public string DepartmentName { get; set; }

        [MaxLength(30, ErrorMessage = "Abteilung nicht gefunden.")]
        public string SubjectName { get; set; }

    }
}

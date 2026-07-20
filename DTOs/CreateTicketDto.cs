using System.ComponentModel.DataAnnotations;
using TicketApplication.Data;
using TicketApplication.Functions;
using TicketApplication.Models;

namespace TicketApplication.DTOs
{
    // eingang für neues ticket, status ist bei erstellung immer open
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

        [Range(typeof(bool), "true", "true", ErrorMessage = "Muss akzeptiert werden.")]
        public bool AgreedBilling { get; set; } = false;

        [Range(typeof(bool), "true", "true", ErrorMessage = "Muss akzeptiert werden.")]
        public bool AgreedAGB { get; set; } = false;

        // optionaler verweis auf ein bestehendes ticket
        [ExistsInColumn(typeof(Ticket), "Id", ErrorMessage = "Referenz-Ticket nicht gefunden.")]
        public int? ReferenceTicketId { get; set; }

        [MaxLength(500, ErrorMessage = "Referenz-Kommentar darf maximal 500 Zeichen lang sein.")]
        public string? ReferenceComment { get; set; }

        // zusatzkontakte als mail, werden im controller in ids aufgelöst
        [ExistsInColumn(typeof(User), "Email", ErrorMessage = "User nicht gefunden.")]
        public string? AssignedUserMail1 { get; set; }

        [RequiresField("AssignedUserMail1", ErrorMessage = "Zusätzlicher User 1 fehlt.")]
        [ExistsInColumn(typeof(User), "Email", ErrorMessage = "User nicht gefunden.")]
        public string? AssignedUserMail2 { get; set; }

        [RequiresField("AssignedUserMail2", ErrorMessage = "Zusätzlicher User 2 fehlt.")]
        [ExistsInColumn(typeof(User), "Email", ErrorMessage = "User nicht gefunden.")]
        public string? AssignedUserMail3 { get; set; }

        [Required(ErrorMessage = "Abteilungsname ist erforderlich.")]
        [ExistsInColumn(typeof(Department), "Name", ErrorMessage = "Abteilung nicht gefunden.")]
        public string DepartmentName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Thema (Subject) ist erforderlich.")]
        [MinLength(2, ErrorMessage = "Thema muss mindestens 2 Zeichen lang sein.")]
        [MaxLength(30, ErrorMessage = "Thema darf maximal 30 Zeichen lang sein.")]
        public string SubjectName { get; set; } = string.Empty;
    }
}

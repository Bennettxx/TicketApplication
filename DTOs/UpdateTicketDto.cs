using TicketApplication.Data;
using TicketApplication.Functions;
using TicketApplication.Models;

namespace TicketApplication.DTOs
{
    public class UpdateTicketDto
    {
        public TicketPriority? Priority { get; set; }
        public string? AssignedToUserMail { get; set; }

        [ExistsInColumn(typeof(User), "Email", ErrorMessage = "User nicht gefunden.")]
        public string? AssignedUserMail1 { get; set; } // Wird im Controler in UserId umgewandelt (ID Safety)

        [RequiresField("AssignedUserMail1", ErrorMessage = "Zusätzlicher User 1 fehlt.")]
        [ExistsInColumn(typeof(User), "Email", ErrorMessage = "User nicht gefunden.")]
        public string? AssignedUserMail2 { get; set; } // Wird im Controler in UserId umgewandelt (ID Safety)

        [RequiresField("AssignedUserMail2", ErrorMessage = "Zusätzlicher User 2 fehlt.")]
        [ExistsInColumn(typeof(User), "Email", ErrorMessage = "User nicht gefunden.")]
        public string? AssignedUserMail3 { get; set; } // Wird im Controler in UserId umgewandelt (ID Safety)

        public string? DepartmentName { get; set; }
        public string? SubjectName { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

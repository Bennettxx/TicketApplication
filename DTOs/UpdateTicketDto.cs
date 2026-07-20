using TicketApplication.Data;
using TicketApplication.Functions;
using TicketApplication.Models;

namespace TicketApplication.DTOs
{
    // teilupdate eines tickets, alle felder optional
    public class UpdateTicketDto
    {
        public TicketPriority? Priority { get; set; }
        public string? AssignedToUserMail { get; set; }

        [ExistsInColumn(typeof(User), "Email", ErrorMessage = "User nicht gefunden.")]
        public string? AssignedUserMail1 { get; set; }

        [RequiresField("AssignedUserMail1", ErrorMessage = "Zusätzlicher User 1 fehlt.")]
        [ExistsInColumn(typeof(User), "Email", ErrorMessage = "User nicht gefunden.")]
        public string? AssignedUserMail2 { get; set; }

        [RequiresField("AssignedUserMail2", ErrorMessage = "Zusätzlicher User 2 fehlt.")]
        [ExistsInColumn(typeof(User), "Email", ErrorMessage = "User nicht gefunden.")]
        public string? AssignedUserMail3 { get; set; }

        public string? DepartmentName { get; set; }
        public string? SubjectName { get; set; }
    }
}

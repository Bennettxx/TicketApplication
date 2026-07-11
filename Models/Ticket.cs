using TicketApplication.Data;

namespace TicketApplication.Models
{
    public class Ticket
    {
        public int Id { get; set; }
        public int CreatedByUserId { get; set; }

        // zuweisung nur durch support/admin
        public int? AssignedToId { get; set; }

        // inhalt, nach erstellung fix
        public TicketPriority Priority { get; set; } = TicketPriority.Low;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ExpectedResult { get; set; } = string.Empty;
        public string ActualResult { get; set; } = string.Empty;
        public bool AgreedBilling { get; set; } = false;
        public bool AgreedAGB { get; set; } = false;

        // optionaler verweis auf ein anderes ticket
        public int? ReferenceTicketId { get; set; }
        public string ReferenceComment { get; set; } = string.Empty;

        // nachträglich änderbar
        public TicketStatus Status { get; set; } = TicketStatus.Open;
        public int? AdditionalUserId1 { get; set; }
        public int? AdditionalUserId2 { get; set; }
        public int? AdditionalUserId3 { get; set; }
        public int DepartmentId { get; set; }
        public int SubjectId { get; set; }

        // zeitstempel, werden automatisch gesetzt
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public DateTime? OpenedAt { get; set; }
    }
}

using TicketApplication.Data;

namespace TicketApplication.Models
{
    public class Ticket
    {
        // Basisinformationen
        public int Id { get; set; } // Primärschlüssel
        public int TicketId { get; set; }
        public int CreatedByUserId { get; set; }
        // Nur durch Support/Admin änderbar
        public int? AssignedToId { get; set; }
        // Nachträglich unveränderlicher Ticketinhalt
        public TicketPriority Priority { get; set; } = TicketPriority.Low;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ExpectedResult { get; set; } = string.Empty;
        public string ActualResult { get; set; } = string.Empty;
        public bool AgreedBilling { get; set; } = false;
        public bool AgreedAGB { get; set; } = false;


        // Nachträglich veränderlicher Ticketinhalt
        public TicketStatus Status { get; set; } = TicketStatus.Open;
        public int? AdditionalUserId1 { get; set; }
        public int? AdditionalUserId2 { get; set; }
        public int? AdditionalUserId3 { get; set; }
        public int DepartmentId { get; set; } // Nachträglich nicht durch User änderbar
        public int SubjectId { get; set; } // Nachträglich nicht durch User änderbar


        // Autom. angepasste Werte
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public DateTime? OpenedAt { get; set; }

    }
}

using TicketApplication.Data;

namespace TicketApplication.Models
{
    public class TicketTransaction
    {
        // Hier werden alle Änderungen dokumentiert
        // Dialog-Texte werden in TicketDialogue gespeichert
        public int TicketId { get; set; } // Primärschlüssel aus Ticket
        public int TransactionId { get; set; } // Primärschlüssel
        public int ResponsibleUserId { get; set; } // Wer hat die Änderung vorgenommen?

        // Nur durch den Support/Admin änderbar
        public int? AssignedToId { get; set; }
        // Veränderlicher Ticketinhalt (aus Ticket)
        public TicketStatus Status { get; set; }
        public int? AdditionalUserId1 { get; set; }
        public int? AdditionalUserId2 { get; set; }
        public int? AdditionalUserId3 { get; set; }
        public int DepartmentId { get; set; } // Nicht durch User änderbar
        public int SubjectId { get; set; } // Nicht durch User änderbar

        public DateTime UpdatedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public DateTime? OpenedAt { get; set; }


    }
}

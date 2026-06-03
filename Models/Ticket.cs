using TicketApplication.Data;

namespace TicketApplication.Models
{
    public class Ticket
    {
        // Basisinformationen
        public int Id { get; set; } // Primärschlüssel
        public int TicketId { get; set; }
        public int CreatedByUserId { get; set; }
        //
        public int? AssignedToId { get; set; }
        // Unveränderlicher Ticketinhalt
        public TicketPriority Priority { get; set; } = TicketPriority.Low;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        // Veränderlicher Ticketinhalt
        public TicketStatus Status { get; set; } = TicketStatus.Open;
        public int? AdditionalUserId1 { get; set; }
        public int? AdditionalUserId2 { get; set; }
        public int? AdditionalUserId3 { get; set; }


        public DateTime UpdatedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public DateTime? OpenedAt { get; set; }

    }
}

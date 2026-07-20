using TicketApplication.Data;

namespace TicketApplication.Models
{
    // audit-eintrag: snapshot der änderbaren ticketfelder je änderung
    public class TicketTransaction
    {
        public int TicketId { get; set; }
        public int TransactionId { get; set; } // fortlaufend pro ticket
        public int ResponsibleUserId { get; set; }

        public int? AssignedToId { get; set; }
        public TicketStatus Status { get; set; }
        public int? AdditionalUserId1 { get; set; }
        public int? AdditionalUserId2 { get; set; }
        public int? AdditionalUserId3 { get; set; }
        public int DepartmentId { get; set; }
        public int SubjectId { get; set; }

        public DateTime UpdatedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public DateTime? OpenedAt { get; set; }
    }
}

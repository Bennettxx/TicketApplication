using TicketApplication.Data;

namespace TicketApplication.DTOs
{
    // ausgang für audit-einträge (aktuell ohne endpunkt in verwendung)
    public class TicketTransactionsDto
    {
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

using TicketApplication.Data;

namespace TicketApplication.Models
{
    // problemmeldung, abgespeckte ticket-variante, auch anonym möglich
    public class Problem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public TicketPriority Priority { get; set; } = TicketPriority.Low;
        public TicketStatus Status { get; set; } = TicketStatus.Open;

        // pflicht, für rückmeldung
        public string ContactEmail { get; set; } = string.Empty;

        // null bei anonymer meldung
        public int? CreatedByUserId { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
    }
}

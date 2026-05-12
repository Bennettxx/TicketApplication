using TicketApplication.Data;

namespace TicketApplication.Models
{
    public class Ticket
    {
        public int Id { get; set; }
        public int CreatedByUserId { get; set; }
        public int? AssignedToId { get; set; }
        public TicketStatus Status { get; set; } = TicketStatus.Open;
        public TicketPriority Priority { get; set; } = TicketPriority.Low;


        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;


        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ClosedAt { get; set; }

    }
}

using TicketApplication.Data;

namespace TicketApplication.DTOs
{
    public class UpdateTicketDto
    {
        public TicketStatus? Status { get; set; }
        public TicketPriority? Priority { get; set; }
        public int? AssignedToUserId { get; set; }
    }
}

using TicketApplication.Data;

namespace TicketApplication.Models
{
    public class TicketTransactions
    {

        public int Id { get; set; } // Primärschlüssel aus Ticket
        public int TransactionId { get; set; } // Primärschlüssel

        // Veränderlicher Ticketinhalt (aus Ticket)
        public TicketStatus Status { get; set; }

        public DateTime UpdatedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public DateTime? OpenedAt { get; set; }


    }
}

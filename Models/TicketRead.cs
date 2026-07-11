namespace TicketApplication.Models
{
    // letzter lesezeitpunkt pro user und ticket, basis für "neue antwort"-markierung
    public class TicketRead
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public int UserId { get; set; }
        public DateTime LastReadAt { get; set; }
    }
}

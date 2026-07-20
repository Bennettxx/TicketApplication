namespace TicketApplication.Models
{
    // manueller zeiteintrag zur ticketbearbeitung, basis für die statistik
    public class TicketTimeEntry
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public int UserId { get; set; } // wer hat erfasst
        public int Minutes { get; set; }
        public string Note { get; set; } = string.Empty;
        public DateTime WorkedAt { get; set; } // arbeitstag
        public DateTime CreatedAt { get; set; }
    }
}

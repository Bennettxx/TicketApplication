namespace TicketApplication.Models
{
    // einzelne chat-nachricht zu einem ticket
    public class TicketDialogue
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public int AuthorUserId { get; set; }
        public string Text { get; set; } = string.Empty;

        // interne notiz, nur für admin/support sichtbar
        public bool IsInternal { get; set; } = false;

        public DateTime CreatedAt { get; set; }
    }
}

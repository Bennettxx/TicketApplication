namespace TicketApplication.Models
{
    public class TicketDialogue
    {
        public int Id { get; set; } // Primärschlüssel aus Ticket
        public int CommentaryId { get; set; } // Primärschlüssel
        public string Text { get; set; } = string.Empty;
        public int AuthorUserId { get; set; }
        public DateTime CreatedAt { get; set; }

        // public int? AssignedDocumentId { get; set; }
    }
}

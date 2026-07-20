namespace TicketApplication.DTOs
{
    // ausgang für eine chat-nachricht
    public class DialogueResponseDto
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public int AuthorUserId { get; set; }
        public string AuthorEmail { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public bool IsInternal { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

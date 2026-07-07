namespace TicketApplication.DTOs
{
    // Ausgangs-DTO für eine Chat-Nachricht. Enthält bewusst nur Daten, die der
    // Empfänger sehen darf – z.B. die E-Mail des Autors, aber nichts Sensibles.
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

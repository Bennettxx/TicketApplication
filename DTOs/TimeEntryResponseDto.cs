namespace TicketApplication.DTOs
{
    // ausgang für einen zeiteintrag
    public class TimeEntryResponseDto
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public int UserId { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public int Minutes { get; set; }
        public string Note { get; set; } = string.Empty;
        public DateTime WorkedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

namespace TicketApplication.DTOs
{
    // ausgang für anhang-metadaten, ohne dateiinhalt
    public class AttachmentResponseDto
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public string DataName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public bool ContainsPrivateData { get; set; }
        public int UploadedByUserId { get; set; }
        public string UploadedByEmail { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}

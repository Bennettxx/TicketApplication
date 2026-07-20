namespace TicketApplication.Models
{
    // datei-anhang zu einem ticket
    // privat -> verschlüsselt in der db (DatenBase64), sonst datei auf platte (DirectoryPath)
    public class TicketAttachments
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public int UploadedByUserId { get; set; }

        public bool ContainsPrivateData { get; set; } = false;

        public string DataName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSize { get; set; }

        // base64 von nonce(12) + tag(16) + ciphertext, nur bei privaten dateien
        public string DatenBase64 { get; set; } = string.Empty;

        // relativer pfad, nur bei nicht-privaten dateien
        public string DirectoryPath { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}

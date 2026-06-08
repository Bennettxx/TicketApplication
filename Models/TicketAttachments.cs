namespace TicketApplication.Models
{
    public class TicketAttachments
    {
        // Wird irgendwann betimmt mal gebraucht ;)
        public int Id { get; set; } // Primärschlüssel
        public int TicketId { get; set; } // FK zu Ticket.Id & PK
        public bool ContainsPrivateData { get; set; } = false;
        public string DataName { get; set; } = string.Empty;
        public string DatenBase64 { get; set; } = string.Empty;
        public string DirectoryPath { get; set; } = string.Empty;

    }
}

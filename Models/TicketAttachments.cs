namespace TicketApplication.Models
{
    // Ein Datei-Anhang zu einem Ticket.
    // Zwei Speicherstrategien:
    //   - ContainsPrivateData = true  -> Datei wird AES-verschlüsselt als
    //     Base64 in der DB (Spalte DatenBase64) abgelegt. DirectoryPath bleibt leer.
    //   - ContainsPrivateData = false -> Datei wird im Projekt-Unterordner
    //     gespeichert; in DirectoryPath steht der relative Pfad. DatenBase64 bleibt leer.
    public class TicketAttachments
    {
        public int Id { get; set; }                 // Primärschlüssel
        public int TicketId { get; set; }           // FK -> Ticket.Id
        public int UploadedByUserId { get; set; }   // wer hat hochgeladen (aus JWT)

        public bool ContainsPrivateData { get; set; } = false;

        public string DataName { get; set; } = string.Empty;     // Originaldateiname
        public string ContentType { get; set; } = string.Empty;  // MIME-Typ
        public long FileSize { get; set; }                       // Größe in Bytes

        // Verschlüsselter Inhalt (nur wenn privat). Format: Base64 von
        // nonce(12) + tag(16) + ciphertext.
        public string DatenBase64 { get; set; } = string.Empty;

        // Relativer Pfad zur Datei auf der Platte (nur wenn NICHT privat).
        public string DirectoryPath { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}

using TicketApplication.Data;

namespace TicketApplication.Models
{
    // Eine "Problemmeldung" – die abgespeckte Variante eines Tickets.
    // Nur Titel, Beschreibung und Priorität; KEINE Abteilung/Subject/Anhänge.
    // Erstellbar sowohl anonym (vor Login) als auch eingeloggt.
    // Bearbeitet werden Probleme ausschließlich von Admins.
    public class Problem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public TicketPriority Priority { get; set; } = TicketPriority.Low;
        public TicketStatus Status { get; set; } = TicketStatus.Open;

        // Kontakt-E-Mail für Rückmeldung. Pflicht. Bei eingeloggten Nutzern wird
        // sie im Frontend vorbefüllt, bleibt aber änderbar.
        public string ContactEmail { get; set; } = string.Empty;

        // Ersteller, falls eingeloggt erstellt; null bei anonymer Meldung.
        public int? CreatedByUserId { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
    }
}

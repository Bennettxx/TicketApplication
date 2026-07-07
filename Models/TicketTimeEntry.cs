namespace TicketApplication.Models
{
    // Ein manuell erfasster Zeiteintrag für die Bearbeitung eines Tickets.
    // Support/Admin tragen ein, wie viele Minuten sie an einem Ticket gearbeitet
    // haben. Aus diesen Einträgen werden später die Statistiken berechnet
    // (Arbeitszeit pro Bearbeiter, Bearbeitungszeit pro Kunde).
    public class TicketTimeEntry
    {
        public int Id { get; set; }            // Primärschlüssel (Auto-Increment)
        public int TicketId { get; set; }      // FK -> Ticket.Id
        public int UserId { get; set; }        // FK -> User.Id, wer hat die Zeit erfasst (Bearbeiter)
        public int Minutes { get; set; }       // Dauer in Minuten (> 0)
        public string Note { get; set; } = string.Empty; // Optionale Beschreibung der Tätigkeit
        public DateTime WorkedAt { get; set; } // Datum, an dem gearbeitet wurde
        public DateTime CreatedAt { get; set; } // Zeitpunkt der Erfassung (autom.)
    }
}

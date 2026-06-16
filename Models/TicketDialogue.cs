namespace TicketApplication.Models
{
    // Eine einzelne Chat-Nachricht innerhalb eines Tickets.
    // Bildet die Dialog-/Chatfunktion ab: Kunde und Support schreiben sich
    // hier abwechselnd. Jede Nachricht ist ein eigener Datensatz mit eigener Id.
    public class TicketDialogue
    {
        public int Id { get; set; }                 // Primärschlüssel (Auto-Increment)
        public int TicketId { get; set; }           // FK -> Ticket.Id, zu welchem Ticket gehört die Nachricht
        public int AuthorUserId { get; set; }       // FK -> User.Id, wer hat geschrieben
        public string Text { get; set; } = string.Empty;

        // Interne Notiz: Nur für Admin/Support sichtbar, NICHT für den Kunden.
        // Normale User können das niemals auf true setzen (wird im Controller erzwungen).
        public bool IsInternal { get; set; } = false;

        public DateTime CreatedAt { get; set; }
    }
}

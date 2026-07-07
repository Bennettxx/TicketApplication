namespace TicketApplication.Models
{
    // Merkt sich, wann ein Benutzer ein Ticket zuletzt angesehen hat.
    // Grundlage für die Benachrichtigung "neue Antwort erhalten": Gibt es eine
    // fremde Nachricht, die neuer ist als dieser Zeitpunkt, gilt sie als ungelesen.
    public class TicketRead
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public int UserId { get; set; }
        public DateTime LastReadAt { get; set; }
    }
}

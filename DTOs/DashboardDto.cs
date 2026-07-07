namespace TicketApplication.DTOs
{
    // Kennzahlen für das Dashboard. Inhalt ist rollenabhängig:
    // Staff sieht systemweite Zahlen, ein normaler User nur seine eigenen.
    public class DashboardDto
    {
        public string Role { get; set; } = string.Empty;
        public bool IsStaff { get; set; }

        // Sichtbarer Bestand (Staff: alle Tickets, User: eigene Tickets)
        public int OpenCount { get; set; }
        public int InProgressCount { get; set; }
        public int ClosedCount { get; set; }
        public int TotalCount { get; set; }

        // Nur für Staff sinnvoll
        public int MyAssignedOpenCount { get; set; } // mir zugewiesen und nicht geschlossen
        public int UnassignedOpenCount { get; set; } // offen und niemandem zugewiesen

        // Anzahl Tickets mit ungelesener fremder Antwort (Benachrichtigung).
        public int UnreadReplyCount { get; set; }

        // Letzte Tickets (Kurzform)
        public List<DashboardTicketDto> Recent { get; set; } = new();
    }

    public class DashboardTicketDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int StatusCode { get; set; }
        public int PriorityCode { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

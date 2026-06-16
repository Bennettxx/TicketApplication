namespace TicketApplication.DTOs
{
    // Statistik pro Bearbeiter (Admin/Support):
    // Wie viele Tickets sind zugewiesen, wie viele wurden geschlossen,
    // wie viel Zeit wurde insgesamt erfasst.
    public class AgentStatsDto
    {
        public int UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int AssignedTicketCount { get; set; }   // aktuell zugewiesene Tickets
        public int ClosedTicketCount { get; set; }     // von diesem Bearbeiter geschlossene Tickets
        public int TotalMinutes { get; set; }          // erfasste Arbeitszeit gesamt
    }

    // Statistik pro Kunde:
    // Wie viele Tickets hat der Kunde erstellt und wie viel Bearbeitungszeit
    // haben diese Tickets insgesamt verursacht.
    public class CustomerStatsDto
    {
        public int UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty; // Abteilung des Kunden
        public int TicketCount { get; set; }           // erstellte Tickets
        public int OpenTicketCount { get; set; }       // davon noch nicht geschlossen
        public int TotalMinutes { get; set; }          // Summe der Bearbeitungszeit aller seiner Tickets
    }

    // Statistik pro Abteilung:
    // Kategorisiert die Bearbeitungszeit nach der Abteilung des Erstellers
    // (Kunden). So sieht man, welche Abteilung wie viel Support-Zeit verursacht.
    public class DepartmentStatsDto
    {
        public int? DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public int UserCount { get; set; }             // Benutzer in dieser Abteilung
        public int TicketCount { get; set; }           // von diesen Benutzern erstellte Tickets
        public int OpenTicketCount { get; set; }       // davon noch nicht geschlossen
        public int TotalMinutes { get; set; }          // Summe der Bearbeitungszeit dieser Tickets
    }
}

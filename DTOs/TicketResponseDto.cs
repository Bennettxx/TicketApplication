namespace TicketApplication.DTOs
{
    public class TicketResponseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ExpectedResult { get; set; } = string.Empty;
        public string ActualResult { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;     // Enum -> lesbarer String
        public int StatusCode { get; set; }                    // Enum-Zahl (fürs Kanban-Board)
        public string Priority { get; set; } = string.Empty;   // Enum -> lesbarer String
        public int PriorityCode { get; set; }                  // Enum-Zahl (zum Sortieren/Farbe)

        public int CreatedByUserId { get; set; }
        public string CreatedByEmail { get; set; } = string.Empty;
        public int? AssignedToUserId { get; set; }
        public string? AssignedToEmail { get; set; }

        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public int SubjectId { get; set; }
        public string SubjectName { get; set; } = string.Empty;

        // Summe aller erfassten Bearbeitungsminuten dieses Tickets.
        public int TotalMinutes { get; set; }

        // true, wenn es für den aktuellen Nutzer eine ungelesene fremde Antwort
        // gibt (für die Kennzeichnung auf Startseite/Dashboard).
        public bool HasUnreadReply { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
    }
}

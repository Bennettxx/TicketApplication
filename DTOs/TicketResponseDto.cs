namespace TicketApplication.DTOs
{
    // ausgang für tickets, angereichert mit namen/mails/minuten
    public class TicketResponseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ExpectedResult { get; set; } = string.Empty;
        public string ActualResult { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int StatusCode { get; set; } // enum-zahl fürs kanban
        public string Priority { get; set; } = string.Empty;
        public int PriorityCode { get; set; } // enum-zahl für sortierung/farbe

        public int CreatedByUserId { get; set; }
        public string CreatedByEmail { get; set; } = string.Empty;
        public int? AssignedToUserId { get; set; }
        public string? AssignedToEmail { get; set; }

        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public int SubjectId { get; set; }
        public string SubjectName { get; set; } = string.Empty;

        // optionaler verweis auf anderes ticket
        public int? ReferenceTicketId { get; set; }
        public string ReferenceComment { get; set; } = string.Empty;

        // summe der erfassten minuten
        public int TotalMinutes { get; set; }

        // true wenn ungelesene fremde antwort vorliegt
        public bool HasUnreadReply { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
    }
}

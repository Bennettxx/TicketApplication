namespace TicketApplication.DTOs
{
    // statistik pro bearbeiter
    public class AgentStatsDto
    {
        public int UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int AssignedTicketCount { get; set; }
        public int ClosedTicketCount { get; set; }
        public int TotalMinutes { get; set; }
    }

    // statistik pro kunde
    public class CustomerStatsDto
    {
        public int UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public int TicketCount { get; set; }
        public int OpenTicketCount { get; set; }
        public int TotalMinutes { get; set; }
    }

    // statistik pro abteilung, kategorisiert nach abteilung der ersteller
    public class DepartmentStatsDto
    {
        public int? DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public int UserCount { get; set; }
        public int TicketCount { get; set; }
        public int OpenTicketCount { get; set; }
        public int TotalMinutes { get; set; }
    }
}

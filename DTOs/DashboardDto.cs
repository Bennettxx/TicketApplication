namespace TicketApplication.DTOs
{
    // kennzahlen fürs dashboard, staff systemweit, user nur eigene
    public class DashboardDto
    {
        public string Role { get; set; } = string.Empty;
        public bool IsStaff { get; set; }

        public int OpenCount { get; set; }
        public int InProgressCount { get; set; }
        public int ClosedCount { get; set; }
        public int TotalCount { get; set; }

        // nur für staff relevant
        public int MyAssignedOpenCount { get; set; }
        public int UnassignedOpenCount { get; set; }

        // tickets mit ungelesener fremder antwort
        public int UnreadReplyCount { get; set; }

        // letzte tickets, kurzform
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

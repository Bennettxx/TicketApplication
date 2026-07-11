namespace TicketApplication.DTOs
{
    // ausgang für abteilungen (auswahllisten)
    public class DepartmentDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    // ausgang für themen
    public class SubjectDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int DepartmentId { get; set; }
        public bool IsVerified { get; set; }
    }
}

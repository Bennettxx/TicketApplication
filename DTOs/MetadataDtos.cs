namespace TicketApplication.DTOs
{
    // Ausgangs-DTO für eine Abteilung (Auswahllisten im Frontend).
    public class DepartmentDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    // Ausgangs-DTO für ein Subject/Thema.
    public class SubjectDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int DepartmentId { get; set; }
        public bool IsVerified { get; set; }
    }
}

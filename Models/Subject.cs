namespace TicketApplication.Models
{
    public class Subject
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int DepartmentId { get; set; }
        public bool IsVerified { get; set; }

    }
}

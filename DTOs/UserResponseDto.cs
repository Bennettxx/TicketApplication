namespace TicketApplication.DTOs
{
    // ausgang für benutzerdaten, ohne passworthash
    public class UserResponseDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string SecondName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActivated { get; set; }
        public bool IsActive { get; set; } = true;

        public int? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
    }
}

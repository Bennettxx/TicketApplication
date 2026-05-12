namespace TicketApplication.DTOs
{
    // Diese DTO-Klasse dient dazu, die Informationen eines Benutzers zu übertragen, ohne sensible Daten wie das Passwort zu enthalten.
    public class UserResponseDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string SecondName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActivated { get; set; }
    }
}

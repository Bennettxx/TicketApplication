using TicketApplication.Data;

namespace TicketApplication.Models
{
    public class User
    {
        public int Id { get; set; } = 0;// Primärschlüssel
        public string FirstName { get; set; } = string.Empty;
        public string SecondName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public UserRole Role { get; set; } = UserRole.User; // Später darf nur der Admin dies anpassen!!!
        public bool IsEmailConfirmed { get; set; } = false; // Evt. vor Bestätigung keine Beschwrden einreichen können oä.
        public bool IsActive { get; set; } = true;
    }
}
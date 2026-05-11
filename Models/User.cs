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

        // IsActivated: Wird beim Register auf false gesetzt. Ein Admin muss das Konto manuell freischalten.
        public bool IsActivated { get; set; } = false;

        // IsActive: Soft-Delete-Flag. Wird auf false gesetzt wenn ein Admin den User "ablehnt" oder deaktiviert.
        // WICHTIG: User werden NIE aus der DB gelöscht, nur deaktiviert!
        public bool IsActive { get; set; } = true;
    }
}
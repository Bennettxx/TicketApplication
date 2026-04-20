namespace TicketApplication.Models
{
    public class User
    {
        public int Id { get; set; } // Primärschlüssel
        public string FirstName { get; set; } = string.Empty;
        public string SecondName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public Boolean IsAdmin { get; set; } = false; // Später darf nur der Admin dies anpassen!!! Set-restriction
    }
}
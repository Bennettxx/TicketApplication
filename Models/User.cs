using System.ComponentModel.DataAnnotations;
using TicketApplication.Data;

namespace TicketApplication.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; } = 0;
        public string FirstName { get; set; } = string.Empty;
        public string SecondName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;

        // rolle ändert nur der admin
        public UserRole Role { get; set; } = UserRole.User;

        // freischaltung durch admin nach registrierung
        public bool IsActivated { get; set; } = false;

        // abteilung, nur für rolle user
        public int? DepartmentId { get; set; }

        // soft-delete / sperre, user werden nie physisch gelöscht
        public bool IsActive { get; set; } = true;
    }
}

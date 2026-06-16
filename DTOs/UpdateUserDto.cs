using System.ComponentModel.DataAnnotations;
using TicketApplication.Data;
using TicketApplication.Functions;
using TicketApplication.Models;

namespace TicketApplication.DTOs
{
    // Partielles Update durch einen Admin: Jedes Feld ist OPTIONAL.
    // Nur gesetzte (nicht-null) Felder werden im Controller übernommen.
    // Deshalb KEIN [Required] hier – sonst müsste man immer alle Felder mitschicken.
    public class UpdateUserDto
    {
        [MaxLength(50, ErrorMessage = "Vorname darf maximal 50 Zeichen lang sein.")]
        public string? FirstName { get; set; }

        [MaxLength(50, ErrorMessage = "Nachname darf maximal 50 Zeichen lang sein.")]
        public string? SecondName { get; set; }

        [EmailAddress(ErrorMessage = "Keine gültige E-Mail-Adresse.")]
        public string? Email { get; set; }

        [EnumDataType(typeof(UserRole), ErrorMessage = "Ungültige Rolle.")]
        public UserRole? Role { get; set; }

        // Optionale Abteilung (muss existieren, wenn gesetzt).
        [ExistsInColumn(typeof(Department), "Name", ErrorMessage = "Abteilung nicht gefunden.")]
        public string? DepartmentName { get; set; }

        public bool? IsActivated { get; set; }
        public bool? IsActive { get; set; }
    }
}

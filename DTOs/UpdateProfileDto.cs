using System.ComponentModel.DataAnnotations;
using TicketApplication.Functions;
using TicketApplication.Models;

namespace TicketApplication.DTOs
{
    // Partielles Update des eigenen Profils: Jedes Feld ist OPTIONAL.
    // Nur gesetzte Felder werden übernommen (siehe AccountController.UpdateMe).
    // Die E-Mail ist BEWUSST nicht enthalten – sie darf nicht geändert werden.
    public class UpdateProfileDto
    {
        [MaxLength(50, ErrorMessage = "Vorname darf maximal 50 Zeichen lang sein.")]
        public string? FirstName { get; set; }

        [MaxLength(50, ErrorMessage = "Nachname darf maximal 50 Zeichen lang sein.")]
        public string? SecondName { get; set; }

        // Abteilung (nur für Rolle User relevant). Muss existieren, wenn gesetzt.
        [ExistsInColumn(typeof(Department), "Name", ErrorMessage = "Abteilung nicht gefunden.")]
        public string? DepartmentName { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
using TicketApplication.Functions;
using TicketApplication.Models;

namespace TicketApplication.DTOs
{
    // Eingangs-DTO für die Zuweisung eines Tickets an einen Bearbeiter.
    // Eintrittstor: Die E-Mail muss zu einem existierenden User gehören.
    // null bedeutet "Zuweisung entfernen".
    public class AssignTicketDto
    {
        [ExistsInColumn(typeof(User), "Email", ErrorMessage = "Bearbeiter (E-Mail) nicht gefunden.")]
        public string? AssignToEmail { get; set; }
    }
}

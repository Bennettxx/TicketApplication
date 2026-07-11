using TicketApplication.Functions;
using TicketApplication.Models;

namespace TicketApplication.DTOs
{
    // eingang für die zuweisung, null = zuweisung entfernen
    public class AssignTicketDto
    {
        [ExistsInColumn(typeof(User), "Email", ErrorMessage = "Bearbeiter (E-Mail) nicht gefunden.")]
        public string? AssignToEmail { get; set; }
    }
}

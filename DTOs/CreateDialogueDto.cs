using System.ComponentModel.DataAnnotations;

namespace TicketApplication.DTOs
{
    // Eingangs-DTO für eine neue Chat-Nachricht zu einem Ticket.
    // Das ist ein Eintrittstor: Hier wird streng geprüft, was hereinkommt.
    public class CreateDialogueDto
    {
        [Required(ErrorMessage = "Nachrichtentext ist erforderlich.")]
        // Mindestlänge 1 sichtbares Zeichen, maximal 4000 – verhindert leere
        // und übergroße Nachrichten.
        [MinLength(1, ErrorMessage = "Nachricht darf nicht leer sein.")]
        [MaxLength(4000, ErrorMessage = "Nachricht darf maximal 4000 Zeichen lang sein.")]
        public string Text { get; set; } = string.Empty;

        // Wunsch des Absenders, dass dies eine interne Notiz ist.
        // ACHTUNG: Ob das wirklich erlaubt ist, entscheidet der Controller
        // anhand der Rolle – ein normaler User kann das nicht erzwingen.
        public bool IsInternal { get; set; } = false;
    }
}

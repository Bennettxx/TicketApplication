using System.ComponentModel.DataAnnotations;

namespace TicketApplication.DTOs
{
    // Eingangs-DTO für das Wiedereröffnen eines geschlossenen Tickets.
    // Die Nachricht ist PFLICHT (Begründung, warum wieder geöffnet wird) und
    // wird als Dialog-Nachricht am Ticket gespeichert.
    public class ReopenTicketDto
    {
        [Required(ErrorMessage = "Eine Nachricht ist beim Wiedereröffnen erforderlich.")]
        [MinLength(1, ErrorMessage = "Nachricht darf nicht leer sein.")]
        [MaxLength(4000, ErrorMessage = "Nachricht darf maximal 4000 Zeichen lang sein.")]
        public string Message { get; set; } = string.Empty;
    }
}

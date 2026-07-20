using System.ComponentModel.DataAnnotations;

namespace TicketApplication.DTOs
{
    // eingang fürs wiedereröffnen, begründung ist pflicht und landet im dialog
    public class ReopenTicketDto
    {
        [Required(ErrorMessage = "Eine Nachricht ist beim Wiedereröffnen erforderlich.")]
        [MinLength(1, ErrorMessage = "Nachricht darf nicht leer sein.")]
        [MaxLength(4000, ErrorMessage = "Nachricht darf maximal 4000 Zeichen lang sein.")]
        public string Message { get; set; } = string.Empty;
    }
}

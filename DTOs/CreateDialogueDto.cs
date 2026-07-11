using System.ComponentModel.DataAnnotations;

namespace TicketApplication.DTOs
{
    // eingang für neue chat-nachricht
    public class CreateDialogueDto
    {
        [Required(ErrorMessage = "Nachrichtentext ist erforderlich.")]
        [MinLength(1, ErrorMessage = "Nachricht darf nicht leer sein.")]
        [MaxLength(4000, ErrorMessage = "Nachricht darf maximal 4000 Zeichen lang sein.")]
        public string Text { get; set; } = string.Empty;

        // wunsch "interne notiz", ob erlaubt entscheidet der controller anhand der rolle
        public bool IsInternal { get; set; } = false;
    }
}

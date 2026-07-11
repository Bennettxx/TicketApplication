using System.ComponentModel.DataAnnotations;
using TicketApplication.Functions;

namespace TicketApplication.DTOs
{
    // eingang für einen zeiteintrag
    public class CreateTimeEntryDto
    {
        [Required(ErrorMessage = "Minuten sind erforderlich.")]
        [Range(1, 1440, ErrorMessage = "Minuten müssen zwischen 1 und 1440 (24h) liegen.")]
        public int Minutes { get; set; }

        [MaxLength(500, ErrorMessage = "Notiz darf maximal 500 Zeichen lang sein.")]
        public string? Note { get; set; }

        [Required(ErrorMessage = "Arbeitsdatum ist erforderlich.")]
        [NotInFuture(ErrorMessage = "Das Arbeitsdatum darf nicht in der Zukunft liegen.")]
        public DateTime WorkedAt { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
using TicketApplication.Functions;

namespace TicketApplication.DTOs
{
    // Eingangs-DTO für einen manuellen Zeiteintrag (Zeiterfassung).
    // Eintrittstor: Minuten müssen sinnvoll sein, das Datum nicht in der Zukunft.
    public class CreateTimeEntryDto
    {
        [Required(ErrorMessage = "Minuten sind erforderlich.")]
        // 1 Minute bis 1440 Minuten (= 24 Stunden) pro einzelnem Eintrag.
        // Verhindert 0/negative Werte und unrealistische Ausreißer.
        [Range(1, 1440, ErrorMessage = "Minuten müssen zwischen 1 und 1440 (24h) liegen.")]
        public int Minutes { get; set; }

        [MaxLength(500, ErrorMessage = "Notiz darf maximal 500 Zeichen lang sein.")]
        public string? Note { get; set; }

        [Required(ErrorMessage = "Arbeitsdatum ist erforderlich.")]
        [NotInFuture(ErrorMessage = "Das Arbeitsdatum darf nicht in der Zukunft liegen.")]
        public DateTime WorkedAt { get; set; }
    }
}

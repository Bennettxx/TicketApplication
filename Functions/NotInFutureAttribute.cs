using System.ComponentModel.DataAnnotations;

namespace TicketApplication.Functions
{
    // Validierungs-Attribut für Datumsfelder: Der Wert darf nicht in der
    // Zukunft liegen. Genutzt z.B. für das Arbeitsdatum eines Zeiteintrags –
    // man kann keine Zeit für die Zukunft erfassen.
    // Erlaubt einen kleinen Puffer (1 Tag), damit Zeitzonen-/Uhr-Differenzen
    // zwischen Client und Server nicht fälschlich abgelehnt werden.
    [AttributeUsage(AttributeTargets.Property)]
    public class NotInFutureAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext context)
        {
            // null wird hier durchgelassen – Pflicht/Optional regelt [Required].
            if (value == null)
                return ValidationResult.Success;

            if (value is DateTime dt)
            {
                if (dt.ToUniversalTime() > DateTime.UtcNow.AddDays(1))
                    return new ValidationResult(ErrorMessage ?? "Das Datum darf nicht in der Zukunft liegen.");
                return ValidationResult.Success;
            }

            return new ValidationResult("Ungültiges Datumsformat.");
        }
    }
}

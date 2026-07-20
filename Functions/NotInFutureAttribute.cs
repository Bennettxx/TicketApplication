using System.ComponentModel.DataAnnotations;

namespace TicketApplication.Functions
{
    // validierung: datum darf nicht in der zukunft liegen
    // 1 tag puffer wegen zeitzonen-differenzen zwischen client und server
    [AttributeUsage(AttributeTargets.Property)]
    public class NotInFutureAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext context)
        {
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

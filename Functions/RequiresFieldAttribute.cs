using System.ComponentModel.DataAnnotations;

namespace TicketApplication.Functions
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class RequiresFieldAttribute : ValidationAttribute
    {
        private readonly string _requiredField;

        public RequiresFieldAttribute(string requiredField)
        {
            _requiredField = requiredField;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext context)
        {
            if (value == null) return ValidationResult.Success;

            var property = context.ObjectType.GetProperty(_requiredField);
            var otherValue = property?.GetValue(context.ObjectInstance);

            if (otherValue == null || string.IsNullOrWhiteSpace(otherValue.ToString()))
            {
                return new ValidationResult(ErrorMessage ?? $"{_requiredField} muss gesetzt sein.");
            }

            return ValidationResult.Success;
        }
    }
}

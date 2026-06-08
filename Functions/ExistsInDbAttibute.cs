using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using TicketApplication.Data;

namespace TicketApplication.Functions
{
    public class ExistsInColumnAttribute : ValidationAttribute
    {
        private readonly Type _entityType;
        private readonly string _columnName;

        public ExistsInColumnAttribute(Type entityType, string columnName)
        {
            _entityType = entityType;
            _columnName = columnName;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext context)
        {
            if (value == null)
                return new ValidationResult("Wert darf nicht leer sein.");

            var db = context.GetRequiredService<ApplicationDbContext>();

            // Dynamisch in beliebiger Spalte suchen
            var entity = db.Model.FindEntityType(_entityType);
            var dbSet = (IQueryable)db.GetType()
                .GetMethod("Set", Type.EmptyTypes)!
                .MakeGenericMethod(_entityType)
                .Invoke(db, null)!;

            var param = Expression.Parameter(_entityType, "e");
            var property = Expression.Property(param, _columnName);
            var constant = Expression.Constant(value, property.Type);
            var equals = Expression.Equal(property, constant);
            var lambda = Expression.Lambda(equals, param);

            var anyMethod = typeof(Queryable)
                .GetMethods()
                .First(m => m.Name == "Any" && m.GetParameters().Length == 2)
                .MakeGenericMethod(_entityType);

            var exists = (bool)anyMethod.Invoke(null, new object[] { dbSet, lambda })!;

            return exists
                ? ValidationResult.Success
                : new ValidationResult(ErrorMessage ?? $"Wert '{value}' nicht gefunden.");
        }
    }
}

using System.ComponentModel.DataAnnotations;
using TicketApplication.Functions;
using TicketApplication.Models;

namespace TicketApplication.DTOs
{
    // Eingangs-DTO zum Anlegen eines Wissensartikels (Admin/Support).
    public class CreateKnowledgeArticleDto
    {
        [Required(ErrorMessage = "Titel ist erforderlich.")]
        [MinLength(3, ErrorMessage = "Titel muss mindestens 3 Zeichen lang sein.")]
        [MaxLength(200, ErrorMessage = "Titel darf maximal 200 Zeichen lang sein.")]
        public string Title { get; set; } = string.Empty;

        [MaxLength(300, ErrorMessage = "Stichwörter dürfen maximal 300 Zeichen lang sein.")]
        public string? Keywords { get; set; }

        [Required(ErrorMessage = "Lösungstext ist erforderlich.")]
        [MaxLength(5000, ErrorMessage = "Lösungstext darf maximal 5000 Zeichen lang sein.")]
        public string Solution { get; set; } = string.Empty;

        // Optionale Zuordnung zu einer Abteilung (muss existieren, wenn gesetzt).
        [ExistsInColumn(typeof(Department), "Name", ErrorMessage = "Abteilung nicht gefunden.")]
        public string? DepartmentName { get; set; }

        public bool IsPublished { get; set; } = true;
    }

    // Eingangs-DTO zum Ändern eines Artikels. Alle Felder optional.
    public class UpdateKnowledgeArticleDto
    {
        [MinLength(3, ErrorMessage = "Titel muss mindestens 3 Zeichen lang sein.")]
        [MaxLength(200, ErrorMessage = "Titel darf maximal 200 Zeichen lang sein.")]
        public string? Title { get; set; }

        [MaxLength(300, ErrorMessage = "Stichwörter dürfen maximal 300 Zeichen lang sein.")]
        public string? Keywords { get; set; }

        [MaxLength(5000, ErrorMessage = "Lösungstext darf maximal 5000 Zeichen lang sein.")]
        public string? Solution { get; set; }

        [ExistsInColumn(typeof(Department), "Name", ErrorMessage = "Abteilung nicht gefunden.")]
        public string? DepartmentName { get; set; }

        public bool? IsPublished { get; set; }
    }

    // Ausgangs-DTO für die Verwaltung (volle Daten).
    public class KnowledgeArticleDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Keywords { get; set; } = string.Empty;
        public string Solution { get; set; } = string.Empty;
        public int? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public bool IsPublished { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    // Ausgangs-DTO für einen Vorschlag beim Ticket-Erstellen (kompakt).
    public class KnowledgeSuggestionDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Solution { get; set; } = string.Empty;
    }
}

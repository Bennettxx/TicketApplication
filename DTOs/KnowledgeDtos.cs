using System.ComponentModel.DataAnnotations;
using TicketApplication.Functions;
using TicketApplication.Models;

namespace TicketApplication.DTOs
{
    // eingang für neuen wissensartikel
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

        [ExistsInColumn(typeof(Department), "Name", ErrorMessage = "Abteilung nicht gefunden.")]
        public string? DepartmentName { get; set; }

        public bool IsPublished { get; set; } = true;
    }

    // teilupdate eines artikels, alle felder optional
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

    // ausgang für die verwaltung
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

    // ausgang für lösungsvorschläge beim ticket-erstellen
    public class KnowledgeSuggestionDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Solution { get; set; } = string.Empty;
    }
}

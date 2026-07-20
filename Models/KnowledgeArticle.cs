namespace TicketApplication.Models
{
    // wissensartikel, wird beim ticket-erstellen als lösungsvorschlag angeboten
    public class KnowledgeArticle
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Keywords { get; set; } = string.Empty; // suchbegriffe
        public string Solution { get; set; } = string.Empty;

        public int? DepartmentId { get; set; }
        public bool IsPublished { get; set; } = true; // nur veröffentlichte werden vorgeschlagen

        public int CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

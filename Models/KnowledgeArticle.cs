namespace TicketApplication.Models
{
    // Ein Wissensartikel / Lösungsvorschlag.
    // Beim Erstellen eines Tickets werden anhand des Titels passende Artikel
    // vorgeschlagen, damit der Nutzer sein Problem ggf. selbst lösen kann.
    public class KnowledgeArticle
    {
        public int Id { get; set; }                         // Primärschlüssel
        public string Title { get; set; } = string.Empty;   // Kurztitel / Problem
        public string Keywords { get; set; } = string.Empty;// Stichwörter (für die Suche), durch Komma/Leerzeichen getrennt
        public string Solution { get; set; } = string.Empty;// Lösungstext

        public int? DepartmentId { get; set; }              // optionale Zuordnung
        public bool IsPublished { get; set; } = true;       // nur veröffentlichte werden vorgeschlagen

        public int CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

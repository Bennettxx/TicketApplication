using System.ComponentModel.DataAnnotations;

namespace TicketApplication.DTOs
{
    // ausgang für abteilungen (auswahllisten)
    public class DepartmentDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    // ausgang für die abteilungsverwaltung, inkl. nutzungszähler für den löschschutz
    public class DepartmentAdminDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int UserCount { get; set; }
        public int TicketCount { get; set; }
        public int SubjectCount { get; set; }
    }

    // eingang für anlegen/umbenennen einer abteilung
    public class SaveDepartmentDto
    {
        [Required(ErrorMessage = "Name ist erforderlich.")]
        [MinLength(2, ErrorMessage = "Name muss mindestens 2 Zeichen lang sein.")]
        [MaxLength(100, ErrorMessage = "Name darf maximal 100 Zeichen lang sein.")]
        public string Name { get; set; } = string.Empty;
    }

    // ausgang für themen
    public class SubjectDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int DepartmentId { get; set; }
        public bool IsVerified { get; set; }
    }
}

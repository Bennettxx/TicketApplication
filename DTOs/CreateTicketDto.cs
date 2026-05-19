using System.ComponentModel.DataAnnotations;
using TicketApplication.Data;

namespace TicketApplication.DTOs
{
    public class CreateTicketDto
    {
        [Required(ErrorMessage = "Titel ist erforderlich.")]
        [MaxLength(200, ErrorMessage = "Titel darf maximal 200 Zeichen lang sein.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Beschreibung ist erforderlich.")]
        public string Description { get; set; } = string.Empty;

        public TicketPriority Priority { get; set; } = TicketPriority.Low;
        public TicketCategory Category { get; set; } = TicketCategory.Normal;
    }
}

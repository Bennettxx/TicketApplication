using System.ComponentModel.DataAnnotations;
using TicketApplication.Data;

namespace TicketApplication.DTOs
{
    // eingang für statuswechsel, ticket-id kommt aus der url
    public class UpdateTicketStatusDto
    {
        [Required]
        [EnumDataType(typeof(TicketStatus), ErrorMessage = "Ungültiger Status.")]
        public TicketStatus Status { get; set; }
    }
}

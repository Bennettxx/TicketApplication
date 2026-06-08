using System.ComponentModel.DataAnnotations;
using TicketApplication.Data;
using TicketApplication.Functions;
using TicketApplication.Models;

namespace TicketApplication.DTOs
{
    public class UpdateTicketStatusDto  
    {
        [Required]
        [ExistsInColumn(typeof(Ticket), "Id", ErrorMessage = "Ticket nicht gefunden.")]
        public int TicketId { get; set; }

        // Es wird die Zahl übergeben
        [Required]
        [EnumDataType(typeof(TicketStatus), ErrorMessage = "Ungültiger Status.")]
        public TicketStatus Status { get; set; }
    }
}

namespace TicketApplication.DTOs
{
    public class LoginDto
    {
        // DTO nur für den Einlogg-Prozess, damit nicht die ganze User-Klasse mitgeschickt werden muss
        // Einzige Stelle wo Password im Klartext übergeben wird, noch nicht gehasht
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}

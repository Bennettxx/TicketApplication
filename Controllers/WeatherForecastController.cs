using TicketApplication.Models;
using Microsoft.AspNetCore.Mvc;

namespace TicketApplication.Controllers
{
    // Sagt .NET "das ist kein normales Programm, sondern eine WEB-Schnittstelle"
    [ApiController]
    // Legt die Adresse fest: bsphost.de/(name der Klasse) (hier [controller] = WeatherForecast)
    // Man kann auch andere Strings einfügen und die Adresse danach benennen
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries =
        [
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        ];

        // HttpGet ist Methode, die ausgeführt wird, wenn jemand Daten abrufen will
        // HttpPost wäre Pendant für das Senden an API (oder speichern über diese)
        // Steht im direkten Zusammenhang mit der darunterstehenden Methode und erlaubt diese bei HttpGet Anfragen zu berücksichtigen
        [HttpGet(Name = "GetWeatherForecast")]

        // IEnumerable<> ist das Interface einer aufzählbaren Liste (rein aufgelistete Werte)
        // Enumerable ist die Liste, auf welche man versch. Operationen wie .OrderBy() etc. anwenden kann
        public IEnumerable<WeatherForecast> Get()
        {
            // index nimmt in jeder Iteration durch die Enumerable-Liste 1,2,3,4,5 an und erzeugt eine neuen Instanz der Klasse WeatherForecast
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                // für jede Instanz (Iteration durch Enum. wird dies einmal durchgeführt)
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }

        // Hiermit eröffnen wir eine Sub-Route
        // die wäre: .../weatherforecast/heute
        // weiter Verzweigungen auf Basis von weatherforecast wären so zu bauen:
        // [HttpGet("heute/beispielverzweigung")] und folgend die Methode
        // der GET würde dann sein: GET {{LocalHost_HostAddress}}/weatherforecast/heute/beispiel
        [HttpGet("heute")]
        public WeatherForecast GetToday()
        {
            return new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now),
                TemperatureC = 25,
                Summary = "Sonnig (Manuell erstellt)"
            };
        }
    }
}

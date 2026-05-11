using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Diagnostics.Eventing.Reader;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using TicketApplication.Data;

var builder = WebApplication.CreateBuilder(args);

// =========================================================================
// LOKALE KONFIGURATION
// Lädt appsettings.Local.json zusätzlich zu appsettings.json.
// Diese Datei steht in der .gitignore und enthält maschinenspezifische
// Werte: Connection-String, JWT-Key. Gehört NICHT ins Repo.
// =========================================================================
builder.Configuration.AddJsonFile(
    "appsettings.Local.json",
    optional: true,
    reloadOnChange: true);

// JWT-Key prüfen — falls keiner gesetzt ist (z.B. beim allerersten Start),
// einen kryptographisch sicheren generieren und in appsettings.Local.json
// speichern. So bekommt jede Installation ihren eigenen einzigartigen Key.
EnsureJwtKeyExists(builder);

// Connection-String prüfen — sonst kommt später eine sehr kryptische
// Fehlermeldung aus dem EF-Core-Innern.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "ConnectionStrings:DefaultConnection ist nicht konfiguriert. " +
        "Bitte 'appsettings.Local.json' anlegen — Vorlage: 'appsettings.Local.json.example'.");
}

// Add services to the container. Auch Dependency Injection genannt - welche Services stehen später zur Verfügung.

// Hier binden wir die DB an die App, damit wir später in den Controllern darauf zugreifen können
// DbContext ist die Klasse, die die DB abbildet und den Zugriff ermöglicht
// Für debugging .LogTo(Console.WriteLine, LogLevel.Information)    Dieses später auskommentieren!
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString).LogTo(Console.WriteLine, LogLevel.Information));

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });

builder.Services.AddAuthorization();

// App-Objekt wird erstellt mit oben implementierten Var etc.
// Ab dem Build() können keine neuen Services hinzugefügt werden, sondern nurnoch der Ablaufplan (Middelware)
var app = builder.Build();

// Ab hier können wir den Ablaufplan der App festlegen (Middelware)

// Hier können wir festlegen, welche Routen für die API-Dokumentation zuständig sind
// In der Entwicklungsumgebung wollen wir die API-Dokumentation sehen, in Produktion nicht
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// Leitet HTTP Aufrufe als HTTPS weiter
app.UseHttpsRedirection();

app.UseAuthentication();

if (app.Environment.IsDevelopment())
{
    app.Use(async (context, next) =>
    {
        if (context.Request.Headers["Authorization"] == "Dev-Admin")
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "0"),
                new Claim(ClaimTypes.Name, "DevAdmin"),
                new Claim(ClaimTypes.Role, "Admin")
            };
            // "DevAuth" als AuthenticationType macht IsAuthenticated = true
            var identity = new ClaimsIdentity(claims, "DevAuth");
            context.User = new ClaimsPrincipal(identity);
        }
        await next();
    });
}

app.UseAuthorization();

// Hier werden die Controller-Routen aktiviert, damit die App auf HTTP-Anfragen reagieren kann
app.MapControllers();

// Hier kann man ergänzen was vor dem Start geschehen soll (unabhängig der Bedienung der App)

// Wir erstellen einen Scope, damit wir die DB-Initialisierung durchführen können
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    // Wir holen uns den 'Dolmetscher' aus dem System
    var context = services.GetRequiredService<ApplicationDbContext>();

    // Wir rufen unsere neue Klasse auf
    DbInitializer.Initialize(context);
}

app.Run();


// =========================================================================
// HILFSFUNKTIONEN
// =========================================================================

// Stellt sicher dass ein JWT-Signing-Key existiert.
// Wenn keiner in der Konfiguration steht (z.B. allererster Start nach Klonen
// des Repos), generieren wir einen kryptographisch sicheren Zufallswert und
// speichern ihn in appsettings.Local.json. Jede lokale Installation bekommt
// so automatisch ihren eigenen einzigartigen Schlüssel.
static void EnsureJwtKeyExists(WebApplicationBuilder builder)
{
    var existingKey = builder.Configuration["Jwt:Key"];
    if (!string.IsNullOrWhiteSpace(existingKey))
        return;  // Key vorhanden — nichts zu tun.

    // 32 Bytes = 256 Bits Entropie. In Hex codiert ergibt das 64 Zeichen.
    // RandomNumberGenerator ist der OS-gestützte Krypto-Zufallsgenerator
    // (NICHT das normale Random — das wäre für Sicherheitszwecke ungeeignet).
    var randomBytes = new byte[32];
    RandomNumberGenerator.Fill(randomBytes);
    var newKey = Convert.ToHexString(randomBytes);

    // appsettings.Local.json laden (falls vorhanden), den Jwt:Key setzen,
    // wieder zurückschreiben. Wir merge'n — bestehende Werte wie der
    // ConnectionString bleiben unangetastet.
    var localJsonPath = Path.Combine(builder.Environment.ContentRootPath, "appsettings.Local.json");
    JsonObject root;
    if (File.Exists(localJsonPath))
    {
        var content = File.ReadAllText(localJsonPath);
        root = string.IsNullOrWhiteSpace(content)
            ? new JsonObject()
            : JsonNode.Parse(content)!.AsObject();
    }
    else
    {
        root = new JsonObject();
    }

    if (root["Jwt"] is not JsonObject jwtSection)
    {
        jwtSection = new JsonObject();
        root["Jwt"] = jwtSection;
    }
    jwtSection["Key"] = newKey;

    var writeOptions = new JsonSerializerOptions { WriteIndented = true };
    File.WriteAllText(localJsonPath, root.ToJsonString(writeOptions));

    // Konfiguration neu laden, damit der frisch geschriebene Key sofort
    // im laufenden builder.Configuration verfügbar ist.
    ((IConfigurationRoot)builder.Configuration).Reload();

    Console.WriteLine("================================================================");
    Console.WriteLine(" Kein JWT-Schlüssel gefunden — ein neuer wurde generiert und in");
    Console.WriteLine(" 'appsettings.Local.json' gespeichert.");
    Console.WriteLine(" Diese Datei NICHT ins Git-Repo committen!");
    Console.WriteLine("================================================================");
}
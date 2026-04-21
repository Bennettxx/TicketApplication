using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Diagnostics.Eventing.Reader;
using System.Security.Claims;
using System.Text;
using TicketApplication.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container. Auch Dependency Injection genannt - welche Services stehen später zur Verfügung.

// Hier binden wir die DB an die App, damit wir später in den Controllern darauf zugreifen können
// connectionString holt sich die Verbindungsdaten aus der appsettings.json
// DbContext ist die Klasse, die die DB abbildet und den Zugriff ermöglicht
// Für debugging .LogTo(Console.WriteLine, LogLevel.Information)    Dieses später auskommentieren!
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
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
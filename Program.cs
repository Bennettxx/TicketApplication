using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using TicketApplication.Data;
using TicketApplication.Services;

var builder = WebApplication.CreateBuilder(args);

// cors: frontend darf api aufrufen, auch von anderem port
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// dev: appsettings.Local.json laden, fehlende keys automatisch erzeugen
// prod: security-keys aus umgebungsvariablen (siehe ANLEITUNG_UMGEBUNGSVARIABLEN.txt)
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddJsonFile(
        "appsettings.Local.json",
        optional: true,
        reloadOnChange: true);

    EnsureJwtKeyExists(builder);
    EnsureAttachmentKeyExists(builder);
}

// prod: security-keys müssen gesetzt sein, hier wird nichts generiert
if (!builder.Environment.IsDevelopment())
{
    if (string.IsNullOrWhiteSpace(builder.Configuration["Jwt:Key"]))
        throw new InvalidOperationException(
            "Jwt:Key fehlt. In Produktion Umgebungsvariable 'Jwt__Key' setzen (siehe ANLEITUNG_UMGEBUNGSVARIABLEN.txt).");
    if (string.IsNullOrWhiteSpace(builder.Configuration["Attachments:Key"]))
        throw new InvalidOperationException(
            "Attachments:Key fehlt. In Produktion Umgebungsvariable 'Attachments__Key' setzen (siehe ANLEITUNG_UMGEBUNGSVARIABLEN.txt).");
}

// eigene services: geschützte config, datei-logging, mail-queue + versand
builder.Services.AddSingleton<AppConfigService>();
builder.Services.AddSingleton<LogService>();
builder.Services.AddSingleton<MailQueue>();
builder.Services.AddScoped<MailService>();
builder.Services.AddHostedService<MailDispatcherService>();
builder.Services.AddHostedService<LogCleanupService>();

// db-context: connection-string kommt zur laufzeit aus der app-config
// (ohne config läuft die app im setup-modus, dann wird der context nicht benutzt)
builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
{
    var cfg = sp.GetRequiredService<AppConfigService>();
    var cs = cfg.GetConnectionString();
    if (!string.IsNullOrWhiteSpace(cs))
    {
        options.UseSqlServer(cs);
        if (builder.Environment.IsDevelopment())
            options.LogTo(Console.WriteLine, LogLevel.Information);
    }
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// jwt-bearer auth, token wird gegen key/issuer/audience geprüft
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
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

var appConfig = app.Services.GetRequiredService<AppConfigService>();
var logService = app.Services.GetRequiredService<LogService>();

// unbehandelte fehler in fehler-log schreiben
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        logService.Error(LogBereich.Fehler, $"{context.Request.Method} {context.Request.Path}", ex);
        throw;
    }
});

// api-doku nur im dev-modus
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

// setup-modus: ohne db-config alles auf setup.html umleiten
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
    if (!appConfig.IsConfigured)
    {
        bool erlaubt = path.StartsWith("/api/setup")
            || path == "/setup.html"
            || path.EndsWith(".css") || path.EndsWith(".js")
            || path.EndsWith(".png") || path.EndsWith(".ico");
        if (!erlaubt)
        {
            context.Response.Redirect("/setup.html");
            return;
        }
    }
    else if (path == "/setup.html")
    {
        context.Response.Redirect("/index.html");
        return;
    }
    await next();
});

app.UseDefaultFiles();

// statische dateien; im dev-modus cache aus, damit frontend-änderungen sofort ankommen
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        if (app.Environment.IsDevelopment())
        {
            ctx.Context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            ctx.Context.Response.Headers["Pragma"] = "no-cache";
            ctx.Context.Response.Headers["Expires"] = "0";
        }
    }
});

// cors vor auth, sonst blockt der browser den login
app.UseCors("AllowAll");

app.UseAuthentication();

// dev-hintertür: header "Authorization: Dev-Admin" = admin ohne login
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
            var identity = new ClaimsIdentity(claims, "DevAuth");
            context.User = new ClaimsPrincipal(identity);
        }
        await next();
    });
}

// passwort-zwangswechsel: solange das flag gesetzt ist, sind nur
// login/logout, profil lesen und passwort ändern erlaubt
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
    if (context.User?.Identity?.IsAuthenticated == true && path.StartsWith("/api"))
    {
        bool erlaubt = path.StartsWith("/api/auth")
            || path == "/api/account/me/password"
            || path == "/api/account/me"
            || path == "/api/user/me";
        if (!erlaubt)
        {
            var idStr = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(idStr, out var uid) && uid > 0)
            {
                var db = context.RequestServices.GetRequiredService<ApplicationDbContext>();
                var muss = await db.Users
                    .Where(u => u.Id == uid)
                    .Select(u => u.MustChangePassword)
                    .FirstOrDefaultAsync();
                if (muss)
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsync("Bitte zuerst das Passwort ändern (Mein Profil).");
                    return;
                }
            }
        }
    }
    await next();
});

app.UseAuthorization();

app.MapControllers();

// db anlegen + startdaten, nur wenn eine verbindung konfiguriert ist
if (appConfig.IsConfigured)
{
    try
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        DbInitializer.Initialize(context, app.Environment.IsDevelopment());
        logService.Info(LogBereich.App, "Anwendung gestartet, Datenbank initialisiert.");
    }
    catch (Exception ex)
    {
        logService.Error(LogBereich.App, "Datenbank-Initialisierung beim Start fehlgeschlagen.", ex);
    }
}
else
{
    logService.Info(LogBereich.App, "Anwendung im Setup-Modus gestartet (keine DB-Konfiguration).");
}

app.Run();

// jwt-key sicherstellen: falls keiner konfiguriert ist, zufallskey erzeugen
// und in appsettings.Local.json ablegen (nur dev)
static void EnsureJwtKeyExists(WebApplicationBuilder builder)
{
    var existingKey = builder.Configuration["Jwt:Key"];
    if (!string.IsNullOrWhiteSpace(existingKey))
        return;

    var randomBytes = new byte[32];
    RandomNumberGenerator.Fill(randomBytes);
    var newKey = Convert.ToHexString(randomBytes);

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

    ((IConfigurationRoot)builder.Configuration).Reload();

    Console.WriteLine("================================================================");
    Console.WriteLine(" Kein JWT-Schlüssel gefunden — ein neuer wurde generiert und in");
    Console.WriteLine(" 'appsettings.Local.json' gespeichert.");
    Console.WriteLine(" Diese Datei NICHT ins Git-Repo committen!");
    Console.WriteLine("================================================================");
}

// aes-key für private anhänge sicherstellen, gleiche logik wie beim jwt-key
static void EnsureAttachmentKeyExists(WebApplicationBuilder builder)
{
    var existingKey = builder.Configuration["Attachments:Key"];
    if (!string.IsNullOrWhiteSpace(existingKey))
        return;

    var randomBytes = new byte[32];
    RandomNumberGenerator.Fill(randomBytes);
    var newKey = Convert.ToBase64String(randomBytes);

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

    if (root["Attachments"] is not JsonObject section)
    {
        section = new JsonObject();
        root["Attachments"] = section;
    }
    section["Key"] = newKey;

    var writeOptions = new JsonSerializerOptions { WriteIndented = true };
    File.WriteAllText(localJsonPath, root.ToJsonString(writeOptions));

    ((IConfigurationRoot)builder.Configuration).Reload();

    Console.WriteLine(" Anhang-Verschlüsselungsschlüssel generiert und in 'appsettings.Local.json' gespeichert.");
}

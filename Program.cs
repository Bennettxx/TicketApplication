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
// prod: alles kommt aus umgebungsvariablen (siehe ANLEITUNG_UMGEBUNGSVARIABLEN.txt)
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddJsonFile(
        "appsettings.Local.json",
        optional: true,
        reloadOnChange: true);

    EnsureJwtKeyExists(builder);
    EnsureAttachmentKeyExists(builder);
}

// connection-string früh prüfen, sonst kryptischer ef-fehler später
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "ConnectionStrings:DefaultConnection ist nicht konfiguriert. " +
        "Development: 'appsettings.Local.json' anlegen. " +
        "Production: Umgebungsvariable 'ConnectionStrings__DefaultConnection' setzen " +
        "(siehe ANLEITUNG_UMGEBUNGSVARIABLEN.txt).");
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

// db-context registrieren, sql-logging nur im dev-modus
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(connectionString);
    if (builder.Environment.IsDevelopment())
        options.LogTo(Console.WriteLine, LogLevel.Information);
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

// api-doku nur im dev-modus
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
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

app.UseAuthorization();

app.MapControllers();

// db anlegen + startdaten einspielen
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();
    DbInitializer.Initialize(context);
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

using TicketApplication.Data;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container. Auch Dependency Injection genannt - welche Services stehen später zur Verfügung.

// Hier wird mittels Database.Context eine SQL-DB eingebunden
// Für debugging .LogTo(Console.WriteLine, LogLevel.Information) beigefügt
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString).LogTo(Console.WriteLine, LogLevel.Information));

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi .. früher Swagger
builder.Services.AddOpenApi();

// App-Objekt wird erstellt mit oben implementierten Var etc.
// Ab dem Build() können keine neuen Services hinzugefügt werden, sondern nurnoch der Ablaufplan (Middelware)
var app = builder.Build();

// Configure the HTTP request pipeline. Wer soll die API-Dokumentation sehen können? (hier nur Entwickler)
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// Leitet HTTP Aufrufe als HTTPS weiter
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// Hier kann man ergänzen was vor dem Start geschehen soll (unabhängig der Bedienung der App)

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    // Wir holen uns den 'Dolmetscher' aus dem System
    var context = services.GetRequiredService<ApplicationDbContext>();

    // Wir rufen unsere neue Klasse auf
    DbInitializer.Initialize(context);
}

app.Run();
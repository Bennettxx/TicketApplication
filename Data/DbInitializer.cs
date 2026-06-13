using Microsoft.EntityFrameworkCore;
using TicketApplication.Models;

namespace TicketApplication.Data
{
    // Diese Klasse wird in Program.cs aufgerufen, um die DB zu initialisieren
    // Schritt 1: Prüfen ob DB da ist und Tabellen laut Schema anlegen
    //            Das Schema ergibt sich aus den DbSet-Variablen in ApplicationDbContext.cs
    // Schritt 2: Prüfen ob bestimmte Tabellen leer sind und Default-Daten
    //            anlegen (z.B. Standard-Admin-User).
    public class DbInitializer
    {
        public static void Initialize(ApplicationDbContext context)
        {
            // Legt die DB inkl. aller Tabellen an, falls sie noch nicht existiert.
            // Bei bestehenden DBs passiert nichts — Schema-Änderungen werden NICHT automatisch eingespielt.
            // Wenn das Model erweitert wird, muss die lokale DB einmal gelöscht werden.
            context.Database.EnsureCreated();

            // Hier könnte später Schritt 2 folgen: 
            // Prüfen ob die Tabelle leer ist und ggf. einen Admin-User anlegen
            if (!context.Users.Any())
            {
                // Logik für: "Wenn keine User da sind, erstelle einen Admin"
                var adminUser = new User
                {
                    FirstName = "Admin",
                    SecondName = "User",
                    Email = "admin@user.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password"),
                    Role = UserRole.Admin,
                    IsActivated = true,
                    IsActive = true
                };
                context.Users.Add(adminUser);
                context.SaveChanges();
            }
            // Default Daten für Departments
            if (!context.Departments.Any())
            {
                string[] departmentNames = { "IT Support", "Einkauf", "Verkauf" };

                foreach (var name in departmentNames)
                {
                    context.Departments.Add(new Department { Name = name });
                }
                context.SaveChanges();
            }
        }
    }
}
using TicketApplication.Models;

namespace TicketApplication.Data
{
    // Diese Klasse wird in Program.cs aufgerufen, um die DB zu initialisieren
    // Schritt 1: Prüfen ob DB da ist und Tabellen laut Schema anlegen
    // Das Schema ergibt sich aus den DbSet-Variablen in ApplicationDbContext.cs
    // Schritt 2: Prüfen ob die Tabelle leer ist und zB. einen Admin-User anlegen
    // 
    public class DbInitializer
    {
        public static void Initialize(ApplicationDbContext context)
        {
            // Prüfen ob DB da ist und Tabellen laut Schema anlegen
            context.Database.EnsureCreated();

            // Hier könnte später Schritt 2 folgen: 
            // Prüfen ob die Tabelle leer ist und ggf. einen Admin-User anlegen
            if (!context.Users.Any())
            {
                // Logik für: "Wenn keine User da sind, erstelle einen Admin"
                var adminUser = new User
                {
                    Id = 0,
                    FirstName = "Admin",
                    SecondName = "User",
                    Email = "admin@user.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password"),
                    Role = UserRole.Admin,
                    IsEmailConfirmed = true,
                    IsActive = true
                };
                context.Users.Add(adminUser);
                context.SaveChanges();
            }
        }
    }
}
using TicketApplication.Models;

namespace TicketApplication.Data
{
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
                    Password = "Password",
                    IsAdmin = true
                };
                context.Users.Add(adminUser);
                context.SaveChanges();
            }
        }
    }
}
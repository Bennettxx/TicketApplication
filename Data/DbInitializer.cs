using TicketApplication.Models;

namespace TicketApplication.Data
{
    // legt db + tabellen an und spielt startdaten ein
    // achtung: EnsureCreated migriert nicht — bei model-änderungen lokale db löschen
    public class DbInitializer
    {
        public static void Initialize(ApplicationDbContext context, bool isDevelopment = false)
        {
            context.Database.EnsureCreated();

            // abteilungen zuerst, user brauchen sie
            if (!context.Departments.Any())
            {
                string[] departmentNames = { "IT Support", "Einkauf", "Verkauf" };
                foreach (var name in departmentNames)
                {
                    context.Departments.Add(new Department { Name = name });
                }
                context.SaveChanges();
            }

            // dev-testuser, passwort jeweils "Password"
            //if (isDevelopment && !context.Users.Any())
            //{
            //    int? einkaufDep = context.Departments.Where(d => d.Name == "Einkauf").Select(d => (int?)d.Id).FirstOrDefault();

            //    context.Users.AddRange(
            //        new User
            //        {
            //            FirstName = "A_Vorname",
            //            SecondName = "A_Nachname",
            //            Email = "admin@user.com",
            //            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password"),
            //            Role = UserRole.Admin,
            //            DepartmentId = null,
            //            IsActivated = true,
            //            IsActive = true,
            //            EmailConfirmed = true
            //        },
            //        new User
            //        {
            //            FirstName = "S_Vorname",
            //            SecondName = "S_Nachname",
            //            Email = "support@user.com",
            //            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password"),
            //            Role = UserRole.Support,
            //            DepartmentId = null,
            //            IsActivated = true,
            //            IsActive = true,
            //            EmailConfirmed = true
            //        },
            //        new User
            //        {
            //            FirstName = "U_Vorname",
            //            SecondName = "U_Nachname",
            //            Email = "user@user.com",
            //            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password"),
            //            Role = UserRole.User,
            //            DepartmentId = einkaufDep,
            //            IsActivated = true,
            //            IsActive = true,
            //            EmailConfirmed = true
            //        });
            //    context.SaveChanges();
            //}

            // default-admin (admin@ticket.local / admin) falls kein admin existiert;
            // muss beim ersten login das passwort ändern
            if (!context.Users.Any(u => u.Role == UserRole.Admin && u.IsActive))
            {
                context.Users.Add(new User
                {
                    FirstName = "Standard",
                    SecondName = "Admin",
                    Email = Controllers.SetupController.DefaultAdminEmail,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(Controllers.SetupController.DefaultAdminPassword),
                    Role = UserRole.Admin,
                    DepartmentId = null,
                    IsActivated = true,
                    IsActive = true,
                    EmailConfirmed = true,
                    MustChangePassword = true
                });
                context.SaveChanges();
            }

            // beispiel-wissensartikel
            if (!context.KnowledgeArticles.Any())
            {
                context.KnowledgeArticles.AddRange(
                    new KnowledgeArticle
                    {
                        Title = "Passwort vergessen / zurücksetzen",
                        Keywords = "passwort login anmeldung zugang gesperrt kennwort",
                        Solution = "Über 'Mein Profil' kann das Passwort jederzeit geändert werden. " +
                                   "Bei einem gesperrten Konto wende dich an einen Administrator zur Freischaltung.",
                        IsPublished = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new KnowledgeArticle
                    {
                        Title = "Drucker druckt nicht",
                        Keywords = "drucker drucken papier toner offline warteschlange",
                        Solution = "1) Prüfe, ob der Drucker eingeschaltet und mit dem Netzwerk verbunden ist. " +
                                   "2) Lösche hängende Druckaufträge in der Warteschlange. " +
                                   "3) Starte den Drucker neu. Hilft das nicht, erstelle ein Ticket.",
                        IsPublished = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new KnowledgeArticle
                    {
                        Title = "VPN-Verbindung schlägt fehl",
                        Keywords = "vpn netzwerk verbindung remote homeoffice zugriff",
                        Solution = "Stelle sicher, dass du eine stabile Internetverbindung hast und die " +
                                   "VPN-Zugangsdaten korrekt sind. Bei wiederholtem Fehler den VPN-Client neu " +
                                   "starten. Besteht das Problem weiter, bitte Ticket mit Fehlermeldung erstellen.",
                        IsPublished = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                context.SaveChanges();
            }
        }
    }
}

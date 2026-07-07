using TicketApplication.Models;
using Microsoft.EntityFrameworkCore;

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

            // ZUERST Abteilungen anlegen, damit die Benutzer ihnen direkt
            // zugeordnet werden können.
            if (!context.Departments.Any())
            {
                string[] departmentNames = { "IT Support", "Einkauf", "Verkauf" };
                foreach (var name in departmentNames)
                {
                    context.Departments.Add(new Department { Name = name });
                }
                context.SaveChanges();
            }

            // Standard-Benutzer für die lokale Entwicklung. Alle mit dem
            // Passwort "Password". In Produktion natürlich ändern!
            //   admin@user.com    -> Admin   (alle Rechte, Statistik)
            //   support@user.com  -> Support (Tickets bearbeiten, Kanban)
            //   kunde@user.com    -> User    (eigene Tickets erstellen)
            if (!context.Users.Any())
            {
                // Abteilungs-Id für den Kunden auflösen.
                // Admin/Support bekommen KEINE Abteilung (DepartmentId = null).
                int? einkaufDep = context.Departments.Where(d => d.Name == "Einkauf").Select(d => (int?)d.Id).FirstOrDefault();

                context.Users.AddRange(
                    new User
                    {
                        FirstName = "A_Vorname",
                        SecondName = "A_Nachname",
                        Email = "admin@user.com",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password"),
                        Role = UserRole.Admin,
                        DepartmentId = null,
                        IsActivated = true,
                        IsActive = true
                    },
                    new User
                    {
                        FirstName = "S_Vorname",
                        SecondName = "S_Nachname",
                        Email = "support@user.com",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password"),
                        Role = UserRole.Support,
                        DepartmentId = null,
                        IsActivated = true,
                        IsActive = true
                    },
                    new User
                    {
                        FirstName = "U_Vorname",
                        SecondName = "U_Nachname",
                        Email = "user@user.com",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password"),
                        Role = UserRole.User,
                        DepartmentId = einkaufDep,
                        IsActivated = true,
                        IsActive = true
                    });
                context.SaveChanges();
            }

            // Beispiel-Wissensartikel für die Lösungsvorschläge.
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
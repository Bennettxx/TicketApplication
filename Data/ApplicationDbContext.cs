using TicketApplication.Models;
using Microsoft.EntityFrameworkCore;

namespace TicketApplication.Data
{
    // Diese Klasse bildet die DB ab und ermöglicht den Zugriff auf die Tabellen
    // Sie wird in Program.cs eingebunden und über Dependency Injection in den Controllern verfügbar gemacht
    // Bsp.: Ein DbSet<User> Users bedeutet, dass es eine Tabelle namens "Users" gibt, die Instanzen der Klasse User enthält
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
        {
        }

        public DbSet<User> Users { get; set; }

    }
}

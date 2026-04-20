using TicketApplication.Models;
using Microsoft.EntityFrameworkCore;

namespace TicketApplication.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
        {
        }

        // Tabelle wird Users heißen
        // Zeilen sind Instanzen der klasse User und haben Werte in den Spalten von Users entsprechend der Variablen der User-Klasse

        public DbSet<User> Users { get; set; }

    }
}

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
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<TicketDialogue> TicketDialogue { get; set; }
        public DbSet<TicketTransaction> TicketTransactions { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Subject> Subjects { get; set; }


        // PKs festlegen - sofern es nicht "nur" die ID ist (wird autom. erkannt)
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TicketTransaction>(entity =>
            {
                entity.HasKey(t => new { t.Id, t.TransactionId });

                entity.HasOne<Ticket>()
                      .WithMany()
                      .HasForeignKey(t => t.Id);
            });
            modelBuilder.Entity<TicketDialogue>(entity =>
            {
                entity.HasKey(t => new { t.Id, t.TicketDialogueId });

                entity.HasOne<Ticket>()
                      .WithMany()
                      .HasForeignKey(t => t.Id);
            });
        }

    }
}

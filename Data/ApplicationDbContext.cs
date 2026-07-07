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
        public DbSet<TicketTimeEntry> TicketTimeEntries { get; set; }
        public DbSet<TicketAttachments> TicketAttachments { get; set; }
        public DbSet<KnowledgeArticle> KnowledgeArticles { get; set; }
        public DbSet<Problem> Problems { get; set; }
        public DbSet<TicketRead> TicketReads { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Subject> Subjects { get; set; }


        // PKs festlegen - sofern es nicht "nur" die ID ist (wird autom. erkannt)
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TicketTransaction>(entity =>
            {
                entity.HasKey(t => new { t.TicketId, t.TransactionId });

                entity.HasOne<Ticket>()
                      .WithMany()
                      .HasForeignKey(t => t.TicketId);
            });

            // TicketDialogue: eigener Auto-Increment-PK (Id), FK auf das Ticket
            // über TicketId. Index auf TicketId, weil wir immer nach allen
            // Nachrichten eines Tickets filtern.
            modelBuilder.Entity<TicketDialogue>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.HasOne<Ticket>()
                      .WithMany()
                      .HasForeignKey(t => t.TicketId);
                entity.HasIndex(t => t.TicketId);
            });

            // TicketTimeEntry: eigener Auto-Increment-PK (Id), FK auf das Ticket.
            modelBuilder.Entity<TicketTimeEntry>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.HasOne<Ticket>()
                      .WithMany()
                      .HasForeignKey(t => t.TicketId);
                entity.HasIndex(t => t.TicketId);
            });

            // TicketAttachments: eigener PK, FK auf das Ticket, Index auf TicketId.
            modelBuilder.Entity<TicketAttachments>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.HasOne<Ticket>()
                      .WithMany()
                      .HasForeignKey(t => t.TicketId);
                entity.HasIndex(t => t.TicketId);
            });

            // KnowledgeArticle: eigener PK.
            modelBuilder.Entity<KnowledgeArticle>(entity =>
            {
                entity.HasKey(t => t.Id);
            });

            // TicketRead: eigener PK, ein Eintrag pro (Ticket, User).
            modelBuilder.Entity<TicketRead>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.HasIndex(t => new { t.TicketId, t.UserId }).IsUnique();
            });
        }

    }
}

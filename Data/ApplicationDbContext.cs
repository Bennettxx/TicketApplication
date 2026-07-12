using TicketApplication.Models;
using Microsoft.EntityFrameworkCore;

namespace TicketApplication.Data
{
    // db-abbild, jedes DbSet = eine tabelle
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
        public DbSet<AppSetting> AppSettings { get; set; }

        // keys, fks und indizes, die nicht der konvention entsprechen
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // zusammengesetzter pk: ticket + laufende nummer
            modelBuilder.Entity<TicketTransaction>(entity =>
            {
                entity.HasKey(t => new { t.TicketId, t.TransactionId });

                entity.HasOne<Ticket>()
                      .WithMany()
                      .HasForeignKey(t => t.TicketId);
            });

            modelBuilder.Entity<TicketDialogue>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.HasOne<Ticket>()
                      .WithMany()
                      .HasForeignKey(t => t.TicketId);
                entity.HasIndex(t => t.TicketId);
            });

            modelBuilder.Entity<TicketTimeEntry>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.HasOne<Ticket>()
                      .WithMany()
                      .HasForeignKey(t => t.TicketId);
                entity.HasIndex(t => t.TicketId);
            });

            modelBuilder.Entity<TicketAttachments>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.HasOne<Ticket>()
                      .WithMany()
                      .HasForeignKey(t => t.TicketId);
                entity.HasIndex(t => t.TicketId);
            });

            modelBuilder.Entity<KnowledgeArticle>(entity =>
            {
                entity.HasKey(t => t.Id);
            });

            // ein leseeintrag pro (ticket, user)
            modelBuilder.Entity<TicketRead>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.HasIndex(t => new { t.TicketId, t.UserId }).IsUnique();
            });

            // einstellungen: key ist der pk
            modelBuilder.Entity<AppSetting>(entity =>
            {
                entity.HasKey(t => t.Key);
            });
        }

    }
}

using Microsoft.EntityFrameworkCore;
using OJTMISApi.Models;

namespace OJTMISApi.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<School> Schools { get; set; } = null!;
        public DbSet<Intern> Interns { get; set; } = null!;
        public DbSet<OJTProgram> Programs { get; set; } = null!;
        public DbSet<Signatory> Signatories { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<School>(entity =>
            {
                entity.HasKey(s => s.Id);

                entity.Property(s => s.Name)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.Property(s => s.Address)
                      .IsRequired()
                      .HasMaxLength(500);

                // Timestamps are assigned by the model/controller (DateTime.Now), not by the
                // database, so no provider-specific default value SQL is used. This keeps the
                // context portable across SQL Server and SQLite.
                entity.Property(s => s.CreatedAt)
                      .ValueGeneratedNever();

                entity.Property(s => s.UpdatedAt)
                      .ValueGeneratedNever();

                entity.HasIndex(s => s.Name)
                      .IsUnique()
                      .HasDatabaseName("IX_Schools_Name");
            });
        }
    }
}

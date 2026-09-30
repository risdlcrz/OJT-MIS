using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OJTMISApi.Models;

namespace OJTMISApi.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<School> Schools { get; set; } = null!;
        public DbSet<Intern> Interns { get; set; } = null!;
        public DbSet<OJTProgram> Programs { get; set; } = null!;
        public DbSet<Signatory> Signatories { get; set; } = null!;
        public DbSet<InternRequest> InternRequests { get; set; } = null!;
        public DbSet<Applicant> Applicants { get; set; } = null!;

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

            modelBuilder.Entity<InternRequest>(entity =>
            {
                entity.HasKey(r => r.Id);

                entity.Property(r => r.OfficeCode).IsRequired().HasMaxLength(50);
                entity.Property(r => r.OfficeName).IsRequired().HasMaxLength(200);
                entity.Property(r => r.Skills).HasMaxLength(500);
                entity.Property(r => r.Description).HasMaxLength(1000);
                entity.Property(r => r.Status).IsRequired().HasMaxLength(20).HasDefaultValue("Open");

                entity.Property(r => r.CreatedAt).ValueGeneratedNever();
                entity.Property(r => r.UpdatedAt).ValueGeneratedNever();
            });

            modelBuilder.Entity<Intern>(entity =>
            {
                entity.Property(i => i.Status).IsRequired().HasMaxLength(20).HasDefaultValue("Applicant");

                // Kapag may request na "Hired" ang intern, dapat valid ang RequestId.
                entity.HasOne<InternRequest>()
                      .WithMany()
                      .HasForeignKey(i => i.RequestId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Applicant>(entity =>
            {
                entity.HasKey(a => a.Id);

                entity.Property(a => a.ApplicantNo).IsRequired().HasMaxLength(20);
                entity.Property(a => a.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(a => a.MiddleName).HasMaxLength(100);
                entity.Property(a => a.LastName).IsRequired().HasMaxLength(100);
                entity.Property(a => a.Suffix).HasMaxLength(10);
                entity.Property(a => a.Email).HasMaxLength(200);
                entity.Property(a => a.ContactNumber).HasMaxLength(50);
                entity.Property(a => a.HouseAddress).HasMaxLength(300);
                entity.Property(a => a.SchoolName).HasMaxLength(200);
                entity.Property(a => a.EducationLevel).HasMaxLength(100);
                entity.Property(a => a.Program).HasMaxLength(200);
                entity.Property(a => a.CoordName).HasMaxLength(200);
                entity.Property(a => a.GuardianName).HasMaxLength(200);
                entity.Property(a => a.GuardianContact).HasMaxLength(50);
                entity.Property(a => a.RequirementsCsv).HasMaxLength(50).HasDefaultValue("0,0,0,0,0,0");
                entity.Property(a => a.ApplicantOffice).HasMaxLength(200);
                entity.Property(a => a.Remarks).HasMaxLength(500);
                entity.Property(a => a.RequestNoLabel).HasMaxLength(200);
                entity.Property(a => a.OrientationTime).HasMaxLength(20);
                entity.Property(a => a.OrientationOffice).HasMaxLength(200);

                entity.HasIndex(a => a.ApplicantNo).IsUnique().HasDatabaseName("IX_Applicants_ApplicantNo");

                entity.Property(a => a.CreatedAt).ValueGeneratedNever();
                entity.Property(a => a.UpdatedAt).ValueGeneratedNever();
            });
        }
    }
}

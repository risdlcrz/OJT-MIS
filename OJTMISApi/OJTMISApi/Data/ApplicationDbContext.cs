using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OJTMISApi.Models;

namespace OJTMISApi.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext(options)
    {
        public DbSet<Applicant> Applicants { get; set; }
        public DbSet<OJTProgram> Programs { get; set; }
        public DbSet<School> Schools { get; set; }
        public DbSet<Signatory> Signatories { get; set; }
        public DbSet<Intern> Interns { get; set; }
        public DbSet<InternRequest> InternRequests { get; set; }
    }
}

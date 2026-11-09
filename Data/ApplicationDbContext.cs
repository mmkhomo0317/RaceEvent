using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RaceEvent.Data;
using RaceEvent.Models;
using System.Reflection.Emit;

namespace RaceEvent.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }
        public DbSet<Event> Events => Set<Event>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Enrolment> Enrolments => Set<Enrolment>();
        public DbSet<Result> Results => Set<Result>();
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Optional: store enum as string
            builder.Entity<ApplicationUser>()
                .Property(u => u.Gender)
                .HasConversion<string>()
                .HasMaxLength(20);
        }
    }

}

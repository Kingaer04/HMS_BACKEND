using HMS.Entities.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HMS.Repository.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Hospital> Hospitals { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Hospital>(e =>
            {
                e.HasKey(h => h.Id);
                e.HasIndex(h => h.HospitalUID).IsUnique();
                e.Property(h => h.HospitalUID).IsRequired().HasMaxLength(50);
                e.Property(h => h.Name).IsRequired().HasMaxLength(200);
                e.Property(h => h.Country).HasDefaultValue("Nigeria");
            });

            builder.Entity<ApplicationUser>(e =>
            {
                e.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
                e.Property(u => u.LastName).IsRequired().HasMaxLength(100);
                e.HasOne(u => u.Hospital)
                 .WithMany(h => h.Staff)
                 .HasForeignKey(u => u.HospitalId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // Seed roles
            builder.Entity<IdentityRole>().HasData(
                new IdentityRole { Id = "1", Name = "HospitalAdmin",  NormalizedName = "HOSPITALADMIN" },
                new IdentityRole { Id = "2", Name = "Doctor",         NormalizedName = "DOCTOR" },
                new IdentityRole { Id = "3", Name = "Receptionist",   NormalizedName = "RECEPTIONIST" }
            );
        }
    }
}

using CollegeEventManagement.Core.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CollegeEventManagement.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Event> Events => Set<Event>();
    public DbSet<EventRegistration> EventRegistrations => Set<EventRegistration>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ApplicationUser configuration
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(u => u.FullName).HasMaxLength(100).IsRequired();
            entity.Property(u => u.StudentId).HasMaxLength(30);
            entity.Property(u => u.Department).HasMaxLength(100);
            entity.Property(u => u.AcademicYear).HasMaxLength(30);

            // Index on StudentId
            entity.HasIndex(u => u.StudentId);
        });

        // Event configuration
        builder.Entity<Event>(entity =>
        {
            entity.HasKey(e => e.EventId);
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Category).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Venue).HasMaxLength(200).IsRequired();
            entity.Property(e => e.OrganizerName).HasMaxLength(150).IsRequired();
            entity.Property(e => e.ImageUrl).HasMaxLength(500);

            entity.HasIndex(e => e.EventDate);
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.IsPublished);
        });

        // EventRegistration configuration
        builder.Entity<EventRegistration>(entity =>
        {
            entity.HasKey(r => r.RegistrationId);
            entity.Property(r => r.Status).HasMaxLength(30).IsRequired();
            entity.Property(r => r.AttendanceStatus).HasMaxLength(30).IsRequired();

            // Crucial: Unique constraint on (UserId, EventId) to prevent duplicate registrations
            entity.HasIndex(r => new { r.UserId, r.EventId }).IsUnique();

            // Relationships
            entity.HasOne(r => r.User)
                  .WithMany(u => u.Registrations)
                  .HasForeignKey(r => r.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(r => r.Event)
                  .WithMany(e => e.Registrations)
                  .HasForeignKey(r => r.EventId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

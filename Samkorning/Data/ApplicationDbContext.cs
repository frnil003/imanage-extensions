using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Samkorning.Models;

namespace Samkorning.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<CompetitionEvent> Events { get; set; }
    public DbSet<RideBooking> Bookings { get; set; }
    public DbSet<Activity> Activities { get; set; }
    public DbSet<ActivitySignup> ActivitySignups { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<RideBooking>()
            .HasOne(b => b.Event)
            .WithMany(e => e.Bookings)
            .HasForeignKey(b => b.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<RideBooking>()
            .HasOne(b => b.User)
            .WithMany(u => u.Bookings)
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<RideBooking>()
            .HasOne(b => b.AssignedToDriverBooking)
            .WithMany(d => d.AssignedPassengers)
            .HasForeignKey(b => b.AssignedToDriverBookingId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Activity>()
            .HasOne(a => a.Event)
            .WithMany(e => e.Activities)
            .HasForeignKey(a => a.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ActivitySignup>()
            .HasOne(s => s.Activity)
            .WithMany(a => a.Signups)
            .HasForeignKey(s => s.ActivityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ActivitySignup>()
            .HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ActivitySignup>()
            .HasIndex(s => new { s.ActivityId, s.UserId })
            .IsUnique();
    }
}

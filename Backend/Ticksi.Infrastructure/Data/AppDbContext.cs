using System;
using Ticksi.Domain.Entities;
using Ticksi.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Ticksi.Infrastructure.Data;

public class AppDbContext(DbContextOptions options) : DbContext(options), IAppDbContext
{
    public DbSet<AppUser> AppUsers { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<OrganizerCompany> OrganizerCompanies { get; set; }
    public DbSet<EventType> EventTypes { get; set; }

    public DbSet<Event> Events { get; set; }

    public DbSet<EventCategory> EventCategories { get; set; }

    public DbSet<Cart> Carts { get; set; }
    public DbSet<CartItem> CartItems { get; set; }

    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<Ticket> Tickets { get; set; }
    public DbSet<Location> Locations { get; set; }
    public DbSet<Seat> Seats { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<Refund> Refunds { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<PromoCode> PromoCodes { get; set; }
    public DbSet<Favorite> Favorites { get; set; }

protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    // Goes through each class
    foreach (var entityType in modelBuilder.Model.GetEntityTypes())
    {
        // Checks if class inherited BaseEntity.cs
        if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
        {
            // Adds UNIQUE index on PublicId
            modelBuilder.Entity(entityType.ClrType)
                .HasIndex(nameof(BaseEntity.PublicId))
                .IsUnique();
        }
    }

    // AppUser Configuration
    modelBuilder.Entity<AppUser>(entity =>
    {
        entity.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(256);

        entity.Property(u => u.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        entity.Property(u => u.LastName)
            .IsRequired()
            .HasMaxLength(100);

        entity.Property(u => u.Phone)
            .HasMaxLength(20);

        entity.Property(u => u.PasswordHash)
            .IsRequired();

        entity.Property(u => u.Status)
            .IsRequired()
            .HasMaxLength(50);

        entity.HasIndex(u => u.Email)
            .IsUnique();
    });

    // Role Configuration
    modelBuilder.Entity<Role>(entity =>
    {
        entity.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(50);

        entity.HasIndex(r => r.Name)
            .IsUnique();
    });

    // Relationships
    modelBuilder.Entity<Order>()
    .HasOne(o => o.AppUser)
    .WithMany(u => u.Orders)
    .HasForeignKey(o => o.AppUserId)
    .OnDelete(DeleteBehavior.Restrict);

modelBuilder.Entity<Review>()
    .HasOne(r => r.AppUser)
    .WithMany(u => u.Reviews)
    .HasForeignKey(r => r.AppUserId)
    .OnDelete(DeleteBehavior.Restrict);

modelBuilder.Entity<Ticket>()
    .HasOne(t => t.AppUser)
    .WithMany(u => u.Tickets)
    .HasForeignKey(t => t.AppUserId)
    .OnDelete(DeleteBehavior.Restrict);

modelBuilder.Entity<Transaction>()
    .HasOne(tr => tr.AppUser)
    .WithMany(u => u.Transactions)
    .HasForeignKey(tr => tr.AppUserId)
    .OnDelete(DeleteBehavior.Restrict);

modelBuilder.Entity<CartItem>()
    .HasOne(ci => ci.Ticket)
    .WithMany(t => t.CartItems)
    .HasForeignKey(ci => ci.TicketId)
    .OnDelete(DeleteBehavior.Restrict);

modelBuilder.Entity<OrderItem>()
    .HasOne(oi => oi.Ticket)
    .WithMany(t => t.OrderItems)
    .HasForeignKey(oi => oi.TicketId)
    .OnDelete(DeleteBehavior.Restrict);

// Favorite Configuration - Unique constraint on (AppUserId, EventId)
modelBuilder.Entity<Favorite>()
    .HasIndex(f => new { f.AppUserId, f.EventId })
    .IsUnique();

modelBuilder.Entity<Favorite>()
    .HasOne(f => f.AppUser)
    .WithMany()
    .HasForeignKey(f => f.AppUserId)
    .OnDelete(DeleteBehavior.Restrict);

modelBuilder.Entity<Favorite>()
    .HasOne(f => f.Event)
    .WithMany()
    .HasForeignKey(f => f.EventId)
    .OnDelete(DeleteBehavior.Restrict);


  }
}

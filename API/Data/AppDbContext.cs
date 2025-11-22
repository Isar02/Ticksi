using System;
using API.Entities;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class AppDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
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
    modelBuilder.Entity<Order>()
    .HasOne(o => o.User)
    .WithMany(u => u.Orders)
    .HasForeignKey(o => o.UserId)
    .OnDelete(DeleteBehavior.Restrict);

modelBuilder.Entity<Review>()
    .HasOne(r => r.User)
    .WithMany(u => u.Reviews)
    .HasForeignKey(r => r.UserId)
    .OnDelete(DeleteBehavior.Restrict);

modelBuilder.Entity<Ticket>()
    .HasOne(t => t.User)
    .WithMany(u => u.Tickets)
    .HasForeignKey(t => t.UserId)
    .OnDelete(DeleteBehavior.Restrict);

modelBuilder.Entity<Transaction>()
    .HasOne(tr => tr.User)
    .WithMany(u => u.Transactions)
    .HasForeignKey(tr => tr.UserId)
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


  }
}

using System;
using API.Entities;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class AppDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<AppUser> Users { get; set; }

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
  }
}

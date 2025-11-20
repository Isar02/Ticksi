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

    // Prolazi kroz sve klase
    foreach (var entityType in modelBuilder.Model.GetEntityTypes())
    {
        // Provjerava da li klasa nasljeduje BaseEntity
        if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
        {
            // Dodaje UNIQUE index na PublicId
            modelBuilder.Entity(entityType.ClrType)
                .HasIndex(nameof(BaseEntity.PublicId))
                .IsUnique();
        }
    }
  }
}

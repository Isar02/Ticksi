using Microsoft.EntityFrameworkCore;
using Ticksi.Domain.Entities;

namespace Ticksi.Infrastructure.Data.Seeders;

public static class StaticDataSeeder
{
    public static void Seed(ModelBuilder modelBuilder)
    {
        SeedRoles(modelBuilder);
        SeedEventTypes(modelBuilder);
    }

    private static void SeedRoles(ModelBuilder modelBuilder) =>
        modelBuilder.Entity<Role>().HasData(
            new Role { Id = 1, PublicId = Guid.Parse("8d0c3f52-6b1e-4f4a-9c35-1f2e7a9b0c01"), Name = Role.Names.Admin },
            new Role { Id = 2, PublicId = Guid.Parse("8d0c3f52-6b1e-4f4a-9c35-1f2e7a9b0c02"), Name = Role.Names.User },
            new Role { Id = 3, PublicId = Guid.Parse("8d0c3f52-6b1e-4f4a-9c35-1f2e7a9b0c03"), Name = Role.Names.Organizer });

    private static void SeedEventTypes(ModelBuilder modelBuilder) =>
        modelBuilder.Entity<EventType>().HasData(
            EventType(1, "Concert", "A live music performance."),
            EventType(2, "Sport", "A match, race or other sports competition."),
            EventType(3, "Theatre", "A play, opera, ballet or stand-up performance."),
            EventType(4, "Workshop", "A hands-on session where visitors learn by doing."),
            EventType(5, "Conference", "Talks and panels on a professional topic."),
            EventType(6, "Festival", "A multi-day programme with several performers."),
            EventType(7, "Exhibition", "An art or design show open to visitors."),
            EventType(8, "Other", "Any event that fits none of the other types."));

    private static EventType EventType(int id, string name, string description) => new()
    {
        Id = id,
        PublicId = Guid.Parse($"4b7e9a10-2c5d-4e8f-a1b3-0d6c8e2f4a{id:D2}"),
        Name = name,
        Description = description
    };
}

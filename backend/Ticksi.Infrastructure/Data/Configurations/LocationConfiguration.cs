using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ticksi.Domain.Entities;

namespace Ticksi.Infrastructure.Data.Configurations;

public class LocationConfiguration : BaseEntityConfiguration<Location>
{
    public override void Configure(EntityTypeBuilder<Location> builder)
    {
        base.Configure(builder);

        builder.ToTable(table => table.HasCheckConstraint(
            "CK_Locations_Capacity", "[Capacity] >= 0"));

        builder.Property(l => l.Name)
            .IsRequired()
            .HasMaxLength(Location.Constraints.NameMaxLength);

        builder.Property(l => l.City)
            .IsRequired()
            .HasMaxLength(Location.Constraints.CityMaxLength);

        builder.Property(l => l.Address)
            .IsRequired()
            .HasMaxLength(Location.Constraints.AddressMaxLength);
    }
}

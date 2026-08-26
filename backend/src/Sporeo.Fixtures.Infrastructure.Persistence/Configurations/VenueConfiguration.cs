using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NetTopologySuite.Geometries;
using Sporeo.Fixtures.Domain.Venues;
using Sporeo.Fixtures.Domain.Venues.ValueObjects;

namespace Sporeo.Fixtures.Infrastructure.Persistence.Configurations;

internal class VenueConfiguration : IEntityTypeConfiguration<Venue>
{
    public void Configure(EntityTypeBuilder<Venue> builder)
    {
        builder.ToTable("venues");
        
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(
                id => id.Value,
                value => VenueId.FromValue(value))
            .ValueGeneratedNever();

        builder.Property(x => x.Name)
           .HasMaxLength(200)
           .IsRequired();

        builder.OwnsOne(x => x.Address, address =>
        {
            address.Property(a => a.Street).HasColumnName("Street").HasMaxLength(200);
            address.Property(a => a.City).HasColumnName("City").HasMaxLength(100);
            address.Property(a => a.Country).HasColumnName("Country").HasMaxLength(100);
        });

        builder.OwnsOne(x => x.Coordinates, coordinates =>
        {
            coordinates.Property(c => c.Latitude)
                .HasColumnName("Latitude")
                .HasColumnType("decimal(9,6)");

            coordinates.Property(c => c.Longitude)
                .HasColumnName("Longitude")
                .HasColumnType("decimal(9,6)");
        });

        builder.Property<Point>("Location")
            .HasColumnType("geography")
            .HasComputedColumnSql(
                "CASE WHEN [Latitude] IS NOT NULL AND [Longitude] IS NOT NULL THEN geography::Point([Latitude], [Longitude], 4326)",
                stored: true);

        builder.HasIndex("Location")
               .HasDatabaseName("IX_Venues_Location");

        builder.Property(x => x.ExternalProviderName)
            .HasMaxLength(100);

        builder.Property(x => x.ExternalProviderId)
            .HasMaxLength(100);

        builder.Property(x => x.IsManuallyEdited)
            .IsRequired();

        builder.Property(x => x.CreatedOn).IsRequired();
        builder.Property(x => x.ModifiedOn);
        builder.Property(x => x.IsDeleted).IsRequired();
        builder.Property(x => x.DeletedOn);
    }
}

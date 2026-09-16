using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NetTopologySuite.Geometries;
using Sporeo.Fixtures.Domain.Venues;
using Sporeo.Fixtures.Domain.Venues.ValueObjects;

namespace Sporeo.Fixtures.Infrastructure.Persistence.Data.Configurations;

/// <summary>
/// EF Core mapping for <see cref="Venue"/>, including owned address/coordinates and the indexed geography location.
/// </summary>
internal class VenueConfiguration : IEntityTypeConfiguration<Venue>
{
    /// <inheritdoc />
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
            address.Property(a => a.Street)
                .HasColumnName("Street")
                .HasMaxLength(200)
                .IsRequired(false);

            address.Property(a => a.City)
                .HasColumnName("City")
                .HasMaxLength(100)
                .IsRequired();

            address.Property(a => a.Country)
                .HasColumnName("Country")
                .HasMaxLength(100)
                .IsRequired();
        });
        builder.Navigation(x => x.Address).IsRequired(false);

        builder.OwnsOne(x => x.Coordinates, coordinates =>
        {
            coordinates.Property(c => c.Latitude)
                .HasColumnName("Latitude")
                .HasColumnType("decimal(9,6)")
                .HasConversion(
                    value => (decimal)value,
                    value => (double)value)
                .IsRequired();

            coordinates.Property(c => c.Longitude)
                .HasColumnName("Longitude")
                .HasColumnType("decimal(9,6)")
                .HasConversion(
                    value => (decimal)value,
                    value => (double)value)
                .IsRequired();
        });
        builder.Navigation(x => x.Coordinates).IsRequired(false);

        // Regular geography column maintained by VenueLocationInterceptor.
        // Spatial index is created manually in the InitialCreate migration.
        builder.Property<Point>("Location")
            .HasColumnType("geography");

        builder.Property(x => x.ExternalProviderName)
            .HasMaxLength(100);

        builder.Property(x => x.ExternalProviderId)
            .HasMaxLength(100);

        builder.HasIndex(x => new { x.ExternalProviderName, x.ExternalProviderId })
            .IsUnique()
            .HasFilter("[ExternalProviderName] IS NOT NULL AND [ExternalProviderId] IS NOT NULL AND [IsDeleted] = 0")
            .HasDatabaseName("IX_venues_ExternalProvider");

        builder.Property(x => x.IsManuallyEdited)
            .IsRequired();

        builder.Property(x => x.CreatedOn).IsRequired();
        builder.Property(x => x.ModifiedOn);
        builder.Property(x => x.IsDeleted).IsRequired();
        builder.Property(x => x.DeletedOn);
    }
}

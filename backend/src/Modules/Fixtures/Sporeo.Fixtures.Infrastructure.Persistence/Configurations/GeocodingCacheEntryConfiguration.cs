using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sporeo.Fixtures.Infrastructure.Persistence.Geocoding;

namespace Sporeo.Fixtures.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for <see cref="GeocodingCacheEntry"/>.
/// </summary>
internal sealed class GeocodingCacheEntryConfiguration : IEntityTypeConfiguration<GeocodingCacheEntry>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<GeocodingCacheEntry> builder)
    {
        builder.ToTable("geocoding-cache-entries");

        builder.HasKey(entry => entry.NormalizedAddress);

        builder.Property(entry => entry.NormalizedAddress)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(entry => entry.IsFound)
            .IsRequired();

        builder.Property(entry => entry.Latitude)
            .HasColumnType("decimal(9,6)")
            .HasConversion(
                value => value.HasValue ? (decimal?)value.Value : null,
                value => value.HasValue ? (double?)value.Value : null);

        builder.Property(entry => entry.Longitude)
            .HasColumnType("decimal(9,6)")
            .HasConversion(
                value => value.HasValue ? (decimal?)value.Value : null,
                value => value.HasValue ? (double?)value.Value : null);

        builder.Property(entry => entry.Street)
            .HasMaxLength(200);

        builder.Property(entry => entry.City)
            .HasMaxLength(100);

        builder.Property(entry => entry.Country)
            .HasMaxLength(100);

        builder.Property(entry => entry.CachedAtUtc)
            .IsRequired();

        builder.Property(entry => entry.ExpiresAtUtc)
            .IsRequired();

        builder.HasIndex(entry => entry.ExpiresAtUtc)
            .HasDatabaseName("IX_GeocodingCacheEntries_ExpiresAtUtc");
    }
}

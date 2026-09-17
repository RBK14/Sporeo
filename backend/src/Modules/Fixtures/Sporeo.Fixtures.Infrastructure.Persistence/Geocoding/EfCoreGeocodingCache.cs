using Microsoft.EntityFrameworkCore;
using Sporeo.Fixtures.Application.Venues.Abstractions.Geocoding;
using Sporeo.Fixtures.Domain.Venues.ValueObjects;
using Sporeo.Fixtures.Infrastructure.Persistence.Context;

namespace Sporeo.Fixtures.Infrastructure.Persistence.Geocoding;

/// <summary>
/// EF Core implementation of <see cref="IGeocodingCache"/> backed by <see cref="FixturesDbContext"/>.
/// </summary>
internal sealed class EfCoreGeocodingCache(FixturesDbContext dbContext) : IGeocodingCache
{
    /// <inheritdoc />
    public async Task<GeocodingCacheLookup?> GetAsync(
        string normalizedAddress,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedAddress);

        var now = DateTimeOffset.UtcNow;
        var entry = await dbContext.GeocodingCacheEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(
                candidate => candidate.NormalizedAddress == normalizedAddress
                             && candidate.ExpiresAtUtc > now,
                cancellationToken);

        if (entry is null)
            return null;

        if (!entry.IsFound)
            return new GeocodingCacheLookup(IsFound: false, Location: null);

        if (entry.Latitude is null || entry.Longitude is null)
            return null;

        var coordinatesResult = Coordinates.Create(entry.Latitude.Value, entry.Longitude.Value);
        if (coordinatesResult.IsFailure)
            return null;

        Address? address = null;
        if (!string.IsNullOrWhiteSpace(entry.Country))
        {
            var addressResult = Address.Create(entry.Street, entry.City, entry.Country);
            if (addressResult.IsSuccess)
                address = addressResult.Value;
        }

        return new GeocodingCacheLookup(
            IsFound: true,
            Location: new GeocodedLocation(coordinatesResult.Value, address));
    }

    /// <inheritdoc />
    public async Task SetFoundAsync(
        string normalizedAddress,
        GeocodedLocation location,
        DateTimeOffset expiresAtUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedAddress);
        ArgumentNullException.ThrowIfNull(location);

        var entry = await GetOrCreateTrackedEntryAsync(normalizedAddress, cancellationToken);
        entry.IsFound = true;
        entry.Latitude = location.Coordinates.Latitude;
        entry.Longitude = location.Coordinates.Longitude;
        entry.Street = location.Address?.Street;
        entry.City = location.Address?.City;
        entry.Country = location.Address?.Country;
        entry.CachedAtUtc = DateTimeOffset.UtcNow;
        entry.ExpiresAtUtc = expiresAtUtc;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task SetMissAsync(
        string normalizedAddress,
        DateTimeOffset expiresAtUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedAddress);

        var entry = await GetOrCreateTrackedEntryAsync(normalizedAddress, cancellationToken);
        entry.IsFound = false;
        entry.Latitude = null;
        entry.Longitude = null;
        entry.Street = null;
        entry.City = null;
        entry.Country = null;
        entry.CachedAtUtc = DateTimeOffset.UtcNow;
        entry.ExpiresAtUtc = expiresAtUtc;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<GeocodingCacheEntry> GetOrCreateTrackedEntryAsync(
        string normalizedAddress,
        CancellationToken cancellationToken)
    {
        var entry = await dbContext.GeocodingCacheEntries
            .FirstOrDefaultAsync(
                candidate => candidate.NormalizedAddress == normalizedAddress,
                cancellationToken);

        if (entry is not null)
            return entry;

        entry = new GeocodingCacheEntry { NormalizedAddress = normalizedAddress };
        dbContext.GeocodingCacheEntries.Add(entry);
        return entry;
    }
}

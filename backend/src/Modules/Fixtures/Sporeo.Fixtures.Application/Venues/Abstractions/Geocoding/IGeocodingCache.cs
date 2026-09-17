namespace Sporeo.Fixtures.Application.Venues.Abstractions.Geocoding;

/// <summary>
/// Durable geocoding cache port. Implementations may use EF Core, Redis, or any other store.
/// </summary>
public interface IGeocodingCache
{
    /// <summary>
    /// Gets a non-expired cache entry for the specified normalized address.
    /// </summary>
    /// <param name="normalizedAddress">The normalized search address used as the cache key.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The cached lookup, or <see langword="null"/> when missing or expired.</returns>
    Task<GeocodingCacheLookup?> GetAsync(
        string normalizedAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores a successful geocoding result.
    /// </summary>
    /// <param name="normalizedAddress">The normalized search address used as the cache key.</param>
    /// <param name="location">The geocoded location to cache.</param>
    /// <param name="expiresAtUtc">UTC expiry timestamp for the cache entry.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task SetFoundAsync(
        string normalizedAddress,
        GeocodedLocation location,
        DateTimeOffset expiresAtUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores a negative geocoding result so the same missing address is not re-queried.
    /// </summary>
    /// <param name="normalizedAddress">The normalized search address used as the cache key.</param>
    /// <param name="expiresAtUtc">UTC expiry timestamp for the negative cache entry.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task SetMissAsync(
        string normalizedAddress,
        DateTimeOffset expiresAtUtc,
        CancellationToken cancellationToken = default);
}

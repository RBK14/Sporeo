namespace Sporeo.Fixtures.Infrastructure.Persistence.Geocoding;

/// <summary>
/// Durable geocoding cache row supporting both positive and negative lookups.
/// </summary>
public sealed class GeocodingCacheEntry
{
    /// <summary>
    /// Gets the normalized address used as the primary cache key.
    /// </summary>
    public string NormalizedAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether a location was found for the address.
    /// </summary>
    public bool IsFound { get; set; }

    /// <summary>
    /// Gets the cached latitude when <see cref="IsFound"/> is <see langword="true"/>.
    /// </summary>
    public double? Latitude { get; set; }

    /// <summary>
    /// Gets the cached longitude when <see cref="IsFound"/> is <see langword="true"/>.
    /// </summary>
    public double? Longitude { get; set; }

    /// <summary>
    /// Gets the cached street when available.
    /// </summary>
    public string? Street { get; set; }

    /// <summary>
    /// Gets the cached city when available.
    /// </summary>
    public string? City { get; set; }

    /// <summary>
    /// Gets the cached country when available.
    /// </summary>
    public string? Country { get; set; }

    /// <summary>
    /// Gets the UTC timestamp when the entry was written.
    /// </summary>
    public DateTimeOffset CachedAtUtc { get; set; }

    /// <summary>
    /// Gets the UTC timestamp when the entry expires.
    /// </summary>
    public DateTimeOffset ExpiresAtUtc { get; set; }
}

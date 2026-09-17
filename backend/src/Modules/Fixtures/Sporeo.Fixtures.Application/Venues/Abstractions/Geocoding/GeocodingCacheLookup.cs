namespace Sporeo.Fixtures.Application.Venues.Abstractions.Geocoding;

/// <summary>
/// Result of a geocoding cache lookup.
/// </summary>
/// <param name="IsFound">
/// <see langword="true"/> when a location was previously resolved;
/// <see langword="false"/> for a cached miss (address not found).
/// </param>
/// <param name="Location">The cached location when <paramref name="IsFound"/> is <see langword="true"/>; otherwise <see langword="null"/>.</param>
public sealed record GeocodingCacheLookup(
    bool IsFound,
    GeocodedLocation? Location);

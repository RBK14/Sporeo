using Sporeo.BuildingBlocks.Domain.Results;
using Sporeo.Fixtures.Domain.Venues.ValueObjects;

namespace Sporeo.Fixtures.Application.Abstractions.Geocoding;

/// <summary>
/// Port for resolving geographic coordinates and addresses for venues.
/// </summary>
public interface IGeocodingService
{
    /// <summary>
    /// Resolves a venue location from its name and optional address parts.
    /// </summary>
    /// <param name="venueName">The venue display name.</param>
    /// <param name="street">The street address, if known.</param>
    /// <param name="city">The city, if known.</param>
    /// <param name="country">The country, if known.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A successful geocoded location, or a typed failure.</returns>
    Task<Result<GeocodedLocation>> GetVenueLocationAsync(
        string venueName,
        string? street,
        string? city,
        string? country,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Geocoding result containing coordinates and an optional normalized address.
/// </summary>
/// <param name="Coordinates">Resolved geographic coordinates.</param>
/// <param name="Address">Optional postal address returned by the geocoder.</param>
public sealed record GeocodedLocation(
    Coordinates Coordinates,
    Address? Address);

using Sporeo.Fixtures.Domain.Venues.ValueObjects;

namespace Sporeo.Fixtures.Application.Venues.Abstractions.Geocoding;

/// <summary>
/// Geocoding result containing coordinates and an optional normalized address.
/// </summary>
/// <param name="Coordinates">Resolved geographic coordinates.</param>
/// <param name="Address">Optional postal address returned by the geocoder.</param>
public sealed record GeocodedLocation(
    Coordinates Coordinates,
    Address? Address);

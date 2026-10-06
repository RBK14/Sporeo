namespace Sporeo.Fixtures.Domain.Venues.Enums;

/// <summary>
/// Represents the outcome of resolving geographic coordinates for a venue.
/// </summary>
public enum GeocodingStatus
{
    /// <summary>
    /// The venue has no coordinates and is waiting for geocoding.
    /// </summary>
    Pending,

    /// <summary>
    /// The venue has coordinates, supplied by a provider, entered manually, or resolved by geocoding.
    /// </summary>
    Resolved,

    /// <summary>
    /// The geocoding service did not find a location for the venue.
    /// </summary>
    NotFound,

    /// <summary>
    /// Geocoding failed because of a technical error.
    /// </summary>
    Failed
}

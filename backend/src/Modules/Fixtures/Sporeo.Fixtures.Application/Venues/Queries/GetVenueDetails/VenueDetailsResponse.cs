namespace Sporeo.Fixtures.Application.Venues.Queries.GetVenueDetails;

/// <summary>
/// Detailed venue data including optional address and coordinates.
/// </summary>
/// <param name="Id">The venue identifier.</param>
/// <param name="Name">The venue display name.</param>
/// <param name="Street">The street line of the venue address, if specified.</param>
/// <param name="City">The city of the venue address, if specified.</param>
/// <param name="Country">The country of the venue address, if specified.</param>
/// <param name="Latitude">The venue latitude in decimal degrees, if specified.</param>
/// <param name="Longitude">The venue longitude in decimal degrees, if specified.</param>
public sealed record VenueDetailsResponse(
    Guid Id,
    string Name,
    string? Street,
    string? City,
    string? Country,
    double? Latitude,
    double? Longitude);

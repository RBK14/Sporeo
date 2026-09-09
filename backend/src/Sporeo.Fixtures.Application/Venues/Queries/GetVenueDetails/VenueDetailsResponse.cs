namespace Sporeo.Fixtures.Application.Venues.Queries.GetVenueDetails;

public sealed record VenueDetailsResponse(
    Guid Id,
    string Name,
    string? Street,
    string City,
    string Country,
    double? Latitude,
    double? Longitude);

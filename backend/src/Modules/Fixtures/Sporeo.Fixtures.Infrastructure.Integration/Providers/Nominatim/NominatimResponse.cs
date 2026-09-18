using System.Text.Json.Serialization;

namespace Sporeo.Fixtures.Infrastructure.Integration.Providers.Nominatim;

internal sealed record NominatimResponse(
    [property: JsonPropertyName("lat")] string? Latitude,
    [property: JsonPropertyName("lon")] string? Longitude,
    [property: JsonPropertyName("address")] NominatimAddress? Address);

internal sealed record NominatimAddress(
    [property: JsonPropertyName("road")] string? Road,
    [property: JsonPropertyName("city")] string? City,
    [property: JsonPropertyName("town")] string? Town,
    [property: JsonPropertyName("village")] string? Village,
    [property: JsonPropertyName("country")] string? Country);

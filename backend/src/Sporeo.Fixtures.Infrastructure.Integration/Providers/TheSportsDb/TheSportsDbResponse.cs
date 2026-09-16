using System.Text.Json.Serialization;

namespace Sporeo.Fixtures.Infrastructure.Integration.Providers.TheSportsDb;

internal sealed record TheSportsDbResponse(
    [property: JsonPropertyName("events")] TheSportsDbEvent[]? Events);

internal sealed record TheSportsDbEvent(
    [property: JsonPropertyName("idEvent")] string? IdEvent,
    [property: JsonPropertyName("strEvent")] string? StrEvent,
    [property: JsonPropertyName("dateEvent")] string? DateEvent,
    [property: JsonPropertyName("strTimestamp")] string? StrTime,
    [property: JsonPropertyName("idHomeTeam")] string? IdHomeTeam,
    [property: JsonPropertyName("idAwayTeam")] string? IdAwayTeam,
    [property: JsonPropertyName("strStatus")] string? StrStatus,
    [property: JsonPropertyName("idVenue")] string? IdVenue,
    [property: JsonPropertyName("strVenue")] string? StrVenue,
    [property: JsonPropertyName("strCountry")] string? StrCountry);

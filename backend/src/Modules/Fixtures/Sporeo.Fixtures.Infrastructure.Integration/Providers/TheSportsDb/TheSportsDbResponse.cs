using System.Text.Json.Serialization;

namespace Sporeo.Fixtures.Infrastructure.Integration.Providers.TheSportsDb;

internal sealed record TheSportsDbResponse(
    [property: JsonPropertyName("events")] TheSportsDbEvent[]? Events);

internal sealed record TheSportsDbEvent(
    [property: JsonPropertyName("idEvent")] string? IdEvent,
    [property: JsonPropertyName("strEvent")] string? StrEvent,
    [property: JsonPropertyName("dateEvent")] string? DateEvent,
    [property: JsonPropertyName("strTimestamp")] string? StrTimestamp,
    [property: JsonPropertyName("strTime")] string? StrTime,
    [property: JsonPropertyName("idHomeTeam")] string? IdHomeTeam,
    [property: JsonPropertyName("idAwayTeam")] string? IdAwayTeam,
    [property: JsonPropertyName("strStatus")] string? StrStatus,
    [property: JsonPropertyName("idVenue")] string? IdVenue,
    [property: JsonPropertyName("strVenue")] string? StrVenue,
    [property: JsonPropertyName("strCountry")] string? StrCountry);

internal sealed record TheSportsDbSportsResponse(
    [property: JsonPropertyName("sports")] TheSportsDbSport[]? Sports);

internal sealed record TheSportsDbSport(
    [property: JsonPropertyName("idSport")] string? IdSport,
    [property: JsonPropertyName("strSport")] string? StrSport,
    [property: JsonPropertyName("strSportIconGreen")] string? StrSportThumb,
    [property: JsonPropertyName("strSportDescription")] string? StrSportDescription);

internal sealed record TheSportsDbLeaguesResponse(
    [property: JsonPropertyName("leagues")] TheSportsDbLeague[]? Leagues);

internal sealed record TheSportsDbLeague(
    [property: JsonPropertyName("idLeague")] string? IdLeague,
    [property: JsonPropertyName("strLeague")] string? StrLeague,
    [property: JsonPropertyName("strSport")] string? StrSport);
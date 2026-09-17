using System.Globalization;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Sporeo.BuildingBlocks.Domain.Results;
using Sporeo.Fixtures.Application.Catalogs.Abstractions.Providers;
using Sporeo.Fixtures.Application.Fixtures.Abstractions.Providers;
using Sporeo.Fixtures.Domain.Fixtures.Enums;
using Sporeo.Fixtures.Infrastructure.Integration.Observability;

namespace Sporeo.Fixtures.Infrastructure.Integration.Providers.TheSportsDb;

/// <summary>
/// TheSportsDB HTTP client that maps provider payloads to application DTOs.
/// </summary>
/// <remarks>
/// Thread-safe when resolved through <c>IHttpClientFactory</c>. Concurrent requests are supported,
/// but caller-level concurrency should remain bounded to avoid provider rate limits.
/// </remarks>
internal sealed class TheSportsDbClient(
    HttpClient httpClient,
    ILogger<TheSportsDbClient> logger) : IExternalFixturesClient, IExternalCatalogClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly Error Unauthorized = new("ExternalFixtures.Unauthorized", "Provider rejected the API credentials.");
    private static readonly Error RateLimited = new("ExternalFixtures.RateLimited", "Provider rate limit was exceeded.");
    private static readonly Error Transient = new("ExternalFixtures.Transient", "Provider returned a transient failure.");
    private static readonly Error Permanent = new("ExternalFixtures.Permanent", "Provider returned a permanent failure.");
    private static readonly Error InvalidPayload = new("ExternalFixtures.InvalidPayload", "Provider returned an invalid payload.");

    private const string DropReasonMissingId = "missing_id";
    private const string DropReasonInvalidDate = "invalid_date";

    /// <inheritdoc />
    public string ProviderName => "TheSportsDB";

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<ExternalFixtureDto>>> FetchFixturesAsync(
        string externalLeagueId,
        string? externalSeasonId,
        SyncMode syncMode,
        CancellationToken cancellationToken = default)
    {
        return syncMode switch
        {
            SyncMode.ShortTerm => await FetchShortTermAsync(externalLeagueId, cancellationToken),
            SyncMode.LongTerm => await FetchLongTermAsync(externalLeagueId, externalSeasonId, cancellationToken),
            _ => Result.Failure<IReadOnlyList<ExternalFixtureDto>>(
                new Error("ExternalFixtures.UnsupportedSyncMode", $"Unsupported sync mode '{syncMode}'."))
        };
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<ExternalSportDto>>> FetchSportsAsync(CancellationToken cancellationToken = default)
    {
        var apiResult = await FetchFromApiAsync<TheSportsDbSportsResponse>("all_sports.php", cancellationToken);

        if (apiResult.IsFailure)
            return Result.Failure<IReadOnlyList<ExternalSportDto>>(apiResult.Error);

        if (apiResult.Value.Sports is null || apiResult.Value.Sports.Length == 0)
            return Result.Success<IReadOnlyList<ExternalSportDto>>([]);

        var mapped = apiResult.Value.Sports
            .Where(s => !string.IsNullOrWhiteSpace(s.IdSport) && !string.IsNullOrWhiteSpace(s.StrSport))
            .Select(s => new ExternalSportDto(
                ProviderId: s.IdSport!,
                ProviderName: ProviderName,
                Name: s.StrSport!))
            .ToList();

        return Result.Success<IReadOnlyList<ExternalSportDto>>(mapped);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<ExternalLeagueDto>>> FetchLeaguesAsync(CancellationToken cancellationToken = default)
    {
        var apiResult = await FetchFromApiAsync<TheSportsDbLeaguesResponse>("all_leagues.php", cancellationToken);

        if (apiResult.IsFailure)
            return Result.Failure<IReadOnlyList<ExternalLeagueDto>>(apiResult.Error);

        if (apiResult.Value.Leagues is null || apiResult.Value.Leagues.Length == 0)
            return Result.Success<IReadOnlyList<ExternalLeagueDto>>([]);

        var mapped = apiResult.Value.Leagues
            .Where(l => !string.IsNullOrWhiteSpace(l.IdLeague)
                && !string.IsNullOrWhiteSpace(l.StrSport)
                && !string.IsNullOrWhiteSpace(l.StrLeague))
            .Select(l => new ExternalLeagueDto(
                ProviderId: l.IdLeague!,
                ProviderName: ProviderName,
                ProviderSportName: l.StrSport!,
                Name: l.StrLeague!,
                Country: string.Empty))
            .ToList();

        return Result.Success<IReadOnlyList<ExternalLeagueDto>>(mapped);
    }

    private async Task<Result<IReadOnlyList<ExternalFixtureDto>>> FetchShortTermAsync(
        string leagueId,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Running short term sync for league {LeagueId}.", leagueId);

        var pastTask = FetchAndMapAsync("eventspastleague.php", leagueId, seasonId: null, cancellationToken);
        var nextTask = FetchAndMapAsync("eventsnextleague.php", leagueId, seasonId: null, cancellationToken);

        await Task.WhenAll(pastTask, nextTask);

        var pastResult = await pastTask;
        if (pastResult.IsFailure)
            return pastResult;

        var nextResult = await nextTask;
        if (nextResult.IsFailure)
            return nextResult;

        var combined = pastResult.Value
            .Concat(nextResult.Value)
            .DistinctBy(fixture => fixture.ProviderId)
            .ToList();

        return Result.Success<IReadOnlyList<ExternalFixtureDto>>(combined);
    }

    private async Task<Result<IReadOnlyList<ExternalFixtureDto>>> FetchLongTermAsync(
        string leagueId,
        string? seasonId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(seasonId))
        {
            return Result.Failure<IReadOnlyList<ExternalFixtureDto>>(
                new Error("ExternalFixtures.MissingSeasonId", "Long term sync requires ExternalSeasonId."));
        }

        logger.LogInformation("Running long term sync (season {SeasonId}) for league {LeagueId}.", seasonId, leagueId);
        return await FetchAndMapAsync("eventsseason.php", leagueId, seasonId, cancellationToken);
    }

    private async Task<Result<IReadOnlyList<ExternalFixtureDto>>> FetchAndMapAsync(
        string relativePath,
        string leagueId,
        string? seasonId,
        CancellationToken cancellationToken)
    {
        var query = new Dictionary<string, string?>
        {
            ["id"] = leagueId
        };

        if (!string.IsNullOrWhiteSpace(seasonId))
            query["s"] = seasonId;

        var requestUri = QueryHelpers.AddQueryString(relativePath, query);

        var apiResult = await FetchFromApiAsync<TheSportsDbResponse>(requestUri, cancellationToken);
        if (apiResult.IsFailure)
            return Result.Failure<IReadOnlyList<ExternalFixtureDto>>(apiResult.Error);

        var data = apiResult.Value;

        // Empty provider responses are treated as a successful no-op — never as a signal to clear local data.
        if (data?.Events is null || data.Events.Length == 0)
            return Result.Success<IReadOnlyList<ExternalFixtureDto>>([]);

        var mapped = new List<ExternalFixtureDto>(data.Events.Length);
        var dropped = 0;

        foreach (var apiEvent in data.Events)
        {
            var mapResult = MapToDto(apiEvent, relativePath);
            if (mapResult.Dto is null)
            {
                dropped++;
                FixturesIntegrationMetrics.EventsDropped.Add(
                    1,
                    new KeyValuePair<string, object?>("provider", ProviderName),
                    new KeyValuePair<string, object?>("reason", mapResult.DropReason ?? "unknown"));

                logger.LogWarning(
                    "Dropped TheSportsDb event {EventId} for {Path}: {DropReason}",
                    apiEvent.IdEvent ?? "(null)",
                    relativePath,
                    mapResult.DropReason);
                continue;
            }

            mapped.Add(mapResult.Dto);
        }

        if (dropped > 0)
        {
            logger.LogWarning(
                "Dropped {DroppedCount} of {TotalCount} TheSportsDb events for {Path} due to invalid mapping.",
                dropped,
                data.Events.Length,
                relativePath);
        }

        // All events present but every one failed validation/parsing — surface as InvalidPayload.
        if (mapped.Count == 0)
            return Result.Failure<IReadOnlyList<ExternalFixtureDto>>(InvalidPayload);

        return Result.Success<IReadOnlyList<ExternalFixtureDto>>(mapped);
    }

    private async Task<Result<T>> FetchFromApiAsync<T>(string requestUri, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.GetAsync(
                requestUri,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                return Result.Failure<T>(Unauthorized);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
                return Result.Failure<T>(RateLimited);

            if ((int)response.StatusCode >= 500)
                return Result.Failure<T>(Transient);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "TheSportsDb API error: {Path} returned status {StatusCode}",
                    requestUri,
                    (int)response.StatusCode);
                return Result.Failure<T>(Permanent);
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var data = await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken);

            if (data is null)
                return Result.Failure<T>(InvalidPayload);

            return Result.Success(data);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError("Transient HTTP failure while fetching {Path}: {ExceptionType}.", requestUri, ex.GetType().Name);
            return Result.Failure<T>(Transient);
        }
        catch (JsonException ex)
        {
            logger.LogError("Invalid JSON payload while fetching {Path}: {ExceptionType}.", requestUri, ex.GetType().Name);
            return Result.Failure<T>(InvalidPayload);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError("Unexpected error while fetching {Path}: {ExceptionType}.", requestUri, ex.GetType().Name);
            return Result.Failure<T>(Permanent);
        }
    }

    private MapResult MapToDto(TheSportsDbEvent apiEvent, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(apiEvent.IdEvent))
            return MapResult.Dropped(DropReasonMissingId);

        if (!TryParseStartDate(apiEvent.StrTimestamp, apiEvent.DateEvent, apiEvent.StrTime, out var startDate))
            return MapResult.Dropped(DropReasonInvalidDate);

        ExternalFixtureVenueDto? venueDto = null;
        if (!string.IsNullOrWhiteSpace(apiEvent.StrVenue))
        {
            var venueProviderId = string.IsNullOrWhiteSpace(apiEvent.IdVenue)
                ? $"fallback-{apiEvent.StrVenue.Trim().Replace(" ", "-").ToLowerInvariant()}"
                : apiEvent.IdVenue;

            var country = string.IsNullOrWhiteSpace(apiEvent.StrCountry) ? null : apiEvent.StrCountry.Trim();

            venueDto = new ExternalFixtureVenueDto(
                ProviderId: venueProviderId,
                ProviderName: ProviderName,
                Name: apiEvent.StrVenue,
                Street: null,
                City: null,
                Country: country,
                Latitude: null,
                Longitude: null);
        }

        return MapResult.Mapped(new ExternalFixtureDto(
            ProviderId: apiEvent.IdEvent,
            ProviderName: ProviderName,
            Name: string.IsNullOrWhiteSpace(apiEvent.StrEvent) ? "Unknown Match" : apiEvent.StrEvent,
            StartDate: startDate,
            Status: MapFixtureStatus(apiEvent.StrStatus, apiEvent.IdEvent, relativePath),
            Venue: venueDto));
    }

    /// <summary>
    /// Parses fixture start time preferring <paramref name="strTimestamp"/>, falling back to
    /// <paramref name="dateEvent"/> + <paramref name="strTime"/>. Results are normalized to UTC.
    /// </summary>
    private static bool TryParseStartDate(
        string? strTimestamp,
        string? dateEvent,
        string? strTime,
        out DateTimeOffset startDate)
    {
        if (!string.IsNullOrWhiteSpace(strTimestamp) &&
            TryParseAsUtc(strTimestamp.Trim(), out startDate))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(dateEvent) &&
            !string.IsNullOrWhiteSpace(strTime))
        {
            var raw = $"{dateEvent.Trim()} {strTime.Trim()}";
            if (TryParseAsUtc(raw, out startDate))
                return true;
        }

        startDate = default;
        return false;
    }

    private static bool TryParseAsUtc(string value, out DateTimeOffset startDate)
    {
        if (DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out startDate))
        {
            return true;
        }

        // Exact common TheSportsDB formats when culture-aware parse fails.
        string[] formats =
        [
            "yyyy-MM-dd'T'HH:mm:ss",
            "yyyy-MM-dd'T'HH:mm:ssK",
            "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFK",
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-dd HH:mm"
        ];

        if (DateTimeOffset.TryParseExact(
                value,
                formats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out startDate))
        {
            return true;
        }

        startDate = default;
        return false;
    }

    private FixtureStatus MapFixtureStatus(string? apiStatus, string eventId, string relativePath)
    {
        switch (apiStatus)
        {
            case "Match Finished":
                return FixtureStatus.Finished;
            case "Not Started":
                return FixtureStatus.Scheduled;
            case "Postponed":
                return FixtureStatus.Postponed;
            case "Cancelled":
                return FixtureStatus.Cancelled;
            default:
                FixturesIntegrationMetrics.UnknownStatuses.Add(
                    1,
                    new KeyValuePair<string, object?>("provider", ProviderName));

                logger.LogWarning(
                    "Unknown TheSportsDb status '{ApiStatus}' for event {EventId} on {Path}; defaulting to Scheduled.",
                    apiStatus ?? "(null)",
                    eventId,
                    relativePath);

                return FixtureStatus.Scheduled;
        }
    }

    private readonly record struct MapResult(ExternalFixtureDto? Dto, string? DropReason)
    {
        public static MapResult Mapped(ExternalFixtureDto dto) => new(dto, null);
        public static MapResult Dropped(string reason) => new(null, reason);
    }
}

/// <summary>
/// Minimal query-string helper to avoid an ASP.NET Core dependency in Integration.
/// </summary>
file static class QueryHelpers
{
    public static string AddQueryString(string path, IDictionary<string, string?> values)
    {
        var query = string.Join(
            "&",
            values
                .Where(pair => !string.IsNullOrWhiteSpace(pair.Value))
                .Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value!)}"));

        return string.IsNullOrEmpty(query) ? path : $"{path}?{query}";
    }
}

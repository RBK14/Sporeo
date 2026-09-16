using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Sporeo.BuildingBlocks.Domain.Results;
using Sporeo.Fixtures.Application.Abstractions.Providers;
using Sporeo.Fixtures.Domain.Fixtures.Enums;

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
    ILogger<TheSportsDbClient> logger) : IExternalFixturesClient
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

        try
        {
            using var response = await httpClient.GetAsync(
                requestUri,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                return Result.Failure<IReadOnlyList<ExternalFixtureDto>>(Unauthorized);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
                return Result.Failure<IReadOnlyList<ExternalFixtureDto>>(RateLimited);

            if ((int)response.StatusCode >= 500)
                return Result.Failure<IReadOnlyList<ExternalFixtureDto>>(Transient);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "TheSportsDb API error: {Path} returned status {StatusCode}",
                    relativePath,
                    (int)response.StatusCode);
                return Result.Failure<IReadOnlyList<ExternalFixtureDto>>(Permanent);
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var data = await JsonSerializer.DeserializeAsync<TheSportsDbResponse>(stream, JsonOptions, cancellationToken);

            if (data?.Events is null || data.Events.Length == 0)
                return Result.Success<IReadOnlyList<ExternalFixtureDto>>([]);

            var mapped = new List<ExternalFixtureDto>(data.Events.Length);
            var dropped = 0;

            foreach (var apiEvent in data.Events)
            {
                var dto = MapToDto(apiEvent);
                if (dto is null)
                {
                    dropped++;
                    continue;
                }

                mapped.Add(dto);
            }

            if (dropped > 0)
            {
                logger.LogWarning(
                    "Dropped {DroppedCount} TheSportsDb events for {Path} due to invalid mapping.",
                    dropped,
                    relativePath);
            }

            return Result.Success<IReadOnlyList<ExternalFixtureDto>>(mapped);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Transient HTTP failure while fetching {Path}.", relativePath);
            return Result.Failure<IReadOnlyList<ExternalFixtureDto>>(Transient);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Invalid JSON payload while fetching {Path}.", relativePath);
            return Result.Failure<IReadOnlyList<ExternalFixtureDto>>(InvalidPayload);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Unexpected error while fetching {Path}.", relativePath);
            return Result.Failure<IReadOnlyList<ExternalFixtureDto>>(Permanent);
        }
    }

    private ExternalFixtureDto? MapToDto(TheSportsDbEvent apiEvent)
    {
        if (string.IsNullOrWhiteSpace(apiEvent.IdEvent) ||
            string.IsNullOrWhiteSpace(apiEvent.DateEvent) ||
            string.IsNullOrWhiteSpace(apiEvent.StrTime))
        {
            return null;
        }

        if (!TryParseStartDate(apiEvent.DateEvent, apiEvent.StrTime, out var startDate))
            return null;

        ExternalFixtureVenueDto? venueDto = null;
        if (!string.IsNullOrWhiteSpace(apiEvent.IdVenue) && !string.IsNullOrWhiteSpace(apiEvent.StrVenue))
        {
            venueDto = new ExternalFixtureVenueDto(
                ProviderId: apiEvent.IdVenue,
                ProviderName: ProviderName,
                Name: apiEvent.StrVenue,
                Street: null,
                City: null,
                Country: null,
                Latitude: null,
                Longitude: null);
        }

        return new ExternalFixtureDto(
            ProviderId: apiEvent.IdEvent,
            ProviderName: ProviderName,
            Name: string.IsNullOrWhiteSpace(apiEvent.StrEvent) ? "Unknown Match" : apiEvent.StrEvent,
            StartDate: startDate,
            Status: MapFixtureStatus(apiEvent.StrStatus),
            Venue: venueDto);
    }

    private static bool TryParseStartDate(string dateEvent, string strTime, out DateTimeOffset startDate)
    {
        var raw = $"{dateEvent} {strTime}";
        if (DateTimeOffset.TryParse(
                raw,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out startDate))
        {
            return true;
        }

        startDate = default;
        return false;
    }

    private static FixtureStatus MapFixtureStatus(string? apiStatus) =>
        apiStatus switch
        {
            "Match Finished" => FixtureStatus.Finished,
            "Not Started" => FixtureStatus.Scheduled,
            "Postponed" => FixtureStatus.Postponed,
            "Cancelled" => FixtureStatus.Cancelled,
            _ => FixtureStatus.Scheduled
        };
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

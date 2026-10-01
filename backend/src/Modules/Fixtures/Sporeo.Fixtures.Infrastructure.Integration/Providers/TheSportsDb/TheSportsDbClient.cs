using Microsoft.Extensions.Logging;
using Sporeo.BuildingBlocks.Domain.Results;
using Sporeo.Fixtures.Application.Catalogs.Abstractions;
using Sporeo.Fixtures.Application.Fixtures.Abstractions;
using Sporeo.Fixtures.Infrastructure.Integration.Observability;
using Errors = Sporeo.Fixtures.Application.Common.Errors;

namespace Sporeo.Fixtures.Infrastructure.Integration.Providers.TheSportsDb;

/// <summary>
/// TheSportsDB client facade that maps provider payloads to application DTOs.
/// </summary>
/// <remarks>
/// Thread-safe when resolved through <c>IHttpClientFactory</c>. Concurrent requests are supported,
/// but caller-level concurrency should remain bounded to avoid provider rate limits.
/// </remarks>
internal sealed class TheSportsDbClient(
    TheSportsDbApi api,
    TheSportsDbFixtureMapper fixtureMapper,
    ILogger<TheSportsDbClient> logger) : IExternalFixturesClient, IExternalCatalogClient
{
    /// <inheritdoc />
    public string ProviderName => "TheSportsDB";

    public async Task<Result<IReadOnlyList<ExternalFixtureDto>>> FetchShortTermFixturesAsync(
        string externalLeagueId,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Running short term sync for league {LeagueId}.", externalLeagueId);

        var pastTask = FetchAndMapFixturesAsync("eventspastleague.php", externalLeagueId, seasonId: null, cancellationToken);
        var nextTask = FetchAndMapFixturesAsync("eventsnextleague.php", externalLeagueId, seasonId: null, cancellationToken);

        await Task.WhenAll(pastTask, nextTask);

        var pastResult = await pastTask;
        if (pastResult.IsFailure)
            return pastResult;

        var nextResult = await nextTask;
        if (nextResult.IsFailure)
            return nextResult;

        var combined = pastResult.Value
            .Concat(nextResult.Value)
            .DistinctBy(fixture => fixture.ExternalId)
            .ToList();

        return Result.Success<IReadOnlyList<ExternalFixtureDto>>(combined);
    }

    public async Task<Result<IReadOnlyList<ExternalFixtureDto>>> FetchLongTermFixturesAsync(
        string externalLeagueId,
        string seasonName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(seasonName))
        {
            return Result.Failure<IReadOnlyList<ExternalFixtureDto>>(Errors.ExternalFixtures.MissingSeasonId);
        }

        logger.LogInformation("Running long term sync (season {SeasonId}) for league {LeagueId}.", seasonName, externalLeagueId);
        return await FetchAndMapFixturesAsync("eventsseason.php", externalLeagueId, seasonName, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<ExternalSportDto>>> FetchSportsAsync(CancellationToken cancellationToken = default)
    {
        var apiResult = await api.FetchAsync<TheSportsDbSportsResponse>("all_sports.php", cancellationToken);

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
        var apiResult = await api.FetchAsync<TheSportsDbLeaguesResponse>("all_leagues.php", cancellationToken);

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

    private async Task<Result<IReadOnlyList<ExternalFixtureDto>>> FetchAndMapFixturesAsync(
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

        var apiResult = await api.FetchAsync<TheSportsDbEventsResponse>(requestUri, cancellationToken);
        if (apiResult.IsFailure)
            return Result.Failure<IReadOnlyList<ExternalFixtureDto>>(apiResult.Error);

        var data = apiResult.Value;

        if (data?.Events is null || data.Events.Length == 0)
            return Result.Success<IReadOnlyList<ExternalFixtureDto>>([]);

        var mapped = new List<ExternalFixtureDto>(data.Events.Length);
        var dropped = 0;

        foreach (var apiEvent in data.Events)
        {
            var mapResult = fixtureMapper.MapToDto(apiEvent, relativePath);
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

        if (mapped.Count == 0)
            return Result.Failure<IReadOnlyList<ExternalFixtureDto>>(Errors.ExternalFixtures.InvalidPayload);

        return Result.Success<IReadOnlyList<ExternalFixtureDto>>(mapped);
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

using Sporeo.BuildingBlocks.Application.Abstractions.Caching;
using Sporeo.BuildingBlocks.Application.Abstractions.Execution;
using Sporeo.BuildingBlocks.Application.Pagination;
using Sporeo.BuildingBlocks.Domain.Results;
using Sporeo.Fixtures.Application.Catalogs.Abstractions;
using Sporeo.Fixtures.Application.Catalogs.Common;
using Sporeo.Fixtures.Application.Leagues.Data;
using Sporeo.Fixtures.Application.Sports.Data;
using Sporeo.Fixtures.Domain.Leagues.ValueObjects;
using Sporeo.Fixtures.Domain.Sports.ValueObjects;

namespace Sporeo.Fixtures.Application.Catalogs.Queries.GetCatalog;

/// <summary>
/// Loads a paged sports catalog from cache (or the external provider), enriched with local monitoring status.
/// </summary>
internal sealed class GetCatalogQueryHandler(
    ILeagueReadStore leagueReadStore,
    ISportReadStore sportReadStore,
    ICacheService cacheService,
    IExternalCatalogClient externalClient) : IQueryHandler<GetCatalogQuery, PagedResult<CatalogSportReadModel>>
{
    /// <inheritdoc />
    public async Task<Result<PagedResult<CatalogSportReadModel>>> Handle(GetCatalogQuery request, CancellationToken cancellationToken)
    {
        var providerName = externalClient.ProviderName;
        var sportsCacheKey = CatalogCacheKeys.SportsKey(providerName);
        var leaguesCacheKey = CatalogCacheKeys.LeaguesKey(providerName);

        var cachedSports = await cacheService.GetAsync<List<ExternalSportDto>>(sportsCacheKey, cancellationToken) ?? [];
        var cachedLeagues = await cacheService.GetAsync<List<ExternalLeagueDto>>(leaguesCacheKey, cancellationToken) ?? [];

        if (cachedSports.Count == 0 || cachedLeagues.Count == 0)
        {
            var externalSports = await externalClient.FetchSportsAsync(cancellationToken);
            var externalLeagues = await externalClient.FetchLeaguesAsync(cancellationToken);

            if (externalSports.IsFailure)
                return Result.Failure<PagedResult<CatalogSportReadModel>>(externalSports.Error);

            if (externalLeagues.IsFailure)
                return Result.Failure<PagedResult<CatalogSportReadModel>>(externalLeagues.Error);

            cachedSports = externalSports.Value.ToList();
            cachedLeagues = externalLeagues.Value.ToList();

            await cacheService.SetAsync(sportsCacheKey, externalSports.Value, TimeSpan.FromHours(12), cancellationToken);
            await cacheService.SetAsync(leaguesCacheKey, externalLeagues.Value, TimeSpan.FromHours(12), cancellationToken);
        }

        // Catalog size is small (dozens of sports); page in memory after cache load.
        var pagedSportDtos = cachedSports
            .Skip((int)request.Pagination.Offset)
            .Take(request.Pagination.PageSize)
            .ToList();

        if (pagedSportDtos.Count == 0)
        {
            return Result.Success(new PagedResult<CatalogSportReadModel>(
                [],
                cachedSports.Count,
                request.Pagination));
        }

        var sportKeys = pagedSportDtos
            .Select(s => (s.ProviderName, s.Name))
            .ToHashSet();

        var pagedLeaguesDto = cachedLeagues
            .Where(l => sportKeys.Contains((l.ProviderName, l.ProviderSportName)))
            .ToList();

        var sportStatusesByProvider = new Dictionary<(string ProviderName, string ProviderId), SportId>();
        foreach (var group in pagedSportDtos.GroupBy(s => s.ProviderName, StringComparer.Ordinal))
        {
            var statuses = await sportReadStore.GetSportStatusesAsync(
                group.Key,
                group.Select(s => s.ProviderId),
                cancellationToken);

            foreach (var (providerId, sportId) in statuses)
                sportStatusesByProvider[(group.Key, providerId)] = sportId;
        }

        var leagueStatusesByProvider = new Dictionary<(string ProviderName, string ProviderId), (LeagueId Id, bool IsMonitored)>();
        foreach (var group in pagedLeaguesDto.GroupBy(l => l.ProviderName, StringComparer.Ordinal))
        {
            var statuses = await leagueReadStore.GetLeagueStatusesAsync(
                group.Key,
                group.Select(l => l.ProviderId),
                cancellationToken);

            foreach (var (providerId, status) in statuses)
                leagueStatusesByProvider[(group.Key, providerId)] = status;
        }

        var pagedSports = pagedSportDtos
            .Select(sport =>
            {
                SportId? localSportId = sportStatusesByProvider.TryGetValue(
                    (sport.ProviderName, sport.ProviderId),
                    out var sportId)
                    ? sportId
                    : null;

                return new CatalogSportReadModel(
                    localSportId,
                    sport.ProviderId,
                    sport.ProviderName,
                    sport.Name,
                    pagedLeaguesDto
                        .Where(league =>
                            league.ProviderName == sport.ProviderName
                            && league.ProviderSportName == sport.Name)
                        .Select(league =>
                        {
                            var existsInDb = leagueStatusesByProvider.TryGetValue(
                                (league.ProviderName, league.ProviderId),
                                out var dbStatus);

                            return new CatalogLeagueReadModel(
                                existsInDb ? dbStatus.Id : null,
                                league.ProviderId,
                                league.ProviderName,
                                league.Name,
                                existsInDb && dbStatus.IsMonitored);
                        })
                        .ToList());
            })
            .ToList();

        return Result.Success(new PagedResult<CatalogSportReadModel>(
            pagedSports,
            cachedSports.Count,
            request.Pagination));
    }
}

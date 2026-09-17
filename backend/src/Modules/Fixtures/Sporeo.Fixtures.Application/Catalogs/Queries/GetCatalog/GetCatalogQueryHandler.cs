using Sporeo.BuildingBlocks.Application.Abstractions.Caching;
using Sporeo.BuildingBlocks.Application.Abstractions.Execution;
using Sporeo.BuildingBlocks.Application.Pagination;
using Sporeo.BuildingBlocks.Domain.Results;
using Sporeo.Fixtures.Application.Catalogs.Abstractions.Providers;
using Sporeo.Fixtures.Application.Catalogs.Common;
using Sporeo.Fixtures.Application.Leagues.Abstractions.ReadModels;

namespace Sporeo.Fixtures.Application.Catalogs.Queries.GetCatalog;

internal sealed class GetCatalogQueryHandler(
    ILeagueReadStore leagueReadStore,
    ICacheService cacheService,
    IExternalCatalogClient externalClient) : IQueryHandler<GetCatalogQuery, PagedResult<CatalogSportResponse>>
{
    // todo: Dodać SportId do Response i przekazywać je jeżeli Sport istnieje w DB
    public async Task<Result<PagedResult<CatalogSportResponse>>> Handle(GetCatalogQuery request, CancellationToken cancellationToken)
    {
        var cachedSports = await cacheService.GetAsync<List<ExternalSportDto>>(CatalogCacheKeys.SportsCacheKey, cancellationToken) ?? [];
        var cachedLeagues = await cacheService.GetAsync<List<ExternalLeagueDto>>(CatalogCacheKeys.LeaguesCacheKey, cancellationToken) ?? [];

        if (cachedSports.Count == 0 || cachedLeagues.Count == 0)
        {
            var externalSports = await externalClient.FetchSportsAsync(cancellationToken);
            var externalLeagues = await externalClient.FetchLeaguesAsync(cancellationToken);

            if (externalSports.IsFailure)
                return Result.Failure<PagedResult<CatalogSportResponse>>(externalSports.Error);

            if (externalLeagues.IsFailure)
                return Result.Failure<PagedResult<CatalogSportResponse>>(externalLeagues.Error);

            cachedSports = externalSports.Value.ToList();
            cachedLeagues = externalLeagues.Value.ToList();

            await cacheService.SetAsync(CatalogCacheKeys.SportsCacheKey, externalSports.Value, TimeSpan.FromHours(12), cancellationToken);
            await cacheService.SetAsync(CatalogCacheKeys.LeaguesCacheKey, externalLeagues.Value, TimeSpan.FromHours(12), cancellationToken);
        }

        var pagedSportDtos = cachedSports
            .Skip((int)request.Pagination.Offset)
            .Take(request.Pagination.PageSize)
            .ToList();

        if (pagedSportDtos.Count == 0)
        {
            return Result.Success(new PagedResult<CatalogSportResponse>(
                [],
                cachedSports.Count,
                request.Pagination));
        }

        var sportNames = pagedSportDtos.Select(s => s.Name).ToHashSet();
        var pagedLeaguesDto = cachedLeagues
            .Where(l => sportNames.Contains(l.ProviderSportName))
            .ToList();

        var providerName = pagedSportDtos.First().ProviderName;
        var leagueProviderIds = pagedLeaguesDto.Select(l => l.ProviderId).ToList();

        var leagueStatusesMap = await leagueReadStore.GetLeagueStatusesAsync(
            providerName,
            leagueProviderIds,
            cancellationToken);

        var pagedSports = pagedSportDtos
        .Select(sport => new CatalogSportResponse(
            sport.ProviderId,
            sport.ProviderName,
            sport.Name,
            pagedLeaguesDto
                .Where(league => league.ProviderSportName == sport.Name)
                .Select(league =>
                {
                    var existsInDb = leagueStatusesMap.TryGetValue(league.ProviderId, out var dbStatus);

                    return new CatalogLeagueResponse(
                        existsInDb ? dbStatus.Id.Value : null,
                        league.ProviderId,
                        league.ProviderName,
                        league.Name,
                        existsInDb ? dbStatus.IsMonitored : false);
                })
                .ToList()))
        .ToList();

        return Result.Success(new PagedResult<CatalogSportResponse>(
            pagedSports,
            cachedSports.Count,
            request.Pagination));
    }
}

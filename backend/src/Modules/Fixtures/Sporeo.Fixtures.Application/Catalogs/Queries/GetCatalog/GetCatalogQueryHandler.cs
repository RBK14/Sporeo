using Sporeo.BuildingBlocks.Application.Abstractions.Caching;
using Sporeo.BuildingBlocks.Application.Abstractions.Execution;
using Sporeo.BuildingBlocks.Application.Pagination;
using Sporeo.BuildingBlocks.Domain.Results;
using Sporeo.Fixtures.Application.Catalogs.Abstractions.Providers;
using Sporeo.Fixtures.Application.Catalogs.Common;

namespace Sporeo.Fixtures.Application.Catalogs.Queries.GetCatalog;

internal sealed class GetCatalogQueryHandler(
    ICacheService cacheService,
    IExternalCatalogClient externalClient) : IQueryHandler<GetCatalogQuery, PagedResult<CatalogSportResponse>>
{
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

        var pagedSports = cachedSports
            .Skip((int)request.Pagination.Offset)
            .Take(request.Pagination.PageSize)
            .Select(sport => new CatalogSportResponse(
                sport.ProviderId,
                sport.ProviderName,
                sport.Name,
                cachedLeagues
                    .Where(league => league.ProviderSportName == sport.Name)
                    .Select(league => new CatalogLeagueResponse(
                        league.ProviderId,
                        league.ProviderName,
                        league.Name))
                    .ToList()))
            .ToList();

        return Result.Success(new PagedResult<CatalogSportResponse>(
            pagedSports,
            cachedSports.Count,
            request.Pagination));
    }
}

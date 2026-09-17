using Sporeo.BuildingBlocks.Application.Abstractions.Caching;
using Sporeo.BuildingBlocks.Application.Abstractions.Execution;
using Sporeo.BuildingBlocks.Domain.Results;
using Sporeo.Fixtures.Application.Catalogs.Abstractions.Providers;
using Sporeo.Fixtures.Application.Catalogs.Common;
using Sporeo.Fixtures.Application.Leagues.Abstractions.Repositories;
using Sporeo.Fixtures.Application.Sports.Abstractions.Repositories;
using Sporeo.Fixtures.Domain.Leagues;
using Sporeo.Fixtures.Domain.Sports;

namespace Sporeo.Fixtures.Application.Catalogs.Commands.UpdateMonitoring;

internal sealed class UpdateMonitoringCommandHandler(
    ISportRepository sportRepository,
    ILeagueRepository leagueRepository,
    ICacheService cacheService) : ICommandHandler<UpdateMonitoringCommand>
{
    public async Task<Result> Handle(UpdateMonitoringCommand request, CancellationToken cancellationToken)
    {
        var cachedSports = await cacheService.GetAsync<List<ExternalSportDto>>(CatalogCacheKeys.SportsCacheKey, cancellationToken) ?? [];
        var cachedLeagues = await cacheService.GetAsync<List<ExternalLeagueDto>>(CatalogCacheKeys.LeaguesCacheKey, cancellationToken) ?? [];

        if (cachedSports.Count == 0 || cachedLeagues.Count == 0)
            return Result.Failure(new Error("Catalog.CacheExpired", "Catalog data expired. Please refresh the page."));

        var cachedSportsDict = cachedSports.ToDictionary(s => s.ProviderId);
        var cachedLeaguesDict = cachedLeagues.ToDictionary(l => l.ProviderId);

        var incomingSportIds = request.Sports.Select(s => s.ProviderId).ToList();
        var incomingLeagueIds = request.Sports.SelectMany(s => s.Leagues.Select(l => l.ProviderId)).ToList();

        // Assusming that the provider name is consistent across all sports and leagues in the request
        var providerName = request.Sports.FirstOrDefault()?.ProviderName;

        if (string.IsNullOrEmpty(providerName))
            return Result.Failure(new Error("Catalog.InvalidRequest", "Provider name is missing in the request."));

        var existingSportsList = await sportRepository.GetByExternalProviderIdsAsync(providerName, incomingSportIds, cancellationToken);
        var existingSports = existingSportsList.ToDictionary(s => s.ExternalProviderId!);

        var existingLeaguesList = await leagueRepository.GetByExternalProviderIdsAsync(providerName, incomingLeagueIds, cancellationToken);
        var existingLeagues = existingLeaguesList.ToDictionary(l => l.ExternalProviderId!);

        foreach (var sportCommandDto in request.Sports)
        {
            if (!cachedSportsDict.TryGetValue(sportCommandDto.ProviderId, out var cachedSport))
                continue;

            if (!existingSports.TryGetValue(sportCommandDto.ProviderId, out var sportEntity))
            {
                var sportResult = Sport.Create(cachedSport.Name, providerName, cachedSport.ProviderId);
                if (sportResult.IsFailure) return sportResult;

                sportEntity = sportResult.Value;
                sportRepository.Add(sportEntity);
            }

            foreach (var leagueCommandDto in sportCommandDto.Leagues)
            {
                if (!cachedLeaguesDict.TryGetValue(leagueCommandDto.ProviderId, out var cachedLeague))
                    continue;

                if (existingLeagues.TryGetValue(leagueCommandDto.ProviderId, out var leagueEntity))
                {
                    var statusResult = leagueEntity.ChangeMonitoringStatus(leagueCommandDto.IsMonitored);
                    if (statusResult.IsFailure) return statusResult;
                }
                else if (leagueCommandDto.IsMonitored)
                {
                    var leagueResult = League.CreateFromProvider(
                        sportEntity.Id,
                        cachedLeague.Name,
                        cachedLeague.Country,
                        providerName,
                        cachedLeague.ProviderId);

                    if (leagueResult.IsFailure) return leagueResult;

                    leagueRepository.Add(leagueResult.Value);
                }
            }
        }

        return Result.Success();
    }
}

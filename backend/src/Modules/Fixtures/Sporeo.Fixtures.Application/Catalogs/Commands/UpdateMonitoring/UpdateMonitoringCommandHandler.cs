using Sporeo.BuildingBlocks.Application.Abstractions.Caching;
using Sporeo.BuildingBlocks.Application.Abstractions.Execution;
using Sporeo.BuildingBlocks.Domain.Results;
using Sporeo.Fixtures.Application.Catalogs.Abstractions;
using Sporeo.Fixtures.Application.Catalogs.Common;
using Sporeo.Fixtures.Application.Leagues.Data;
using Sporeo.Fixtures.Application.Sports.Data;
using Sporeo.Fixtures.Domain.Leagues;
using Sporeo.Fixtures.Domain.Leagues.ValueObjects;
using Sporeo.Fixtures.Domain.Sports;
using Sporeo.Fixtures.Domain.Sports.ValueObjects;
using DomainErrors = Sporeo.Fixtures.Domain.Common.Errors;
using Errors = Sporeo.Fixtures.Application.Common.Errors;

namespace Sporeo.Fixtures.Application.Catalogs.Commands.UpdateMonitoring;

/// <summary>
/// Upserts sports and leagues from the admin catalog selection and applies monitoring flags.
/// Prefer local ids when present; fall back to provider identity per item.
/// </summary>
internal sealed class UpdateMonitoringCommandHandler(
    ISportRepository sportRepository,
    ILeagueRepository leagueRepository,
    ICacheService cacheService) : ICommandHandler<UpdateMonitoringCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(UpdateMonitoringCommand request, CancellationToken cancellationToken)
    {
        if (request.Sports.Count == 0)
            return Result.Failure(Errors.Catalog.InvalidRequest);

        foreach (var sportDto in request.Sports)
        {
            if (string.IsNullOrWhiteSpace(sportDto.ProviderName) || string.IsNullOrWhiteSpace(sportDto.ProviderId))
                return Result.Failure(Errors.Catalog.InvalidRequest);

            foreach (var leagueDto in sportDto.Leagues)
            {
                if (string.IsNullOrWhiteSpace(leagueDto.ProviderName) || string.IsNullOrWhiteSpace(leagueDto.ProviderId))
                    return Result.Failure(Errors.Catalog.InvalidRequest);
            }
        }

        var sportIds = request.Sports
            .Where(s => s.Id is not null)
            .Select(s => s.Id!)
            .Distinct()
            .ToList();

        var leagueIds = request.Sports
            .SelectMany(s => s.Leagues)
            .Where(l => l.Id is not null)
            .Select(l => l.Id!)
            .Distinct()
            .ToList();

        var sportsById = (await sportRepository.GetByIdsAsync(sportIds, cancellationToken))
            .ToDictionary(s => s.Id);

        var leaguesById = (await leagueRepository.GetByIdsAsync(leagueIds, cancellationToken))
            .ToDictionary(l => l.Id);

        var sportsByProvider = new Dictionary<(string ProviderName, string ProviderId), Sport>();
        var leaguesByProvider = new Dictionary<(string ProviderName, string ProviderId), League>();

        foreach (var group in request.Sports.GroupBy(s => s.ProviderName, StringComparer.Ordinal))
        {
            var providerIds = group.Select(s => s.ProviderId).Distinct(StringComparer.Ordinal).ToList();
            var existing = await sportRepository.GetByExternalProviderIdsAsync(group.Key, providerIds, cancellationToken);
            foreach (var sport in existing)
            {
                if (sport.ExternalProviderId is null)
                    continue;

                sportsByProvider[(sport.ExternalProviderName!, sport.ExternalProviderId)] = sport;
            }
        }

        foreach (var group in request.Sports
                     .SelectMany(s => s.Leagues)
                     .GroupBy(l => l.ProviderName, StringComparer.Ordinal))
        {
            var providerIds = group.Select(l => l.ProviderId).Distinct(StringComparer.Ordinal).ToList();
            var existing = await leagueRepository.GetByExternalProviderIdsAsync(group.Key, providerIds, cancellationToken);
            foreach (var league in existing)
            {
                if (league.ExternalProviderId is null)
                    continue;

                leaguesByProvider[(league.ExternalProviderName!, league.ExternalProviderId)] = league;
            }
        }

        var needsCache = request.Sports.Any(sportDto =>
            ResolveExistingSport(sportDto, sportsById, sportsByProvider) is null
            || sportDto.Leagues.Any(leagueDto =>
                leagueDto.IsMonitored
                && ResolveExistingLeague(leagueDto, leaguesById, leaguesByProvider) is null));

        Dictionary<(string ProviderName, string ProviderId), ExternalSportDto>? cachedSportsDict = null;
        Dictionary<(string ProviderName, string ProviderId), ExternalLeagueDto>? cachedLeaguesDict = null;

        if (needsCache)
        {
            var providerNames = request.Sports
                .Select(s => s.ProviderName)
                .Concat(request.Sports.SelectMany(s => s.Leagues.Select(l => l.ProviderName)))
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            var cachedSports = new List<ExternalSportDto>();
            var cachedLeagues = new List<ExternalLeagueDto>();

            foreach (var providerName in providerNames)
            {
                var sports = await cacheService.GetAsync<List<ExternalSportDto>>(
                    CatalogCacheKeys.SportsKey(providerName),
                    cancellationToken) ?? [];
                var leagues = await cacheService.GetAsync<List<ExternalLeagueDto>>(
                    CatalogCacheKeys.LeaguesKey(providerName),
                    cancellationToken) ?? [];

                if (sports.Count == 0 || leagues.Count == 0)
                    return Result.Failure(Errors.Catalog.CacheExpired);

                cachedSports.AddRange(sports);
                cachedLeagues.AddRange(leagues);
            }

            if (cachedSports.Count == 0 || cachedLeagues.Count == 0)
                return Result.Failure(Errors.Catalog.CacheExpired);

            cachedSportsDict = cachedSports
                .GroupBy(s => (s.ProviderName, s.ProviderId))
                .ToDictionary(g => g.Key, g => g.First());

            cachedLeaguesDict = cachedLeagues
                .GroupBy(l => (l.ProviderName, l.ProviderId))
                .ToDictionary(g => g.Key, g => g.First());
        }

        foreach (var sportCommandDto in request.Sports)
        {
            var sportEntity = ResolveExistingSport(sportCommandDto, sportsById, sportsByProvider);

            if (sportCommandDto.Id is not null && sportEntity is null)
                return Result.Failure(DomainErrors.Sport.NotFound(sportCommandDto.Id));

            if (sportEntity is not null)
            {
                if (!MatchesProviderIdentity(
                        sportEntity.ExternalProviderName,
                        sportEntity.ExternalProviderId,
                        sportCommandDto.ProviderName,
                        sportCommandDto.ProviderId))
                {
                    return Result.Failure(Errors.Catalog.IdentityMismatch);
                }
            }
            else
            {
                if (cachedSportsDict is null
                    || !cachedSportsDict.TryGetValue(
                        (sportCommandDto.ProviderName, sportCommandDto.ProviderId),
                        out var cachedSport))
                {
                    return Result.Failure(Errors.Catalog.ItemNotFoundInCache);
                }

                var sportResult = Sport.Create(
                    cachedSport.Name,
                    sportCommandDto.ProviderName,
                    cachedSport.ProviderId);

                if (sportResult.IsFailure)
                    return sportResult;

                sportEntity = sportResult.Value;
                sportRepository.Add(sportEntity);
                sportsByProvider[(sportCommandDto.ProviderName, sportCommandDto.ProviderId)] = sportEntity;
            }

            foreach (var leagueCommandDto in sportCommandDto.Leagues)
            {
                var leagueEntity = ResolveExistingLeague(leagueCommandDto, leaguesById, leaguesByProvider);

                if (leagueCommandDto.Id is not null && leagueEntity is null)
                    return Result.Failure(DomainErrors.League.NotFound(leagueCommandDto.Id));

                if (leagueEntity is not null)
                {
                    if (!MatchesProviderIdentity(
                            leagueEntity.ExternalProviderName,
                            leagueEntity.ExternalProviderId,
                            leagueCommandDto.ProviderName,
                            leagueCommandDto.ProviderId))
                    {
                        return Result.Failure(Errors.Catalog.IdentityMismatch);
                    }

                    var statusResult = leagueEntity.ChangeMonitoringStatus(leagueCommandDto.IsMonitored);
                    if (statusResult.IsFailure)
                        return statusResult;
                }
                else if (leagueCommandDto.IsMonitored)
                {
                    if (cachedLeaguesDict is null
                        || !cachedLeaguesDict.TryGetValue(
                            (leagueCommandDto.ProviderName, leagueCommandDto.ProviderId),
                            out var cachedLeague))
                    {
                        return Result.Failure(Errors.Catalog.ItemNotFoundInCache);
                    }

                    var leagueResult = League.CreateFromProvider(
                        sportEntity.Id,
                        cachedLeague.Name,
                        cachedLeague.Country,
                        leagueCommandDto.ProviderName,
                        cachedLeague.ProviderId);

                    if (leagueResult.IsFailure)
                        return leagueResult;

                    leagueRepository.Add(leagueResult.Value);
                    leaguesByProvider[(leagueCommandDto.ProviderName, leagueCommandDto.ProviderId)] = leagueResult.Value;
                }
            }
        }

        return Result.Success();
    }

    private static Sport? ResolveExistingSport(
        UpdateMonitoringSportDto sportDto,
        IReadOnlyDictionary<SportId, Sport> sportsById,
        IReadOnlyDictionary<(string ProviderName, string ProviderId), Sport> sportsByProvider)
    {
        if (sportDto.Id is not null && sportsById.TryGetValue(sportDto.Id, out var byId))
            return byId;

        if (sportsByProvider.TryGetValue((sportDto.ProviderName, sportDto.ProviderId), out var byProvider))
            return byProvider;

        return null;
    }

    private static League? ResolveExistingLeague(
        UpdateMonitoringLeagueDto leagueDto,
        IReadOnlyDictionary<LeagueId, League> leaguesById,
        IReadOnlyDictionary<(string ProviderName, string ProviderId), League> leaguesByProvider)
    {
        if (leagueDto.Id is not null && leaguesById.TryGetValue(leagueDto.Id, out var byId))
            return byId;

        if (leaguesByProvider.TryGetValue((leagueDto.ProviderName, leagueDto.ProviderId), out var byProvider))
            return byProvider;

        return null;
    }

    private static bool MatchesProviderIdentity(
        string? entityProviderName,
        string? entityProviderId,
        string requestProviderName,
        string requestProviderId) =>
        string.Equals(entityProviderName, requestProviderName, StringComparison.Ordinal)
        && string.Equals(entityProviderId, requestProviderId, StringComparison.Ordinal);
}

using Sporeo.BuildingBlocks.Application.Abstractions.Execution;
using Sporeo.BuildingBlocks.Domain.Results;
using Sporeo.Fixtures.Application.Seasons.Abstractions.Repositories;
using Sporeo.Fixtures.Domain.Leagues.ValueObjects;
using Sporeo.Fixtures.Domain.Seasons;

namespace Sporeo.Fixtures.Application.Seasons.Commands.EnsureSeasonsForSync;

internal sealed class EnsureSeasonsForSyncCommandHandler(
    ISeasonRepository seasonRepository)
    : ICommandHandler<EnsureSeasonsForSyncCommand, IReadOnlyDictionary<string, Guid>>
{
    public async Task<Result<IReadOnlyDictionary<string, Guid>>> Handle(
        EnsureSeasonsForSyncCommand request,
        CancellationToken cancellationToken)
    {
        var leagueId = LeagueId.FromValue(request.LeagueId);
        var existingSeasons = (await seasonRepository.GetByLeagueIdAsync(leagueId, cancellationToken)).ToList();
        var seasonMap = existingSeasons.ToDictionary(season => season.Name, season => season.Id.Value);

        var missingSeasonNames = request.SeasonNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Except(seasonMap.Keys)
            .ToList();

        foreach (var seasonName in missingSeasonNames)
        {
            var createResult = Season.Create(leagueId, seasonName);
            if (createResult.IsFailure)
                return Result.Failure<IReadOnlyDictionary<string, Guid>>(createResult.Error);

            var newSeason = createResult.Value;
            seasonRepository.Add(newSeason);
            existingSeasons.Add(newSeason);
            seasonMap[seasonName] = newSeason.Id.Value;
        }

        if (request.NextFixtureDate.HasValue && !string.IsNullOrWhiteSpace(request.NextFixtureSeasonName))
        {
            var activeSeason = existingSeasons.FirstOrDefault(season => season.Name == request.NextFixtureSeasonName);

            if (activeSeason is not null && !activeSeason.IsCurrent)
            {
                foreach (var previousSeason in existingSeasons.Where(season => season.IsCurrent && season.Id != activeSeason.Id))
                {
                    previousSeason.UnmarkAsCurrent();
                }

                activeSeason.MarkAsCurrent();
            }
        }

        return Result.Success<IReadOnlyDictionary<string, Guid>>(seasonMap);
    }
}

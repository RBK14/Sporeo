using Sporeo.BuildingBlocks.Application.Abstractions.Execution;
using Sporeo.BuildingBlocks.Domain.Results;
using Sporeo.Fixtures.Application.Leagues.Data;
using Sporeo.Fixtures.Application.Seasons.Data;
using Sporeo.Fixtures.Domain.Common;
using Sporeo.Fixtures.Domain.Seasons;
using Sporeo.Fixtures.Domain.Seasons.ValueObjects;

namespace Sporeo.Fixtures.Application.Seasons.Commands.CreateSeason;

internal sealed class CreateSeasonCommandHandler(
    ISeasonRepository seasonRepository,
    ILeagueRepository leagueRepository) : ICommandHandler<CreateSeasonCommand, SeasonId>
{
    public async Task<Result<SeasonId>> Handle(CreateSeasonCommand request, CancellationToken cancellationToken)
    {
        var league = await leagueRepository.GetByIdAsync(request.LeagueId, cancellationToken);
        if (league is null)
            return Result.Failure<SeasonId>(Errors.League.NotFound(request.LeagueId));

        var seasonResult = Season.Create(
            request.LeagueId,
            request.Name);

        if (seasonResult.IsFailure)
            return Result.Failure<SeasonId>(seasonResult.Error);

        var season = seasonResult.Value;
        seasonRepository.Add(season);

        return Result.Success(season.Id);
    }
}

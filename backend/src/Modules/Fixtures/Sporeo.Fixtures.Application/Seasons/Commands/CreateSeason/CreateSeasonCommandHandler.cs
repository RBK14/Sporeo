using Sporeo.BuildingBlocks.Application.Abstractions.Execution;
using Sporeo.BuildingBlocks.Domain.Results;
using Sporeo.Fixtures.Application.Seasons.Abstractions.Repositories;
using Sporeo.Fixtures.Domain.Seasons;
using Sporeo.Fixtures.Domain.Seasons.ValueObjects;

namespace Sporeo.Fixtures.Application.Seasons.Commands.CreateSeason;

internal sealed class CreateSeasonCommandHandler(
    ISeasonRepository seasonRepository) : ICommandHandler<CreateSeasonCommand, SeasonId>
{
    public async Task<Result<SeasonId>> Handle(CreateSeasonCommand request, CancellationToken cancellationToken)
    {
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

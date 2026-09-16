using Sporeo.BuildingBlocks.Application.Abstractions.Data;
using Sporeo.BuildingBlocks.Application.Abstractions.Execution;
using Sporeo.BuildingBlocks.Domain.Results;
using Sporeo.Fixtures.Application.Abstractions.Repositories;
using Sporeo.Fixtures.Domain.Leagues.ValueObjects;
using Sporeo.Fixtures.Domain.Seasons;

namespace Sporeo.Fixtures.Application.Seasons.Commands.CreateSeason;

internal sealed class CreateSeasonCommandHandler(
    ISeasonRepository seasonRepository) : ICommandHandler<CreateSeasonCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateSeasonCommand request, CancellationToken cancellationToken)
    {
        var leagueId = LeagueId.FromValue(request.LeagueId);

        var seasonResult = Season.CreateManually(
            leagueId,
            request.Name,
            request.StartDate,
            request.EndDate);

        if (seasonResult.IsFailure)
            return Result.Failure<Guid>(seasonResult.Error);

        var season = seasonResult.Value;
        seasonRepository.Add(season);

        return Result.Success(season.Id.Value);
    }
}

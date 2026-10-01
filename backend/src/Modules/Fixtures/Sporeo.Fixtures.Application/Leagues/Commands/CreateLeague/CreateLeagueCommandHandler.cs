using Sporeo.BuildingBlocks.Application.Abstractions.Execution;
using Sporeo.BuildingBlocks.Domain.Results;
using Sporeo.Fixtures.Application.Leagues.Data;
using Sporeo.Fixtures.Application.Sports.Data;
using Sporeo.Fixtures.Domain.Common;
using Sporeo.Fixtures.Domain.Leagues;
using Sporeo.Fixtures.Domain.Leagues.ValueObjects;

namespace Sporeo.Fixtures.Application.Leagues.Commands.CreateLeague;

internal sealed class CreateLeagueCommandHandler(
    ILeagueRepository leagueRepository,
    ISportRepository sportRepository) : ICommandHandler<CreateLeagueCommand, LeagueId>
{
    public async Task<Result<LeagueId>> Handle(CreateLeagueCommand request, CancellationToken cancellationToken)
    {
        var sport = await sportRepository.GetByIdAsync(request.SportId, cancellationToken);
        if (sport is null)
            return Result.Failure<LeagueId>(Errors.Sport.NotFound(request.SportId));

        var leagueResult = League.CreateManually(request.SportId, request.Name, request.Country);

        if (leagueResult.IsFailure)
            return Result.Failure<LeagueId>(leagueResult.Error);

        var league = leagueResult.Value;
        leagueRepository.Add(league);

        return Result.Success(league.Id);
    }
}

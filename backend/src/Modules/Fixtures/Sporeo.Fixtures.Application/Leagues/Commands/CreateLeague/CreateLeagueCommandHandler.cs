using Sporeo.BuildingBlocks.Application.Abstractions.Execution;
using Sporeo.BuildingBlocks.Domain.Results;
using Sporeo.Fixtures.Application.Leagues.Data;
using Sporeo.Fixtures.Domain.Leagues;
using Sporeo.Fixtures.Domain.Leagues.ValueObjects;

namespace Sporeo.Fixtures.Application.Leagues.Commands.CreateLeague;

internal sealed class CreateLeagueCommandHandler(
    ILeagueRepository leagueRepository) : ICommandHandler<CreateLeagueCommand, LeagueId>
{
    public async Task<Result<LeagueId>> Handle(CreateLeagueCommand request, CancellationToken cancellationToken)
    {
        var leagueResult = League.CreateManually(request.SportId, request.Name, request.Country);

        if (leagueResult.IsFailure)
            return Result.Failure<LeagueId>(leagueResult.Error);

        var league = leagueResult.Value;
        leagueRepository.Add(league);

        return Result.Success(league.Id);
    }
}

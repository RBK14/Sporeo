using Sporeo.BuildingBlocks.Application.Abstractions.Data;
using Sporeo.BuildingBlocks.Application.Abstractions.Execution;
using Sporeo.BuildingBlocks.Domain.Results;
using Sporeo.Fixtures.Application.Leagues.Abstractions.Repositories;
using Sporeo.Fixtures.Domain.Leagues;
using Sporeo.Fixtures.Domain.Sports.ValueObjects;

namespace Sporeo.Fixtures.Application.Leagues.Commands.CreateLeague;

internal sealed class CreateLeagueCommandHandler(
    ILeagueRepository leagueRepository) : ICommandHandler<CreateLeagueCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateLeagueCommand request, CancellationToken cancellationToken)
    {
        var sportId = SportId.FromValue(request.SportId);

        var leagueResult = League.CreateManually(sportId, request.Name, request.Country);

        if (leagueResult.IsFailure)
            return Result.Failure<Guid>(leagueResult.Error);

        var league = leagueResult.Value;
        leagueRepository.Add(league);

        return Result.Success(league.Id.Value);
    }
}

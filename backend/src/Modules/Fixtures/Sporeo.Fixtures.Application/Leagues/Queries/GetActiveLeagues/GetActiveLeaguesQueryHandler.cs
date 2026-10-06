using Sporeo.BuildingBlocks.Application.Abstractions.Execution;
using Sporeo.BuildingBlocks.Domain.Results;
using Sporeo.Fixtures.Application.Leagues.Data;

namespace Sporeo.Fixtures.Application.Leagues.Queries.GetActiveLeagues;

internal sealed class GetActiveLeaguesQueryHandler(ILeagueReadStore readStore)
    : IQueryHandler<GetActiveLeaguesQuery, IReadOnlyList<ActiveLeagueReadModel>>
{
    public async Task<Result<IReadOnlyList<ActiveLeagueReadModel>>> Handle(
        GetActiveLeaguesQuery request,
        CancellationToken cancellationToken)
    {
        var leagues = await readStore.GetActiveLeaguesAsync(request.SportId, cancellationToken);
        return Result.Success(leagues);
    }
}

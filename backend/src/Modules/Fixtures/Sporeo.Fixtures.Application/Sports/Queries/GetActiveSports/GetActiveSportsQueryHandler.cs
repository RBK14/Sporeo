using Sporeo.BuildingBlocks.Application.Abstractions.Execution;
using Sporeo.BuildingBlocks.Domain.Results;
using Sporeo.Fixtures.Application.Sports.Data;

namespace Sporeo.Fixtures.Application.Sports.Queries.GetActiveSports;

internal sealed class GetActiveSportsQueryHandler(ISportReadStore readStore)
    : IQueryHandler<GetActiveSportsQuery, IReadOnlyList<ActiveSportReadModel>>
{
    public async Task<Result<IReadOnlyList<ActiveSportReadModel>>> Handle(
        GetActiveSportsQuery request,
        CancellationToken cancellationToken)
    {
        var sports = await readStore.GetActiveSportsAsync(cancellationToken);
        return Result.Success(sports);
    }
}

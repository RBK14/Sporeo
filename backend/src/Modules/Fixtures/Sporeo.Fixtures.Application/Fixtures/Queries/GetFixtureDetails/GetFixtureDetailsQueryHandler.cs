using Sporeo.BuildingBlocks.Application.Abstractions.Execution;
using Sporeo.BuildingBlocks.Domain.Results;
using Sporeo.Fixtures.Application.Fixtures.Data;
using Sporeo.Fixtures.Domain.Common;

namespace Sporeo.Fixtures.Application.Fixtures.Queries.GetFixtureDetails;

internal sealed class GetFixtureDetailsQueryHandler(IFixtureReadStore readStore)
    : IQueryHandler<GetFixtureDetailsQuery, FixtureDetailsReadModel>
{
    public async Task<Result<FixtureDetailsReadModel>> Handle(
        GetFixtureDetailsQuery request,
        CancellationToken cancellationToken)
    {
        var details = await readStore.GetFixtureDetailsAsync(request.FixtureId, cancellationToken);
        return details is null
            ? Result.Failure<FixtureDetailsReadModel>(Errors.Fixture.NotFound(request.FixtureId))
            : Result.Success(details);
    }
}

using Sporeo.BuildingBlocks.Application.Abstractions.Execution;
using Sporeo.BuildingBlocks.Domain.Results;
using Sporeo.Fixtures.Application.Fixtures.Abstractions.ReadModels;
using Sporeo.Fixtures.Domain.Common;

namespace Sporeo.Fixtures.Application.Fixtures.Queries.GetFixtureDetails;

internal sealed class GetFixtureDetailsQueryHandler(IFixtureReadStore readStore)
    : IQueryHandler<GetFixtureDetailsQuery, FixtureDetailsResponse>
{
    public async Task<Result<FixtureDetailsResponse>> Handle(
        GetFixtureDetailsQuery request,
        CancellationToken cancellationToken)
    {
        var details = await readStore.GetFixtureDetailsAsync(request.FixtureId, cancellationToken);
        return details is null
            ? Result.Failure<FixtureDetailsResponse>(Errors.Fixture.NotFound(request.FixtureId))
            : Result.Success(details);
    }
}

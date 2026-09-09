using Sporeo.BuildingBlocks.Application.Abstractions.Execution;

namespace Sporeo.Fixtures.Application.Fixtures.Queries.GetFixtureDetails;

public sealed record GetFixtureDetailsQuery(Guid FixtureId) : IQuery<FixtureDetailsResponse>
{
}

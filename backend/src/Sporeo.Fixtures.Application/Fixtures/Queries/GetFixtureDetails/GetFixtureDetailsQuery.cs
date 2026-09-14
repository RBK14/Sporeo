using Sporeo.BuildingBlocks.Application.Abstractions.Execution;

namespace Sporeo.Fixtures.Application.Fixtures.Queries.GetFixtureDetails;

/// <summary>
/// Query that retrieves detailed information for a single fixture.
/// </summary>
/// <param name="FixtureId">The identifier of the fixture to retrieve.</param>
public sealed record GetFixtureDetailsQuery(Guid FixtureId) : IQuery<FixtureDetailsResponse>;

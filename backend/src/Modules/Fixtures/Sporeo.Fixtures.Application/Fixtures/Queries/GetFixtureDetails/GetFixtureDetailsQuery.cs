using Sporeo.BuildingBlocks.Application.Abstractions.Execution;
using Sporeo.Fixtures.Domain.Fixtures.ValueObjects;

namespace Sporeo.Fixtures.Application.Fixtures.Queries.GetFixtureDetails;

/// <summary>
/// Query that retrieves detailed information for a single fixture.
/// </summary>
/// <param name="FixtureId">The identifier of the fixture to retrieve.</param>
public sealed record GetFixtureDetailsQuery(FixtureId FixtureId) : IQuery<FixtureDetailsReadModel>;

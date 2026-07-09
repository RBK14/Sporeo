using Sporeo.BuildingBlocks.Domain.Results;
using Sporeo.BuildingBlocks.Domain.Rules;
using Sporeo.Fixtures.Domain.Common;
using Sporeo.Fixtures.Domain.Fixtures.Enums;

namespace Sporeo.Fixtures.Domain.Fixtures.Rules;

/// <summary>
/// Ensures that a finished fixture cannot be modified.
/// </summary>
/// <param name="fixture">The fixture to evaluate.</param>
public sealed class FinishedFixtureIsImmutableRule(Fixture fixture) : IBusinessRule
{
    /// <inheritdoc />
    public bool IsBroken() => fixture.Status == FixtureStatus.Finished;

    /// <inheritdoc />
    public Error Error => Errors.Fixture.Finished;
}

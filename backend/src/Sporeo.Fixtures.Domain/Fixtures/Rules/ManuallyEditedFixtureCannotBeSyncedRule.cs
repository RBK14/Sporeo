using Sporeo.BuildingBlocks.Domain.Results;
using Sporeo.BuildingBlocks.Domain.Rules;
using Sporeo.Fixtures.Domain.Common;

namespace Sporeo.Fixtures.Domain.Fixtures.Rules;

/// <summary>
/// Ensures that a manually edited fixture cannot be overwritten by external synchronization.
/// </summary>
/// <param name="fixture">The fixture to evaluate.</param>
public sealed class ManuallyEditedFixtureCannotBeSyncedRule(Fixture fixture) : IBusinessRule
{
    /// <inheritdoc />
    public bool IsBroken() => fixture.IsManuallyEdited;

    /// <inheritdoc />
    public Error Error => Errors.Fixture.LockedForSync;
}

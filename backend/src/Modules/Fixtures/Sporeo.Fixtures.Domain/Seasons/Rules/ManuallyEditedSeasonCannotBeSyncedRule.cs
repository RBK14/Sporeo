using Sporeo.BuildingBlocks.Domain.Results;
using Sporeo.BuildingBlocks.Domain.Rules;
using Sporeo.Fixtures.Domain.Common;

namespace Sporeo.Fixtures.Domain.Seasons.Rules;

/// <summary>
/// Ensures that a manually edited season cannot be overwritten by external synchronization.
/// </summary>
/// <param name="season">The season to evaluate.</param>
public sealed class ManuallyEditedSeasonCannotBeSyncedRule(Season season) : IBusinessRule
{
    /// <inheritdoc />
    public bool IsBroken() => season.IsManuallyEdited;

    /// <inheritdoc />
    public Error Error => Errors.Season.LockedForSync;
}

using Sporeo.BuildingBlocks.Domain.Results;
using Sporeo.BuildingBlocks.Domain.Rules;
using Sporeo.Fixtures.Domain.Common;

namespace Sporeo.Fixtures.Domain.Leagues.Rules;

/// <summary>
/// Ensures that a manually edited league cannot be overwritten by external synchronization.
/// </summary>
/// <param name="league">The league to evaluate.</param>
public sealed class ManuallyEditedLeagueCannotBeSyncedRule(League league) : IBusinessRule
{
    /// <inheritdoc />
    public bool IsBroken() => league.IsManuallyEdited;

    /// <inheritdoc />
    public Error Error => Errors.League.LockedForSync;
}

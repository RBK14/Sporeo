using Sporeo.BuildingBlocks.Domain.Results;
using Sporeo.BuildingBlocks.Domain.Rules;
using Sporeo.Fixtures.Domain.Common;

namespace Sporeo.Fixtures.Domain.Venues.Rules;

/// <summary>
/// Ensures that a manually edited venue cannot be overwritten by external synchronization.
/// </summary>
/// <param name="venue">The venue to evaluate.</param>
public sealed class ManuallyEditedVenueCannotBeSyncedRule(Venue venue) : IBusinessRule
{
    /// <inheritdoc />
    public bool IsBroken() => venue.IsManuallyEdited;

    /// <inheritdoc />
    public Error Error => Errors.Venue.LockedForSync;
}

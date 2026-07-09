using Sporeo.BuildingBlocks.Domain.Results;
using Sporeo.BuildingBlocks.Domain.Rules;
using Sporeo.Fixtures.Domain.Common;
using Sporeo.Fixtures.Domain.Fixtures.Enums;

namespace Sporeo.Fixtures.Domain.Fixtures.Rules;

/// <summary>
/// Ensures that a fixture status transition follows the allowed lifecycle rules.
/// </summary>
/// <param name="currentStatus">The current fixture status.</param>
/// <param name="newStatus">The requested fixture status.</param>
public sealed class InvalidFixtureStatusTransitionRule(
    FixtureStatus currentStatus,
    FixtureStatus newStatus) : IBusinessRule
{
    /// <inheritdoc />
    public bool IsBroken() => !IsValidTransition(currentStatus, newStatus);

    /// <inheritdoc />
    public Error Error => Errors.Fixture.InvalidStatusTransition;

    private static bool IsValidTransition(FixtureStatus currentStatus, FixtureStatus newStatus)
    {
        if (currentStatus == newStatus)
            return true;

        return currentStatus switch
        {
            FixtureStatus.Scheduled => newStatus is FixtureStatus.Postponed or FixtureStatus.Cancelled or FixtureStatus.Finished,
            FixtureStatus.Postponed => newStatus is FixtureStatus.Scheduled or FixtureStatus.Cancelled or FixtureStatus.Finished,
            FixtureStatus.Cancelled => false,
            FixtureStatus.Finished => false,
            _ => false
        };
    }
}

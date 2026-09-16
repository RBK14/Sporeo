namespace Sporeo.Fixtures.Domain.Fixtures.Enums;

/// <summary>
/// Represents the lifecycle status of a fixture.
/// </summary>
public enum FixtureStatus
{
    /// <summary>
    /// The fixture is scheduled to take place.
    /// </summary>
    Scheduled,

    /// <summary>
    /// The fixture has been postponed to a later date.
    /// </summary>
    Postponed,

    /// <summary>
    /// The fixture has been cancelled and will not take place.
    /// </summary>
    Cancelled,

    /// <summary>
    /// The fixture has concluded and is immutable.
    /// </summary>
    Finished
}

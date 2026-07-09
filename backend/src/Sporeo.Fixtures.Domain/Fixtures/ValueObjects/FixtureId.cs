using Sporeo.BuildingBlocks.Domain.Models;

namespace Sporeo.Fixtures.Domain.Fixtures.ValueObjects;

/// <summary>
/// Strongly typed identifier for a <see cref="Fixture"/> aggregate.
/// </summary>
public sealed record FixtureId : TypedIdBase
{
    private FixtureId(Guid value) : base(value)
    {
    }

    /// <summary>
    /// Creates a new random <see cref="FixtureId"/>.
    /// </summary>
    /// <returns>A new identifier.</returns>
    public static FixtureId New() => new(Guid.NewGuid());

    /// <summary>
    /// Creates a <see cref="FixtureId"/> from an existing <see cref="Guid"/> value.
    /// </summary>
    /// <param name="value">The underlying identifier value. Must not be <see cref="Guid.Empty"/>.</param>
    /// <returns>A <see cref="FixtureId"/> wrapping the specified value.</returns>
    public static FixtureId FromValue(Guid value) => new(value);
}

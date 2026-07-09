using Sporeo.BuildingBlocks.Domain.Models;

namespace Sporeo.Fixtures.Domain.Seasons.ValueObjects;

/// <summary>
/// Strongly typed identifier for a <see cref="Season"/> aggregate.
/// </summary>
public sealed record SeasonId : TypedIdBase
{
    private SeasonId(Guid value) : base(value)
    {
    }

    /// <summary>
    /// Creates a new random <see cref="SeasonId"/>.
    /// </summary>
    /// <returns>A new identifier.</returns>
    public static SeasonId New() => new(Guid.NewGuid());

    /// <summary>
    /// Creates a <see cref="SeasonId"/> from an existing <see cref="Guid"/> value.
    /// </summary>
    /// <param name="value">The underlying identifier value. Must not be <see cref="Guid.Empty"/>.</param>
    /// <returns>A <see cref="SeasonId"/> wrapping the specified value.</returns>
    public static SeasonId FromValue(Guid value) => new(value);
}

using Sporeo.BuildingBlocks.Domain.Models;

namespace Sporeo.Fixtures.Domain.Sports.ValueObjects;

/// <summary>
/// Strongly typed identifier for a <see cref="Sport"/> aggregate.
/// </summary>
public sealed record SportId : TypedIdBase
{
    private SportId(Guid value) : base(value)
    {
    }

    /// <summary>
    /// Creates a new random <see cref="SportId"/>.
    /// </summary>
    /// <returns>A new identifier.</returns>
    public static SportId New() => new(Guid.NewGuid());

    /// <summary>
    /// Creates a <see cref="SportId"/> from an existing <see cref="Guid"/> value.
    /// </summary>
    /// <param name="value">The underlying identifier value. Must not be <see cref="Guid.Empty"/>.</param>
    /// <returns>A <see cref="SportId"/> wrapping the specified value.</returns>
    public static SportId FromValue(Guid value) => new(value);
}

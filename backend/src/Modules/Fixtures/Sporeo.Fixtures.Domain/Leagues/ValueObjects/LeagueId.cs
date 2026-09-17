using Sporeo.BuildingBlocks.Domain.Models;

namespace Sporeo.Fixtures.Domain.Leagues.ValueObjects;

/// <summary>
/// Strongly typed identifier for a <see cref="League"/> aggregate.
/// </summary>
public sealed record LeagueId : TypedIdBase
{
    private LeagueId(Guid value) : base(value)
    {
    }

    /// <summary>
    /// Creates a new random <see cref="LeagueId"/>.
    /// </summary>
    /// <returns>A new identifier.</returns>
    public static LeagueId New() => new(Guid.NewGuid());

    /// <summary>
    /// Creates a <see cref="LeagueId"/> from an existing <see cref="Guid"/> value.
    /// </summary>
    /// <param name="value">The underlying identifier value. Must not be <see cref="Guid.Empty"/>.</param>
    /// <returns>A <see cref="LeagueId"/> wrapping the specified value.</returns>
    public static LeagueId FromValue(Guid value) => new(value);
}

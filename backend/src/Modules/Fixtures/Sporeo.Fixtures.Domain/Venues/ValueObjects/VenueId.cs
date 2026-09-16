using Sporeo.BuildingBlocks.Domain.Models;

namespace Sporeo.Fixtures.Domain.Venues.ValueObjects;

/// <summary>
/// Strongly typed identifier for a <see cref="Venue"/> aggregate.
/// </summary>
public sealed record VenueId : TypedIdBase
{
    private VenueId(Guid value) : base(value)
    {
    }

    /// <summary>
    /// Creates a new random <see cref="VenueId"/>.
    /// </summary>
    /// <returns>A new identifier.</returns>
    public static VenueId New() => new(Guid.NewGuid());

    /// <summary>
    /// Creates a <see cref="VenueId"/> from an existing <see cref="Guid"/> value.
    /// </summary>
    /// <param name="value">The underlying identifier value. Must not be <see cref="Guid.Empty"/>.</param>
    /// <returns>A <see cref="VenueId"/> wrapping the specified value.</returns>
    public static VenueId FromValue(Guid value) => new(value);
}

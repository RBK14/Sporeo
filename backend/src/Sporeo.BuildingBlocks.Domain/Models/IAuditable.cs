namespace Sporeo.BuildingBlocks.Domain.Models;

/// <summary>
/// Marks an entity whose creation and modification timestamps are tracked.
/// </summary>
public interface IAuditable
{
    /// <summary>
    /// Gets the UTC timestamp when the entity was created.
    /// </summary>
    DateTimeOffset CreatedOn { get; }

    /// <summary>
    /// Gets the UTC timestamp when the entity was last modified, or <see langword="null"/> if it has never been modified.
    /// </summary>
    DateTimeOffset? ModifiedOn { get; }
}

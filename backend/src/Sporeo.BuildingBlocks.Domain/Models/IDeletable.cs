namespace Sporeo.BuildingBlocks.Domain.Models;

/// <summary>
/// Marks an entity that supports soft deletion.
/// </summary>
public interface IDeletable
{
    /// <summary>
    /// Gets a value indicating whether the entity has been soft-deleted.
    /// </summary>
    bool IsDeleted { get; }

    /// <summary>
    /// Gets the UTC timestamp when the entity was soft-deleted, or <see langword="null"/> if it has not been deleted.
    /// </summary>
    DateTimeOffset? DeletedOn { get; }
}

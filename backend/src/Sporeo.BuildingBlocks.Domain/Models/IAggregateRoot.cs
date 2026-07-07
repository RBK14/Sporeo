namespace Sporeo.BuildingBlocks.Domain.Models;

/// <summary>
/// Marks an entity as an aggregate root in the domain model.
/// Aggregate roots are the only entities through which external code may modify the aggregate.
/// </summary>
public interface IAggregateRoot
{
}

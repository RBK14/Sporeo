namespace Sporeo.BuildingBlocks.Application.Abstractions.Data;

/// <summary>
/// Defines the unit of work boundary for persisting domain changes atomically.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Persists all pending changes to the underlying data store.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The number of state entries written to the database.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

using Sporeo.Fixtures.Domain.Sports;
using Sporeo.Fixtures.Domain.Sports.ValueObjects;

namespace Sporeo.Fixtures.Application.Sports.Abstractions.Repositories;

/// <summary>
/// Persistence port for <see cref="Sport"/> aggregates.
/// Soft-deleted sports are excluded by the persistence layer; deletion is performed via <see cref="Sport.Delete"/>.
/// </summary>
public interface ISportRepository
{
    /// <summary>
    /// Gets a sport by its identifier.
    /// </summary>
    /// <param name="id">The sport identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The sport when found; otherwise, <see langword="null"/>.</returns>
    Task<Sport?> GetByIdAsync(SportId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a sport by its external provider identity.
    /// </summary>
    /// <param name="providerName">The external provider name.</param>
    /// <param name="providerId">The identifier assigned by the external provider.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The sport when found; otherwise, <see langword="null"/>.</returns>
    Task<Sport?> GetByExternalProviderAsync(
        string providerName,
        string providerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets sports matching the given external provider name and provider identifiers.
    /// </summary>
    /// <param name="providerName">The external provider name.</param>
    /// <param name="providerIds">The identifiers assigned by the external provider.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The matching sports. Missing identifiers are omitted.</returns>
    Task<IReadOnlyList<Sport>> GetByExternalProviderIdsAsync(
        string providerName,
        IEnumerable<string> providerIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a new sport for persistence.
    /// </summary>
    /// <param name="sport">The sport to add.</param>
    void Add(Sport sport);
}

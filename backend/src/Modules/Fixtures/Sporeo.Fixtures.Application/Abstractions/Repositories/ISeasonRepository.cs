using Sporeo.Fixtures.Domain.Seasons;
using Sporeo.Fixtures.Domain.Seasons.ValueObjects;

namespace Sporeo.Fixtures.Application.Abstractions.Repositories;

/// <summary>
/// Persistence port for <see cref="Season"/> aggregates.
/// Soft-deleted seasons are excluded by the persistence layer; deletion is performed via <see cref="Season.Delete"/>.
/// </summary>
public interface ISeasonRepository
{
    /// <summary>
    /// Gets a season by its identifier.
    /// </summary>
    /// <param name="id">The season identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The season when found; otherwise, <see langword="null"/>.</returns>
    Task<Season?> GetByIdAsync(SeasonId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a season by its external provider identity.
    /// </summary>
    /// <param name="providerName">The external provider name.</param>
    /// <param name="providerId">The identifier assigned by the external provider.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The season when found; otherwise, <see langword="null"/>.</returns>
    Task<Season?> GetByExternalProviderAsync(
        string providerName,
        string providerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a new season for persistence.
    /// </summary>
    /// <param name="season">The season to add.</param>
    void Add(Season season);
}

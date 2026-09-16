using Sporeo.Fixtures.Domain.Venues;
using Sporeo.Fixtures.Domain.Venues.ValueObjects;

namespace Sporeo.Fixtures.Application.Abstractions.Repositories;

/// <summary>
/// Persistence port for <see cref="Venue"/> aggregates.
/// Soft-deleted venues are excluded by the persistence layer; deletion is performed via <see cref="Venue.Delete"/>.
/// </summary>
public interface IVenueRepository
{
    /// <summary>
    /// Gets a venue by its identifier.
    /// </summary>
    /// <param name="id">The venue identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The venue when found; otherwise, <see langword="null"/>.</returns>
    Task<Venue?> GetByIdAsync(VenueId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a venue by its external provider identity.
    /// </summary>
    /// <param name="providerName">The external provider name.</param>
    /// <param name="providerId">The identifier assigned by the external provider.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The venue when found; otherwise, <see langword="null"/>.</returns>
    Task<Venue?> GetByExternalProviderAsync(
        string providerName,
        string providerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets venues matching the given external provider name and provider identifiers.
    /// </summary>
    /// <param name="providerName">The external provider name.</param>
    /// <param name="providerIds">The identifiers assigned by the external provider.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The matching venues. Missing identifiers are omitted.</returns>
    Task<IReadOnlyList<Venue>> GetByExternalProviderIdsAsync(
        string providerName,
        IEnumerable<string> providerIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a new venue for persistence.
    /// </summary>
    /// <param name="venue">The venue to add.</param>
    void Add(Venue venue);
}

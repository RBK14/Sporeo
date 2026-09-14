using Sporeo.Fixtures.Domain.Fixtures.ValueObjects;

namespace Sporeo.Fixtures.Domain.Fixtures;

/// <summary>
/// Persistence port for <see cref="Fixture"/> aggregates.
/// Soft-deleted fixtures are excluded by the persistence layer; deletion is performed via <see cref="Fixture.Delete"/>.
/// </summary>
public interface IFixtureRepository
{
    /// <summary>
    /// Gets a fixture by its identifier.
    /// </summary>
    /// <param name="id">The fixture identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The fixture when found; otherwise, <see langword="null"/>.</returns>
    Task<Fixture?> GetByIdAsync(FixtureId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a fixture by its external provider identity.
    /// </summary>
    /// <param name="providerName">The external provider name.</param>
    /// <param name="providerId">The identifier assigned by the external provider.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The fixture when found; otherwise, <see langword="null"/>.</returns>
    Task<Fixture?> GetByExternalProviderAsync(
        string providerName,
        string providerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets fixtures matching the given external provider name and provider identifiers.
    /// </summary>
    /// <param name="providerName">The external provider name.</param>
    /// <param name="providerIds">The identifiers assigned by the external provider.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The matching fixtures. Missing identifiers are omitted.</returns>
    Task<IReadOnlyList<Fixture>> GetByExternalProviderIdsAsync(
        string providerName,
        IEnumerable<string> providerIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a new fixture for persistence.
    /// </summary>
    /// <param name="fixture">The fixture to add.</param>
    void Add(Fixture fixture);
}

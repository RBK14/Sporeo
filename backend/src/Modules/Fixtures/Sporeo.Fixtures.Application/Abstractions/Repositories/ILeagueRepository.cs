using Sporeo.Fixtures.Domain.Leagues;
using Sporeo.Fixtures.Domain.Leagues.ValueObjects;

namespace Sporeo.Fixtures.Application.Abstractions.Repositories;

/// <summary>
/// Persistence port for <see cref="League"/> aggregates.
/// Soft-deleted leagues are excluded by the persistence layer; deletion is performed via <see cref="League.Delete"/>.
/// </summary>
public interface ILeagueRepository
{
    /// <summary>
    /// Gets a league by its identifier.
    /// </summary>
    /// <param name="id">The league identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The league when found; otherwise, <see langword="null"/>.</returns>
    Task<League?> GetByIdAsync(LeagueId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a league by its external provider identity.
    /// </summary>
    /// <param name="providerName">The external provider name.</param>
    /// <param name="providerId">The identifier assigned by the external provider.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The league when found; otherwise, <see langword="null"/>.</returns>
    Task<League?> GetByExternalProviderAsync(
        string providerName,
        string providerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a new league for persistence.
    /// </summary>
    /// <param name="league">The league to add.</param>
    void Add(League league);
}

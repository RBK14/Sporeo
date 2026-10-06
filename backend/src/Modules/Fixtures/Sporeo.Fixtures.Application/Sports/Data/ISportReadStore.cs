using Sporeo.Fixtures.Application.Sports.Queries.GetActiveSports;
using Sporeo.Fixtures.Domain.Sports.ValueObjects;

namespace Sporeo.Fixtures.Application.Sports.Data;

/// <summary>
/// Read-model port for sport catalog status lookups.
/// </summary>
public interface ISportReadStore
{
    /// <summary>
    /// Gets local identifiers for sports matching the given external provider identity.
    /// </summary>
    /// <param name="providerName">The external provider name.</param>
    /// <param name="sportProviderIds">The sport identifiers assigned by the external provider.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// A dictionary keyed by external provider sport id, mapping to the local sport id.
    /// Missing identifiers are omitted.
    /// </returns>
    Task<IReadOnlyDictionary<string, SportId>> GetSportStatusesAsync(
        string providerName,
        IEnumerable<string> sportProviderIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets sports that are not deleted and currently have at least one monitored league.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Active sports ordered by name.</returns>
    Task<IReadOnlyList<ActiveSportReadModel>> GetActiveSportsAsync(
        CancellationToken cancellationToken = default);
}

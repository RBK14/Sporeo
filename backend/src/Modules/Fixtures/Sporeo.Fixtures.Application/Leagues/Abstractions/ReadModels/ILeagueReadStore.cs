using Sporeo.Fixtures.Domain.Leagues.ValueObjects;

namespace Sporeo.Fixtures.Application.Leagues.Abstractions.ReadModels;

/// <summary>
/// Read-model port for league catalog status lookups.
/// </summary>
public interface ILeagueReadStore
{
    /// <summary>
    /// Gets monitoring status for leagues matching the given external provider identity.
    /// </summary>
    /// <param name="providerName">The external provider name.</param>
    /// <param name="leagueProviderIds">The league identifiers assigned by the external provider.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// A dictionary keyed by external provider league id, mapping to the local league id and monitored flag.
    /// Missing identifiers are omitted.
    /// </returns>
    Task<IReadOnlyDictionary<string, (LeagueId Id, bool IsMonitored)>> GetLeagueStatusesAsync(
        string providerName,
        IEnumerable<string> leagueProviderIds,
        CancellationToken cancellationToken = default);
}

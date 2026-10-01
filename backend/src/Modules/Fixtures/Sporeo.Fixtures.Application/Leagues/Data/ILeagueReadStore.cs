using Sporeo.Fixtures.Domain.Leagues.ValueObjects;

using Sporeo.Fixtures.Application.Leagues.ReadModels;

namespace Sporeo.Fixtures.Application.Leagues.Data;

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

    /// <summary>
    /// Gets a list of monitored leagues for which sync jobs should be dispatched.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A list of monitored leagues.</returns>
    Task<IReadOnlyList<MonitoredLeagueForSyncReadModel>> GetMonitoredLeaguesForSyncAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a list of monitored leagues with an current season.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A list of leagues with current seasons.</returns>
    Task<IReadOnlyList<LeagueWithCurrentSeasonReadModel>> GetMonitoredLeaguesWithCurrentSeasonAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets external provider identity for a league.
    /// </summary>
    /// <param name="leagueId">The local league identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Provider identity when found; otherwise <see langword="null"/>.</returns>
    Task<LeagueExternalProviderDataReadModel?> GetLeagueExternalProviderDataAsync(LeagueId leagueId, CancellationToken cancellationToken = default);
}

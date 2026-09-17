using Sporeo.Fixtures.Domain.Leagues.ValueObjects;

namespace Sporeo.Fixtures.Application.Leagues.Abstractions.ReadModels;

/// <summary>
/// Represents a read-only store for league data, providing methods to retrieve league statuses based on provider information.
/// </summary>
public interface ILeagueReadStore
{
    /// <summary>
    /// Retrieves the statuses of leagues based on the provided external provider name and a collection of league provider IDs.
    /// The result is a dictionary mapping each league provider ID to a tuple containing the league's unique identifier and its monitored status.
    /// </summary>
    /// <param name="providerName"></param>
    /// <param name="leagueProviderIds"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<IReadOnlyDictionary<string, (LeagueId Id, bool IsMonitored)>> GetLeagueStatusesAsync(
        string providerName,
        IEnumerable<string> leagueProviderIds,
        CancellationToken cancellationToken = default);
}

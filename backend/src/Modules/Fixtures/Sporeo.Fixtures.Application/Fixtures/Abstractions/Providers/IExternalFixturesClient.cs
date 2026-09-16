using Sporeo.BuildingBlocks.Domain.Results;

namespace Sporeo.Fixtures.Application.Fixtures.Abstractions.Providers;

/// <summary>
/// Port for fetching fixtures from an external sports data provider.
/// </summary>
public interface IExternalFixturesClient
{
    /// <summary>
    /// Gets the stable provider name used for identity and configuration matching.
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Fetches fixtures for the specified league and optional season.
    /// </summary>
    /// <param name="externalLeagueId">The provider league identifier.</param>
    /// <param name="externalSeasonId">The provider season identifier required for long-term sync.</param>
    /// <param name="syncMode">The synchronization mode.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// A successful result containing fixtures (possibly empty), or a typed failure for
    /// unauthorized, rate-limited, transient, or permanent provider errors.
    /// </returns>
    Task<Result<IReadOnlyList<ExternalFixtureDto>>> FetchFixturesAsync(
        string externalLeagueId,
        string? externalSeasonId,
        SyncMode syncMode,
        CancellationToken cancellationToken = default);
}

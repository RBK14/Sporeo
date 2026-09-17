using Sporeo.BuildingBlocks.Domain.Results;

namespace Sporeo.Fixtures.Application.Catalogs.Abstractions.Providers;

/// <summary>
/// Port for fetching the master catalog of sports and leagues from an external provider.
/// </summary>
public interface IExternalCatalogClient
{
    /// <summary>
    /// Gets the stable provider name used for identity and configuration matching.
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Fetches all available sports from the external provider.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A successful result containing the sports list, or a failure when the provider call fails.</returns>
    Task<Result<IReadOnlyList<ExternalSportDto>>> FetchSportsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches all available leagues from the external provider.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A successful result containing the leagues list, or a failure when the provider call fails.</returns>
    Task<Result<IReadOnlyList<ExternalLeagueDto>>> FetchLeaguesAsync(CancellationToken cancellationToken = default);
}

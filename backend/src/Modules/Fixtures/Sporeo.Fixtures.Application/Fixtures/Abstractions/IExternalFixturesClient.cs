using Sporeo.BuildingBlocks.Domain.Results;

namespace Sporeo.Fixtures.Application.Fixtures.Abstractions;

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
    /// Fetches near-term fixtures for the given external league identifier.
    /// </summary>
    /// <param name="externalLeagueId">The league identifier assigned by the external provider.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A successful result containing fixtures, or a failure when the provider call fails.</returns>
    Task<Result<IReadOnlyList<ExternalFixtureDto>>> FetchShortTermFixturesAsync(
        string externalLeagueId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches full-season fixtures for the given external league and season name.
    /// </summary>
    /// <param name="externalLeagueId">The league identifier assigned by the external provider.</param>
    /// <param name="seasonName">The season display name used by the provider.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A successful result containing fixtures, or a failure when the provider call fails.</returns>
    Task<Result<IReadOnlyList<ExternalFixtureDto>>> FetchLongTermFixturesAsync(
        string externalLeagueId,
        string seasonName,
        CancellationToken cancellationToken = default);
}

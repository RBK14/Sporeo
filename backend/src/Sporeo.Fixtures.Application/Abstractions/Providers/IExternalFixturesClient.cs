using Sporeo.BuildingBlocks.Domain.Results;
using Sporeo.Fixtures.Domain.Fixtures.Enums;

namespace Sporeo.Fixtures.Application.Abstractions.Providers;

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

/// <summary>
/// Synchronization horizon used by fixture sync jobs.
/// </summary>
public enum SyncMode
{
    /// <summary>
    /// Fetches recently finished and upcoming fixtures for a league.
    /// </summary>
    ShortTerm = 0,

    /// <summary>
    /// Fetches the full season schedule for a league.
    /// </summary>
    LongTerm = 1
}

/// <summary>
/// Provider-agnostic fixture payload used by batch synchronization.
/// </summary>
/// <param name="ProviderId">The fixture identifier assigned by the external provider.</param>
/// <param name="ProviderName">The external provider name.</param>
/// <param name="Name">The display name of the fixture.</param>
/// <param name="StartDate">The scheduled start timestamp in UTC.</param>
/// <param name="Status">The mapped fixture lifecycle status.</param>
/// <param name="Venue">The optional venue payload.</param>
public sealed record ExternalFixtureDto(
    string ProviderId,
    string ProviderName,
    string Name,
    DateTimeOffset StartDate,
    FixtureStatus Status,
    ExternalFixtureVenueDto? Venue);

/// <summary>
/// Provider-agnostic venue payload nested under an external fixture.
/// </summary>
/// <param name="ProviderId">The venue identifier assigned by the external provider.</param>
/// <param name="ProviderName">The external provider name.</param>
/// <param name="Name">The venue display name.</param>
/// <param name="Street">The street address, if known.</param>
/// <param name="City">The city, if known.</param>
/// <param name="Country">The country, if known.</param>
/// <param name="Latitude">The latitude, if known.</param>
/// <param name="Longitude">The longitude, if known.</param>
public sealed record ExternalFixtureVenueDto(
    string ProviderId,
    string ProviderName,
    string Name,
    string? Street,
    string? City,
    string? Country,
    double? Latitude,
    double? Longitude);

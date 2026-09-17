using Sporeo.Fixtures.Domain.Fixtures.Enums;

namespace Sporeo.Fixtures.Application.Fixtures.Abstractions.Providers;

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

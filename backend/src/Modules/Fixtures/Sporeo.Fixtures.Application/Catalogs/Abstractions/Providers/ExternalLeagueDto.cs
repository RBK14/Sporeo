namespace Sporeo.Fixtures.Application.Catalogs.Abstractions.Providers;

/// <summary>
/// Provider-agnostic league payload used by the admin catalog.
/// </summary>
/// <param name="ProviderId">The league identifier assigned by the external provider.</param>
/// <param name="ProviderName">The external provider name.</param>
/// <param name="ProviderSportName">The sport display name used by the provider to associate the league.</param>
/// <param name="Name">The display name of the league.</param>
/// <param name="Country">The country in which the league operates, if known.</param>
public sealed record ExternalLeagueDto(
    string ProviderId,
    string ProviderName,
    string ProviderSportName,
    string Name,
    string? Country);

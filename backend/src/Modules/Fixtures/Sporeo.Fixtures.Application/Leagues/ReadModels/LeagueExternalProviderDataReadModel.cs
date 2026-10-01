namespace Sporeo.Fixtures.Application.Leagues.ReadModels;

/// <summary>
/// External provider identity for a league used by sync and monitoring flows.
/// </summary>
/// <param name="ExternalProviderName">The external provider name.</param>
/// <param name="ExternalProviderId">The league identifier assigned by the external provider.</param>
public sealed record LeagueExternalProviderDataReadModel(
    string ExternalProviderName,
    string ExternalProviderId);

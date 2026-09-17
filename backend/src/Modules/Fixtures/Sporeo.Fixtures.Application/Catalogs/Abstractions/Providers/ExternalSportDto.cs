namespace Sporeo.Fixtures.Application.Catalogs.Abstractions.Providers;

/// <summary>
/// Provider-agnostic sport payload used by the admin catalog.
/// </summary>
/// <param name="ProviderId">The sport identifier assigned by the external provider.</param>
/// <param name="ProviderName">The external provider name.</param>
/// <param name="Name">The display name of the sport.</param>
public sealed record ExternalSportDto(
    string ProviderId,
    string ProviderName,
    string Name);

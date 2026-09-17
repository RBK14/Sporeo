namespace Sporeo.Fixtures.Application.Catalogs.Queries.GetCatalog;

/// <summary>
/// Sport entry returned by the admin catalog query, including nested leagues.
/// </summary>
/// <param name="ProviderId">The sport identifier assigned by the external provider.</param>
/// <param name="ProviderName">The external provider name.</param>
/// <param name="Name">The display name of the sport.</param>
/// <param name="Leagues">The leagues associated with this sport in the provider catalog.</param>
public sealed record CatalogSportResponse(
    string ProviderId,
    string ProviderName,
    string Name,
    IReadOnlyList<CatalogLeagueResponse> Leagues);

/// <summary>
/// League entry nested under a catalog sport response.
/// </summary>
/// <param name="ProviderId">The league identifier assigned by the external provider.</param>
/// <param name="ProviderName">The external provider name.</param>
/// <param name="Name">The display name of the league.</param>
/// <param name="IsMonitored">Indicates whether the league is monitored by synchronization processes.</param>
public sealed record CatalogLeagueResponse(
    Guid? Id,
    string ProviderId,
    string ProviderName,
    string Name,
    bool IsMonitored);

namespace Sporeo.Fixtures.Contracts.Catalogs.Responses;

/// <summary>
/// Sport entry in the administrator catalog browse response.
/// </summary>
/// <param name="ProviderId">External provider identifier for the sport.</param>
/// <param name="ProviderName">Name of the upstream sports data provider.</param>
/// <param name="Name">Display name of the sport.</param>
/// <param name="Leagues">Leagues associated with the sport.</param>
public sealed record CatalogSportResponse(
    string ProviderId,
    string ProviderName,
    string Name,
    IReadOnlyList<CatalogLeagueResponse> Leagues);

/// <summary>
/// League entry nested under a sport in the administrator catalog.
/// </summary>
/// <param name="Id">Internal Sporeo league id when the league already exists locally; otherwise null.</param>
/// <param name="ProviderId">External provider identifier for the league.</param>
/// <param name="ProviderName">Name of the upstream sports data provider.</param>
/// <param name="Name">Display name of the league.</param>
/// <param name="IsMonitored">Whether fixture sync is currently enabled for this league.</param>
public sealed record CatalogLeagueResponse(
    Guid? Id,
    string ProviderId,
    string ProviderName,
    string Name,
    bool IsMonitored);

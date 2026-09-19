namespace Sporeo.Fixtures.Contracts.Catalogs.Responses;

public sealed record CatalogSportResponse(
    string ProviderId,
    string ProviderName,
    string Name,
    IReadOnlyList<CatalogLeagueResponse> Leagues);

public sealed record CatalogLeagueResponse(
    Guid? Id,
    string ProviderId,
    string ProviderName,
    string Name,
    bool IsMonitored);

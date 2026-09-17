namespace Sporeo.Fixtures.Application.Catalogs.Queries.GetCatalog;

public record CatalogSportResponse(
    string ProviderId,
    string ProviderName,
    string Name,
    IReadOnlyList<CatalogLeagueResponse> Leagues);

public sealed record CatalogLeagueResponse(string ProviderId, string ProviderName, string Name);

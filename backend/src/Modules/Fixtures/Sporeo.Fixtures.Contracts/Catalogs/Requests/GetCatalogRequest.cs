namespace Sporeo.Fixtures.Contracts.Catalogs.Requests;

public sealed record GetCatalogRequest(int PageNumber = 1, int PageSize = 10);
namespace Sporeo.Fixtures.Contracts.Catalogs.Requests;

/// <summary>
/// Query parameters for browsing the external sports catalog.
/// </summary>
/// <param name="PageNumber">1-based page index. Defaults to 1.</param>
/// <param name="PageSize">Number of sports returned per page. Defaults to 10.</param>
public sealed record GetCatalogRequest(int PageNumber = 1, int PageSize = 10);

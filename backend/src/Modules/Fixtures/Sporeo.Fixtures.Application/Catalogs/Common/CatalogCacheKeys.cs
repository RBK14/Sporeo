namespace Sporeo.Fixtures.Application.Catalogs.Common;

/// <summary>
/// Redis cache keys for the admin sports and leagues catalog.
/// </summary>
internal static class CatalogCacheKeys
{
    /// <summary>
    /// Cache key for the full sports catalog payload.
    /// </summary>
    public static readonly string SportsCacheKey = "admin:catalog:sports";

    /// <summary>
    /// Cache key for the full leagues catalog payload.
    /// </summary>
    public static readonly string LeaguesCacheKey = "admin:catalog:leagues";
}

namespace Sporeo.Fixtures.Application.Catalogs.Common;

/// <summary>
/// Redis cache keys for the admin sports and leagues catalog, scoped per external provider.
/// </summary>
internal static class CatalogCacheKeys
{
    /// <summary>
    /// Builds the cache key for the sports catalog of the specified provider.
    /// </summary>
    public static string SportsKey(string providerName) =>
        $"admin:catalog:{providerName}:sports";

    /// <summary>
    /// Builds the cache key for the leagues catalog of the specified provider.
    /// </summary>
    public static string LeaguesKey(string providerName) =>
        $"admin:catalog:{providerName}:leagues";
}

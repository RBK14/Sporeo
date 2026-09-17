using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Sporeo.BuildingBlocks.Application.Abstractions.Caching;

namespace Sporeo.Fixtures.Infrastructure.Persistence.Caching;

/// <summary>
/// Redis-backed implementation of <see cref="ICacheService"/> using System.Text.Json.
/// </summary>
internal sealed class RedisCacheService(IDistributedCache distributedCache) : ICacheService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var cachedData = await distributedCache.GetAsync(key, cancellationToken);
        if (cachedData is null)
        {
            return default;
        }

        using var stream = new MemoryStream(cachedData);
        return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken);
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        using var stream = new MemoryStream();
        await JsonSerializer.SerializeAsync(stream, value, JsonOptions, cancellationToken);

        var options = new DistributedCacheEntryOptions();
        if (expiration.HasValue)
        {
            options.AbsoluteExpirationRelativeToNow = expiration;
        }

        await distributedCache.SetAsync(key, stream.ToArray(), options, cancellationToken);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await distributedCache.RemoveAsync(key, cancellationToken);
    }
}
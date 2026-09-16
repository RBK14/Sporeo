using Sporeo.BuildingBlocks.Application.Abstractions.Execution;

namespace Sporeo.BuildingBlocks.Application.Abstractions.Caching;

/// <summary>
/// Marks a query whose successful response can be served from a cache.
/// </summary>
public interface ICachedQuery
{
    /// <summary>
    /// Gets the unique cache key for the query.
    /// Must include user, tenant, or other scope identifiers when the response is not globally shared.
    /// </summary>
    string CacheKey { get; }

    /// <summary>
    /// Gets the optional time-to-live for the cached response.
    /// </summary>
    TimeSpan? Expiration { get; }
}

/// <summary>
/// Marks a query that returns a typed response and supports response caching.
/// </summary>
/// <typeparam name="TResponse">The type of the query response.</typeparam>
public interface ICachedQuery<TResponse> : IQuery<TResponse>, ICachedQuery
{
}

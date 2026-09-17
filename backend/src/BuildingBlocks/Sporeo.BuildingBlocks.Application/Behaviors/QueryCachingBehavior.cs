using MediatR;
using Microsoft.Extensions.Logging;
using Sporeo.BuildingBlocks.Application.Abstractions.Caching;
using Sporeo.BuildingBlocks.Application.Extensions;
using Sporeo.BuildingBlocks.Domain.Results;

namespace Sporeo.BuildingBlocks.Application.Behaviors;

/// <summary>
/// MediatR pipeline behavior that caches successful query responses using <see cref="ICacheService"/>.
/// </summary>
/// <typeparam name="TRequest">The type of the cached query being handled.</typeparam>
/// <typeparam name="TResponse">The type of the response returned by the handler.</typeparam>
/// <param name="cacheService">The cache service used to store and retrieve responses.</param>
/// <param name="logger">The logger used to record cache hit and miss events.</param>
public class QueryCachingBehavior<TRequest, TResponse>(
    ICacheService cacheService,
    ILogger<QueryCachingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICachedQuery
    where TResponse : Result
{
    private readonly ICacheService _cacheService = cacheService;
    private readonly ILogger<QueryCachingBehavior<TRequest, TResponse>> _logger = logger;

    /// <inheritdoc />
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var cacheKeyHash = BehaviorsLoggerExtensions.ToLogSafeCacheKeyHash(request.CacheKey);
        var cachedResponse = await _cacheService.GetAsync<TResponse>(request.CacheKey, cancellationToken);

        if (cachedResponse is not null)
        {
            _logger.LogCacheHit(cacheKeyHash);
            return cachedResponse;
        }

        _logger.LogCacheMiss(cacheKeyHash);

        var response = await next();

        if (response.IsSuccess)
        {
            await _cacheService.SetAsync(
                request.CacheKey,
                response,
                request.Expiration,
                cancellationToken);
        }

        return response;
    }
}

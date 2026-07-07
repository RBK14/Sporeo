using MediatR;
using Microsoft.Extensions.Logging;
using Sporeo.BuildingBlocks.Application.Abstractions.Caching;
using Sporeo.BuildingBlocks.Application.Extensions;
using Sporeo.BuildingBlocks.Domain.Results;

namespace Sporeo.BuildingBlocks.Application.Behaviors;

public class QueryCachingBehavior<TRequest, TResponse>(
    ICacheService cacheService,
    ILogger<QueryCachingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICachedQuery
    where TResponse : Result
{
    private readonly ICacheService _cacheService = cacheService;
    private readonly ILogger<QueryCachingBehavior<TRequest, TResponse>> _logger = logger;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var cachedResponse = await _cacheService.GetAsync<TResponse>(request.CacheKey, cancellationToken);
        
        if (cachedResponse is not null)
        {
            _logger.LogCacheHit(request.CacheKey);
            return cachedResponse;
        }

        _logger.LogCacheMiss(request.CacheKey);

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

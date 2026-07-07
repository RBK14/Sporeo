using MediatR;
using Microsoft.Extensions.Logging;
using Sporeo.BuildingBlocks.Application.Extensions;

namespace Sporeo.BuildingBlocks.Application.Behaviors;

/// <summary>
/// MediatR pipeline behavior that logs the start, completion, and failure of request handling.
/// </summary>
/// <typeparam name="TRequest">The type of the request being handled.</typeparam>
/// <typeparam name="TResponse">The type of the response returned by the handler.</typeparam>
/// <param name="logger">The logger used to record request lifecycle events.</param>
public class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger = logger;

    /// <inheritdoc />
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        _logger.LogProcessingRequest(requestName);

        try
        {
            var response = await next();
            _logger.LogProcessedRequest(requestName);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogProcessingRequestFailed(ex, requestName);
            throw;
        }
    }
}

using MediatR;
using Microsoft.Extensions.Logging;
using Sporeo.BuildingBlocks.Application.Extensions;

namespace Sporeo.BuildingBlocks.Application.Behaviors;

public class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger = logger;

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

using MediatR;
using Microsoft.Extensions.Logging;
using Sporeo.BuildingBlocks.Application.Extensions;
using System.Diagnostics;

namespace Sporeo.BuildingBlocks.Application.Behaviors;

/// <summary>
/// MediatR pipeline behavior that logs a warning when request handling exceeds a duration threshold.
/// </summary>
/// <typeparam name="TRequest">The type of the request being handled.</typeparam>
/// <typeparam name="TResponse">The type of the response returned by the handler.</typeparam>
/// <param name="logger">The logger used to record long-running requests.</param>
public class PerformanceBehavior<TRequest, TResponse>(
    ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger = logger;
    private const int ThresholdInMilliseconds = 500;

    /// <inheritdoc />
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var timer = Stopwatch.StartNew();

        var response = await next();

        timer.Stop();

        if (timer.ElapsedMilliseconds > ThresholdInMilliseconds)
        {
            var requestName = typeof(TRequest).Name;
            _logger.LogLongRunningRequest(requestName, timer.ElapsedMilliseconds);
        }

        return response;
    }
}

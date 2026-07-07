using MediatR;
using Microsoft.Extensions.Logging;
using Sporeo.BuildingBlocks.Application.Extensions;
using System.Diagnostics;

namespace Sporeo.BuildingBlocks.Application.Behaviors;

public class PerformanceBehavior<TRequest, TResponse>(
    ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger = logger;
    private const int ThresholdInMilliseconds = 500;

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

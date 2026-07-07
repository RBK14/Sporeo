using MediatR;
using Microsoft.Extensions.Logging;
using Sporeo.BuildingBlocks.Application.Abstractions.Data;
using Sporeo.BuildingBlocks.Application.Abstractions.Execution;
using Sporeo.BuildingBlocks.Application.Extensions;
using Sporeo.BuildingBlocks.Domain.Results;

namespace Sporeo.BuildingBlocks.Application.Behaviors;

/// <summary>
/// MediatR pipeline behavior that commits pending unit-of-work changes after a successful command.
/// Skips persistence when the handler returns a failed <see cref="Result"/>.
/// </summary>
/// <typeparam name="TRequest">The type of the command being handled.</typeparam>
/// <typeparam name="TResponse">The type of the response returned by the handler.</typeparam>
/// <param name="unitOfWork">The unit of work used to persist changes.</param>
/// <param name="logger">The logger used to record unit-of-work lifecycle events.</param>
public class TransactionBehavior<TRequest, TResponse>(
    IUnitOfWork unitOfWork,
    ILogger<TransactionBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IBaseCommand
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger = logger;

    /// <inheritdoc />
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var commandName = typeof(TRequest).Name;
        _logger.LogBeginUnitOfWork(commandName);

        var response = await next();

        if (response is Result { IsFailure: true })
        {
            _logger.LogSkippedUnitOfWorkCommit(commandName);
            return response;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogCommitUnitOfWork(commandName);

        return response;
    }
}

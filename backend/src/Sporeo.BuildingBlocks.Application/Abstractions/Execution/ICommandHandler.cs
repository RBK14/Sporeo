using MediatR;
using Sporeo.BuildingBlocks.Domain.Results;

namespace Sporeo.BuildingBlocks.Application.Abstractions.Execution;

/// <summary>
/// Handles a command that does not return a value on success.
/// </summary>
/// <typeparam name="TCommand">The type of the command to handle.</typeparam>
public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand, Result>
    where TCommand : ICommand
{
}

/// <summary>
/// Handles a command that returns a value on success.
/// </summary>
/// <typeparam name="TCommand">The type of the command to handle.</typeparam>
/// <typeparam name="TResponse">The type of the value returned on success.</typeparam>
public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse>
{
}

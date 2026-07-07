using MediatR;
using Sporeo.BuildingBlocks.Domain.Results;

namespace Sporeo.BuildingBlocks.Application.Abstractions.Execution;

/// <summary>
/// Marks a request as a command that mutates application state.
/// </summary>
public interface IBaseCommand
{
}

/// <summary>
/// Represents a command that does not return a value on success.
/// </summary>
public interface ICommand : IRequest<Result>, IBaseCommand
{
}

/// <summary>
/// Represents a command that returns a value on success.
/// </summary>
/// <typeparam name="TResponse">The type of the value returned on success.</typeparam>
public interface ICommand<TResponse> : IRequest<Result<TResponse>>, IBaseCommand
{
}

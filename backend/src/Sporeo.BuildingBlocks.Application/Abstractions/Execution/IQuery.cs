using MediatR;
using Sporeo.BuildingBlocks.Domain.Results;

namespace Sporeo.BuildingBlocks.Application.Abstractions.Execution;

/// <summary>
/// Represents a read-only request that returns a value wrapped in a <see cref="Result{TResponse}"/>.
/// </summary>
/// <typeparam name="TResponse">The type of the value returned on success.</typeparam>
public interface IQuery<TResponse> : IRequest<Result<TResponse>>
{
}

using MediatR;
using Sporeo.BuildingBlocks.Domain.Results;

namespace Sporeo.BuildingBlocks.Application.Abstractions.Execution;

public interface IQuery<TResponse> : IRequest<Result<TResponse>>
{
}

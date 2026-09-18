using Sporeo.BuildingBlocks.Application.Abstractions.Execution;
using Sporeo.BuildingBlocks.Domain.Results;
using Sporeo.Fixtures.Application.Sports.Abstractions.Repositories;
using Sporeo.Fixtures.Domain.Sports;
using Sporeo.Fixtures.Domain.Sports.ValueObjects;

namespace Sporeo.Fixtures.Application.Sports.Commands.CreateSport;

internal sealed class CreateSportCommandHandler(
    ISportRepository sportRepository) : ICommandHandler<CreateSportCommand, SportId>
{
    public async Task<Result<SportId>> Handle(CreateSportCommand request, CancellationToken cancellationToken)
    {
        var sportResult = Sport.Create(request.Name);

        if (sportResult.IsFailure)
            return Result.Failure<SportId>(sportResult.Error);

        var sport = sportResult.Value;
        sportRepository.Add(sport);

        return Result.Success(sport.Id);
    }
}

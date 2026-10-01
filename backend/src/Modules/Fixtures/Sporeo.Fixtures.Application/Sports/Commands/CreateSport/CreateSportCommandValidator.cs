using FluentValidation;
using Sporeo.Fixtures.Domain.Common;

namespace Sporeo.Fixtures.Application.Sports.Commands.CreateSport;

internal sealed class CreateSportCommandValidator : AbstractValidator<CreateSportCommand>
{
    public CreateSportCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .WithErrorCode(Errors.Sport.EmptyName.Code)
            .WithMessage(Errors.Sport.EmptyName.Message);
    }
}

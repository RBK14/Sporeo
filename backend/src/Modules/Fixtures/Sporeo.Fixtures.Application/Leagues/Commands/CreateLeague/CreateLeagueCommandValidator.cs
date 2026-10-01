using FluentValidation;
using Sporeo.Fixtures.Domain.Common;

namespace Sporeo.Fixtures.Application.Leagues.Commands.CreateLeague;

internal sealed class CreateLeagueCommandValidator : AbstractValidator<CreateLeagueCommand>
{
    public CreateLeagueCommandValidator()
    {
        RuleFor(command => command.SportId)
            .NotNull()
            .WithErrorCode(Errors.Sport.NotFoundCode)
            .WithMessage("Sport id is required.");

        RuleFor(command => command.Name)
            .NotEmpty()
            .WithErrorCode(Errors.League.EmptyName.Code)
            .WithMessage(Errors.League.EmptyName.Message);
    }
}

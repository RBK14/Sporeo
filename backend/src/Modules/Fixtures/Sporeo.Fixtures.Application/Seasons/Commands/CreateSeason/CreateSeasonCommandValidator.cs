using FluentValidation;
using Sporeo.Fixtures.Domain.Common;

namespace Sporeo.Fixtures.Application.Seasons.Commands.CreateSeason;

internal sealed class CreateSeasonCommandValidator : AbstractValidator<CreateSeasonCommand>
{
    public CreateSeasonCommandValidator()
    {
        RuleFor(command => command.LeagueId)
            .NotNull()
            .WithErrorCode(Errors.League.NotFoundCode)
            .WithMessage("League id is required.");

        RuleFor(command => command.Name)
            .NotEmpty()
            .WithErrorCode(Errors.Season.EmptyName.Code)
            .WithMessage(Errors.Season.EmptyName.Message);
    }
}

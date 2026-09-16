using FluentValidation;

namespace Sporeo.Fixtures.Application.Fixtures.Commands.SyncFixturesBatch;

/// <summary>
/// Validates the envelope of <see cref="SyncFixturesBatchCommand"/> before orchestration.
/// Item-level defects are reported as skipped/failed in the sync report, not as validation errors.
/// </summary>
internal sealed class SyncFixturesBatchCommandValidator : AbstractValidator<SyncFixturesBatchCommand>
{
    public SyncFixturesBatchCommandValidator()
    {
        RuleFor(command => command.SportId)
            .NotEmpty()
            .WithErrorCode("SyncFixtures.InvalidSportId");

        RuleFor(command => command.LeagueId)
            .Must(id => id is null || id != Guid.Empty)
            .WithErrorCode("SyncFixtures.InvalidLeagueId");

        RuleFor(command => command.SeasonId)
            .Must(id => id is null || id != Guid.Empty)
            .WithErrorCode("SyncFixtures.InvalidSeasonId");

        RuleFor(command => command.ProviderName)
            .NotEmpty()
            .WithErrorCode("SyncFixtures.InvalidProviderName");

        RuleFor(command => command.Fixtures)
            .NotNull()
            .WithErrorCode("SyncFixtures.FixturesRequired")
            .Must(fixtures => fixtures.Count > 0)
            .WithErrorCode("SyncFixtures.FixturesEmpty")
            .Must(fixtures => fixtures.Count <= SyncFixturesBatchCommand.MaxBatchSize)
            .WithErrorCode("SyncFixtures.BatchTooLarge")
            .WithMessage($"Batch size must be between 1 and {SyncFixturesBatchCommand.MaxBatchSize}.");
    }
}

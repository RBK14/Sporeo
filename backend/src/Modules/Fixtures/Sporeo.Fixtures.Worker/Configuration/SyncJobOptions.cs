using Sporeo.Fixtures.Application.Fixtures.Abstractions.Providers;
using System.ComponentModel.DataAnnotations;
using Quartz;

namespace Sporeo.Fixtures.Worker.Configuration;

/// <summary>
/// Configuration for a single fixture synchronization Quartz job.
/// </summary>
public sealed class SyncJobOptions : IValidatableObject
{
    /// <summary>
    /// Gets the stable Quartz job identity.
    /// </summary>
    [Required]
    public string JobId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the cron expression used to schedule the job.
    /// </summary>
    [Required]
    public string CronSchedule { get; init; } = string.Empty;

    /// <summary>
    /// Gets the synchronization mode.
    /// </summary>
    public SyncMode SyncMode { get; init; }

    /// <summary>
    /// Gets the external provider name used to resolve an <see cref="IExternalFixturesClient"/>.
    /// </summary>
    [Required]
    public string ProviderName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the external league identifier.
    /// </summary>
    [Required]
    public string ExternalLeagueId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the external season identifier required for long-term synchronization.
    /// </summary>
    public string? ExternalSeasonId { get; init; }

    /// <summary>
    /// Gets the Sporeo sport identifier assigned to synchronized fixtures.
    /// </summary>
    public Guid SportId { get; init; }

    /// <summary>
    /// Gets the optional Sporeo league identifier.
    /// </summary>
    public Guid? LeagueId { get; init; }

    /// <summary>
    /// Gets the optional Sporeo season identifier.
    /// </summary>
    public Guid? SeasonId { get; init; }

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (SportId == Guid.Empty)
            yield return new ValidationResult("'SportId' cannot be an empty Guid.", [nameof(SportId)]);

        if (LeagueId == Guid.Empty)
            yield return new ValidationResult("'LeagueId' cannot be an empty Guid.", [nameof(LeagueId)]);

        if (SeasonId == Guid.Empty)
            yield return new ValidationResult("'SeasonId' cannot be an empty Guid.", [nameof(SeasonId)]);

        if (SyncMode == SyncMode.LongTerm && string.IsNullOrWhiteSpace(ExternalSeasonId))
            yield return new ValidationResult("Long term sync requires a valid 'ExternalSeasonId'.", [nameof(ExternalSeasonId)]);

        if (!string.IsNullOrWhiteSpace(CronSchedule))
        {
            var cronIsValid = true;
            try
            {
                _ = new CronExpression(CronSchedule);
            }
            catch (Exception)
            {
                cronIsValid = false;
            }

            if (!cronIsValid)
                yield return new ValidationResult($"'{CronSchedule}' is not a valid cron expression.", [nameof(CronSchedule)]);
        }
    }
}

using System.ComponentModel.DataAnnotations;

namespace Sporeo.Fixtures.Worker.Configuration;

/// <summary>
/// Root configuration section for fixture synchronization jobs scheduled by Quartz.
/// </summary>
public sealed class SyncJobsRootOptions : IValidatableObject
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "SyncJobs";

    /// <summary>
    /// Gets the synchronization job definitions. An empty list is allowed when only outbox processing is required.
    /// </summary>
    [Required]
    public List<SyncJobOptions> Jobs { get; init; } = [];

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var duplicateIds = Jobs
            .GroupBy(job => job.JobId, StringComparer.OrdinalIgnoreCase)
            .Where(group => !string.IsNullOrWhiteSpace(group.Key) && group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        if (duplicateIds.Count > 0)
        {
            yield return new ValidationResult(
                $"Duplicate SyncJobs JobId values: {string.Join(", ", duplicateIds)}.",
                [nameof(Jobs)]);
        }
    }
}

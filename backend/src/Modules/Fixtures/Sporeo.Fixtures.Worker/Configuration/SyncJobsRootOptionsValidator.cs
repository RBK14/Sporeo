using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Sporeo.Fixtures.Application.Fixtures.Abstractions.Providers;

namespace Sporeo.Fixtures.Worker.Configuration;

/// <summary>
/// Fail-fast validator for sync job configuration and provider identity.
/// </summary>
internal sealed class SyncJobsRootOptionsValidator(IServiceScopeFactory scopeFactory)
    : IValidateOptions<SyncJobsRootOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, SyncJobsRootOptions options)
    {
        if (options is null)
            return ValidateOptionsResult.Fail("SyncJobs configuration is required.");

        var failures = new List<string>();

        var duplicateIds = options.Jobs
            .GroupBy(job => job.JobId, StringComparer.OrdinalIgnoreCase)
            .Where(group => !string.IsNullOrWhiteSpace(group.Key) && group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        if (duplicateIds.Count > 0)
        {
            failures.Add($"Duplicate SyncJobs JobId values: {string.Join(", ", duplicateIds)}.");
        }

        string[] providerNames;
        using (var scope = scopeFactory.CreateScope())
        {
            providerNames = scope.ServiceProvider
                .GetServices<IExternalFixturesClient>()
                .Select(provider => provider.ProviderName)
                .Where(providerName => !string.IsNullOrWhiteSpace(providerName))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        for (var index = 0; index < options.Jobs.Count; index++)
        {
            var job = options.Jobs[index];
            var context = new ValidationContext(job);
            var results = new List<ValidationResult>();
            if (!Validator.TryValidateObject(job, context, results, validateAllProperties: true))
            {
                failures.AddRange(results.Select(result =>
                    $"SyncJobs:Jobs[{index}]: {result.ErrorMessage}"));
            }

            if (!string.IsNullOrWhiteSpace(job.ProviderName)
                && providerNames.Length > 0
                && !providerNames.Contains(job.ProviderName, StringComparer.OrdinalIgnoreCase))
            {
                failures.Add(
                    $"SyncJobs:Jobs[{index}]: ProviderName '{job.ProviderName}' does not match a registered provider ({string.Join(", ", providerNames)}).");
            }
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}

using Microsoft.Extensions.Options;
using Quartz;

namespace Sporeo.Fixtures.Worker.Configuration;

internal sealed class WorkerOptionsValidator : IValidateOptions<WorkerOptions>
{
    public ValidateOptionsResult Validate(string? name, WorkerOptions options)
    {
        var failures = new List<string>();

        ValidateCron(options.DispatcherCron, nameof(options.DispatcherCron), failures);
        ValidateCron(options.LongTermSyncCron, nameof(options.LongTermSyncCron), failures);

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }

    private static void ValidateCron(string? value, string propertyName, List<string> failures)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            failures.Add($"{propertyName} is required.");
            return;
        }

        if (!CronExpression.TryParse(value, out _))
        {
            failures.Add($"{propertyName} has invalid cron expression format: '{value}'");
        }
    }
}

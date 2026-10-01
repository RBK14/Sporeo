using System.ComponentModel.DataAnnotations;

namespace Sporeo.BuildingBlocks.Infrastructure.Messaging.Outbox.Configuration;

/// <summary>
/// Configurable settings for outbox claiming, batching, and retry budgets.
/// </summary>
public sealed class OutboxOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Outbox";

    /// <summary>
    /// Maximum number of failed attempts (including lease reclaim) before dead-lettering.
    /// </summary>
    [Range(1, 100)]
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Maximum number of messages claimed in a single processing batch.
    /// </summary>
    [Range(1, 500)]
    public int BatchSize { get; set; } = 20;

    /// <summary>
    /// Lease duration in seconds granted when a worker claims a message for processing.
    /// Defaults to 5 minutes.
    /// </summary>
    [Range(30, 3600)]
    public int ClaimLeaseSeconds { get; set; } = 300;

    /// <summary>
    /// Gets the claim lease as a <see cref="TimeSpan"/>.
    /// </summary>
    public TimeSpan ClaimLease => TimeSpan.FromSeconds(ClaimLeaseSeconds);
}

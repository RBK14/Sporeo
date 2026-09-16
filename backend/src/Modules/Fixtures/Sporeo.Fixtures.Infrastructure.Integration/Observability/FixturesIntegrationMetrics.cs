using System.Diagnostics.Metrics;

namespace Sporeo.Fixtures.Infrastructure.Integration.Observability;

/// <summary>
/// OpenTelemetry metrics for external fixture provider integration.
/// </summary>
public static class FixturesIntegrationMetrics
{
    /// <summary>
    /// Meter name used by the fixtures integration layer.
    /// </summary>
    public const string MeterName = "Sporeo.Fixtures.Integration";

    private static readonly Meter Meter = new(MeterName);

    /// <summary>
    /// Counts provider events dropped during mapping/validation.
    /// </summary>
    public static readonly Counter<long> EventsDropped =
        Meter.CreateCounter<long>("fixtures.integration.events.dropped");

    /// <summary>
    /// Counts provider status values that fell back to a default mapping.
    /// </summary>
    public static readonly Counter<long> UnknownStatuses =
        Meter.CreateCounter<long>("fixtures.integration.status.unknown");
}

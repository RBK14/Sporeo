using System.Runtime.CompilerServices;

// Set InternalsVisibleTo attribute to allow access to internal members from the test project
[assembly: InternalsVisibleTo("Sporeo.BuildingBlocks.Domain.Tests")]

namespace Sporeo.BuildingBlocks.Domain.Time;

/// <summary>
/// Provides access to the current UTC time through a replaceable <see cref="TimeProvider"/>.
/// </summary>
public static class SystemTimeProvider
{
    /// <summary>
    /// Gets or sets the <see cref="TimeProvider"/> used to obtain the current time.
    /// Defaults to <see cref="TimeProvider.System"/>.
    /// </summary>
    public static TimeProvider Provider { get; internal set; } = TimeProvider.System;

    /// <summary>
    /// Gets the current UTC date and time.
    /// </summary>
    public static DateTimeOffset Now => Provider.GetUtcNow();
}

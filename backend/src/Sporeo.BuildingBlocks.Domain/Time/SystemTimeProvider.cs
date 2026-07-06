using System.Runtime.CompilerServices;

// set InternalsVisibleTo attribute to allow access to internal members from the test project
[assembly: InternalsVisibleTo("Sporeo.BuildingBlocks.Domain.Tests")]

namespace Sporeo.BuildingBlocks.Domain.Time;

public static class SystemTimeProvider
{
    public static TimeProvider Provider { get; internal set; } = TimeProvider.System;

    public static DateTimeOffset Now => Provider.GetUtcNow();
}

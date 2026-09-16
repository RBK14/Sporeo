using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Sporeo.Fixtures.Infrastructure.Integration.Configuration;
using Sporeo.Fixtures.Infrastructure.Integration.Geocoding;

namespace Sporeo.Fixtures.Infrastructure.Integration.Tests.Geocoding;

public sealed class GeocodingRateLimiterTests
{
    [Fact]
    public async Task WaitAsync_EnforcesMinimumOneSecondBetweenAcquisitions()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        using var limiter = new GeocodingRateLimiter(
            Options.Create(new NominatimOptions
            {
                MinRequestInterval = TimeSpan.FromSeconds(1),
                UserAgent = "Sporeo/1.0 (fixtures-worker; contact@sporeo.app)",
                BaseUrl = "https://nominatim.openstreetmap.org"
            }),
            timeProvider);

        await limiter.WaitAsync();

        var secondWait = limiter.WaitAsync().AsTask();
        secondWait.IsCompleted.Should().BeFalse();

        timeProvider.Advance(TimeSpan.FromMilliseconds(999));
        secondWait.IsCompleted.Should().BeFalse();

        timeProvider.Advance(TimeSpan.FromMilliseconds(1));
        await secondWait.WaitAsync(TimeSpan.FromSeconds(1));
        secondWait.IsCompletedSuccessfully.Should().BeTrue();
    }
}

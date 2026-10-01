using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Sporeo.Fixtures.Infrastructure.Integration.Configuration;
using Sporeo.Fixtures.Infrastructure.Integration.Providers.Nominatim;

namespace Sporeo.Fixtures.Infrastructure.Integration.Tests.Geocoding;

public sealed class GeocodingRateLimiterTests
{
    [Fact]
    public async Task AcquireAsync_EnforcesMinimumOneSecondBetweenAcquisitions()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        using var limiter = new NominatimRateLimiter(
            Options.Create(new NominatimOptions
            {
                MinRequestInterval = TimeSpan.FromSeconds(1),
                UserAgent = "Sporeo/1.0 (fixtures-worker; contact@sporeo.app)",
                BaseUrl = "https://nominatim.openstreetmap.org"
            }),
            timeProvider);

        await using var firstLease = await limiter.AcquireAsync();
        await firstLease.DisposeAsync();

        var secondAcquire = limiter.AcquireAsync().AsTask();
        secondAcquire.IsCompleted.Should().BeFalse();

        timeProvider.Advance(TimeSpan.FromMilliseconds(999));
        secondAcquire.IsCompleted.Should().BeFalse();

        timeProvider.Advance(TimeSpan.FromMilliseconds(1));
        await using var secondLease = await secondAcquire.WaitAsync(TimeSpan.FromSeconds(1));
        secondAcquire.IsCompletedSuccessfully.Should().BeTrue();
    }

    [Fact]
    public async Task AcquireAsync_HoldsGateUntilLeaseDisposed()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        using var limiter = new NominatimRateLimiter(
            Options.Create(new NominatimOptions
            {
                MinRequestInterval = TimeSpan.FromSeconds(1),
                UserAgent = "Sporeo/1.0 (fixtures-worker; contact@sporeo.app)",
                BaseUrl = "https://nominatim.openstreetmap.org"
            }),
            timeProvider);

        var firstLease = await limiter.AcquireAsync();
        var secondAcquire = limiter.AcquireAsync().AsTask();
        secondAcquire.IsCompleted.Should().BeFalse();

        await firstLease.DisposeAsync();
        secondAcquire.IsCompleted.Should().BeFalse();

        timeProvider.Advance(TimeSpan.FromSeconds(1));
        await using var secondLease = await secondAcquire.WaitAsync(TimeSpan.FromSeconds(1));
        secondAcquire.IsCompletedSuccessfully.Should().BeTrue();
    }
}

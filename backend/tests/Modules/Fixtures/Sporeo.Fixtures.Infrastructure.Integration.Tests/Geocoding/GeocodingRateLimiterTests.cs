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

        // Gate released, but the min-interval delay still applies. Advance may race with
        // timer registration on FakeTimeProvider, so pump until the waiter completes.
        secondAcquire.IsCompleted.Should().BeFalse();

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!secondAcquire.IsCompleted)
        {
            timeProvider.Advance(TimeSpan.FromSeconds(1));
            try
            {
                await Task.Delay(10, timeout.Token);
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException(
                    "Second AcquireAsync did not complete after disposing the first lease and advancing fake time.");
            }
        }

        await using var secondLease = await secondAcquire;
        secondAcquire.IsCompletedSuccessfully.Should().BeTrue();
    }
}

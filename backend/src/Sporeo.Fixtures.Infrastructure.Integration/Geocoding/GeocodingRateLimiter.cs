using System.Threading.RateLimiting;
using Microsoft.Extensions.Options;
using Sporeo.Fixtures.Infrastructure.Integration.Configuration;

namespace Sporeo.Fixtures.Infrastructure.Integration.Geocoding;

/// <summary>
/// Process-wide rate limiter for geocoding requests.
/// </summary>
public interface IGeocodingRateLimiter
{
    /// <summary>
    /// Waits until a geocoding request is permitted.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the wait.</param>
    ValueTask WaitAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Token-bucket style limiter enforcing a single global geocoding cadence.
/// </summary>
internal sealed class GeocodingRateLimiter : IGeocodingRateLimiter, IDisposable
{
    private readonly TokenBucketRateLimiter _limiter;

    public GeocodingRateLimiter(IOptions<NominatimOptions> options)
    {
        var interval = options.Value.MinRequestInterval <= TimeSpan.Zero
            ? TimeSpan.FromMilliseconds(1100)
            : options.Value.MinRequestInterval;

        _limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
        {
            TokenLimit = 1,
            TokensPerPeriod = 1,
            ReplenishmentPeriod = interval,
            QueueLimit = 100,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            AutoReplenishment = true
        });
    }

    /// <inheritdoc />
    public async ValueTask WaitAsync(CancellationToken cancellationToken = default)
    {
        using var lease = await _limiter.AcquireAsync(1, cancellationToken);
        if (!lease.IsAcquired)
            throw new InvalidOperationException("Unable to acquire geocoding rate limiter lease.");
    }

    /// <inheritdoc />
    public void Dispose() => _limiter.Dispose();
}

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
/// Serializes outbound Nominatim calls and enforces a minimum interval between HTTP attempts.
/// </summary>
internal sealed class GeocodingRateLimiter : IGeocodingRateLimiter, IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _minInterval;
    private long _nextAllowedTimestamp;

    public GeocodingRateLimiter(IOptions<NominatimOptions> options, TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
        var configured = options.Value.MinRequestInterval;
        _minInterval = configured < TimeSpan.FromSeconds(1)
            ? TimeSpan.FromSeconds(1)
            : configured;
        _nextAllowedTimestamp = _timeProvider.GetTimestamp();
    }

    /// <inheritdoc />
    public async ValueTask WaitAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var now = _timeProvider.GetTimestamp();
            if (now < _nextAllowedTimestamp)
            {
                var delay = TimestampDeltaToTimeSpan(_nextAllowedTimestamp - now);
                if (delay > TimeSpan.Zero)
                    await DelayAsync(delay, cancellationToken).ConfigureAwait(false);
            }

            _nextAllowedTimestamp = _timeProvider.GetTimestamp()
                + TimeSpanToTimestampDelta(_minInterval);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var registration = cancellationToken.Register(
            static state => ((TaskCompletionSource)state!).TrySetCanceled(),
            completion);

        using var timer = _timeProvider.CreateTimer(
            static state => ((TaskCompletionSource)state!).TrySetResult(),
            completion,
            delay,
            Timeout.InfiniteTimeSpan);

        await completion.Task.ConfigureAwait(false);
    }

    private long TimeSpanToTimestampDelta(TimeSpan value) =>
        (long)(value.TotalSeconds * _timeProvider.TimestampFrequency);

    private TimeSpan TimestampDeltaToTimeSpan(long timestampDelta) =>
        TimeSpan.FromSeconds((double)timestampDelta / _timeProvider.TimestampFrequency);

    /// <inheritdoc />
    public void Dispose() => _gate.Dispose();
}

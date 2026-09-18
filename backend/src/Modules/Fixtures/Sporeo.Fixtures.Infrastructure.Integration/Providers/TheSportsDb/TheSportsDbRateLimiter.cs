using System.Net;
using System.Threading.RateLimiting;

namespace Sporeo.Fixtures.Infrastructure.Integration.Providers.TheSportsDb;

internal sealed class TheSportsDbRateLimiter(TokenBucketRateLimiter rateLimiter) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        using var lease = await rateLimiter.AcquireAsync(1, cancellationToken);

        if (!lease.IsAcquired)
            return new HttpResponseMessage(HttpStatusCode.TooManyRequests);

        return await base.SendAsync(request, cancellationToken);
    }
}
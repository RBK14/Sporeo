using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Sporeo.Fixtures.Infrastructure.Integration.Configuration;

namespace Sporeo.Fixtures.Infrastructure.Integration.Health;

/// <summary>
/// Health check that verifies TheSportsDB base URL is reachable.
/// </summary>
public sealed class TheSportsDbHealthCheck(
    IHttpClientFactory httpClientFactory,
    IOptions<TheSportsDbOptions> options) : IHealthCheck
{
    /// <summary>
    /// Named HTTP client used by this health check.
    /// </summary>
    public const string HttpClientName = nameof(TheSportsDbHealthCheck);

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client = httpClientFactory.CreateClient(HttpClientName);
            var baseUrl = options.Value.BaseUrl.TrimEnd('/') + "/";
            using var response = await client.GetAsync(baseUrl, cancellationToken);

            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("TheSportsDB is reachable.")
                : HealthCheckResult.Degraded($"TheSportsDB returned {(int)response.StatusCode}.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("TheSportsDB is unreachable.", ex);
        }
    }
}

/// <summary>
/// Health check that verifies Nominatim base URL is reachable.
/// </summary>
public sealed class NominatimHealthCheck(
    IHttpClientFactory httpClientFactory,
    IOptions<NominatimOptions> options) : IHealthCheck
{
    /// <summary>
    /// Named HTTP client used by this health check.
    /// </summary>
    public const string HttpClientName = nameof(NominatimHealthCheck);

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client = httpClientFactory.CreateClient(HttpClientName);
            var statusUrl = options.Value.BaseUrl.TrimEnd('/') + "/status.php";
            using var response = await client.GetAsync(statusUrl, cancellationToken);

            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("Nominatim is reachable.")
                : HealthCheckResult.Degraded($"Nominatim returned {(int)response.StatusCode}.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Nominatim is unreachable.", ex);
        }
    }
}

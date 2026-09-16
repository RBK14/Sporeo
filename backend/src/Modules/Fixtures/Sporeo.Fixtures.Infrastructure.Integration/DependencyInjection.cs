using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Sporeo.Fixtures.Application.Abstractions.Geocoding;
using Sporeo.Fixtures.Application.Abstractions.Providers;
using Sporeo.Fixtures.Infrastructure.Integration.Configuration;
using Sporeo.Fixtures.Infrastructure.Integration.Geocoding;
using Sporeo.Fixtures.Infrastructure.Integration.Providers.TheSportsDb;
using System.Net;
using System.Net.Http.Headers;
using System.Threading.RateLimiting;

namespace Sporeo.Fixtures.Infrastructure.Integration;

/// <summary>
/// Registers external integration services for fixture providers and geocoding.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds typed HTTP clients and options for TheSportsDB and Nominatim.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The same <paramref name="services"/> instance for chaining.</returns>
    public static IServiceCollection AddIntegration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<TheSportsDbOptions>()
            .Bind(configuration.GetSection(TheSportsDbOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(
                options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var uri) &&
                           uri.Scheme == Uri.UriSchemeHttps &&
                           !string.IsNullOrWhiteSpace(options.ApiKey),
                "ExternalProviders:TheSportsDb requires an HTTPS BaseUrl and ApiKey.")
            .ValidateOnStart();

        services.AddOptions<NominatimOptions>()
            .Bind(configuration.GetSection(NominatimOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(
                options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var uri) &&
                           (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp),
                "ExternalProviders:Nominatim requires an absolute BaseUrl.")
            .ValidateOnStart();

        services.AddSingleton<IGeocodingRateLimiter, GeocodingRateLimiter>();

        services.AddHttpClient<IExternalFixturesClient, TheSportsDbClient>((sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<TheSportsDbOptions>>().Value;
                var baseUrl = options.BaseUrl.TrimEnd('/');
                client.BaseAddress = new Uri($"{baseUrl}/{options.ApiKey}/", UriKind.Absolute);
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                ConnectTimeout = TimeSpan.FromSeconds(5),
                MaxConnectionsPerServer = 4,
                AutomaticDecompression = DecompressionMethods.All
            })
            .AddStandardResilienceHandler();

        services.AddHttpClient<IGeocodingService, GeocodingService>((sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<NominatimOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/", UriKind.Absolute);
                client.Timeout = TimeSpan.FromSeconds(15);
                client.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                ConnectTimeout = TimeSpan.FromSeconds(5),
                MaxConnectionsPerServer = 1,
                AutomaticDecompression = DecompressionMethods.All
            })
            .AddStandardResilienceHandler();

        return services;
    }
}

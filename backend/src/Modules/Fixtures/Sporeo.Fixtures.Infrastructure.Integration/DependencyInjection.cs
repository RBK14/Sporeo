using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Timeout;
using Sporeo.Fixtures.Application.Venues.Abstractions;
using Sporeo.Fixtures.Application.Fixtures.Abstractions;
using Sporeo.Fixtures.Infrastructure.Integration.Configuration;
using Sporeo.Fixtures.Infrastructure.Integration.Providers.TheSportsDb;
using System.Net;
using System.Net.Http.Headers;
using System.Threading.RateLimiting;
using Sporeo.Fixtures.Application.Catalogs.Abstractions;
using Sporeo.Fixtures.Infrastructure.Integration.Providers.Nominatim;

namespace Sporeo.Fixtures.Infrastructure.Integration;

/// <summary>
/// Registers external integration services for fixture providers and geocoding.
/// </summary>
public static class DependencyInjection
{
    private static readonly TimeSpan TotalRequestTimeout = TimeSpan.FromSeconds(90);
    private static readonly TimeSpan AttemptTimeout = TimeSpan.FromSeconds(5);
    private static readonly string[] PlaceholderUserAgents =
    [
        "contact@example.com",
        "example.com"
    ];

    /// <summary>
    /// Adds typed HTTP clients and options for TheSportsDB and Nominatim.
    /// Prefer <see cref="AddExternalFixtures"/> / <see cref="AddGeocoding"/> when only one side is needed.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The same <paramref name="services"/> instance for chaining.</returns>
    public static IServiceCollection AddIntegration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddExternalFixtures(configuration);
        services.AddGeocoding(configuration);

        return services;
    }

    /// <summary>
    /// Adds TheSportsDB HTTP client for fixtures and catalog providers.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The same <paramref name="services"/> instance for chaining.</returns>
    public static IServiceCollection AddExternalFixtures(
        this IServiceCollection services,
        IConfiguration configuration)
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

        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<TheSportsDbOptions>>().Value;

            return new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = options.RequestsPerMinute,
                TokensPerPeriod = options.RequestsPerMinute,
                ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                QueueLimit = 1000,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            });
        });

        services.AddTransient<TheSportsDbRateLimiter>();
        services.AddTransient<TheSportsDbApiKeyHandler>();
        services.AddSingleton<TheSportsDbFixtureMapper>();

        services.AddHttpClient<TheSportsDbApi>((sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<TheSportsDbOptions>>().Value;
                var baseUrl = options.BaseUrl.TrimEnd('/') + "/";

                client.BaseAddress = new Uri($"{baseUrl}/[API_KEY]/", UriKind.Absolute);

                // Polly owns both timeout scopes; HttpClient.Timeout must not compete with them.
                client.Timeout = Timeout.InfiniteTimeSpan;
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            })
            .AddHttpMessageHandler<TheSportsDbRateLimiter>()
            .AddHttpMessageHandler<TheSportsDbApiKeyHandler>()
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                ConnectTimeout = TimeSpan.FromSeconds(5),
                MaxConnectionsPerServer = 4,
                AutomaticDecompression = DecompressionMethods.All
            })
            .AddStandardResilienceHandler(options =>
            {
                options.RateLimiter.DefaultRateLimiterOptions = new ConcurrencyLimiterOptions
                {
                    PermitLimit = 4,
                    QueueLimit = 10,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                };

                options.TotalRequestTimeout.Timeout = TotalRequestTimeout;

                options.Retry.ShouldHandle = CreateTransientHttpPredicate();
                options.Retry.MaxRetryAttempts = 3;
                options.Retry.Delay = TimeSpan.FromSeconds(1);
                options.Retry.BackoffType = DelayBackoffType.Exponential;
                options.Retry.UseJitter = true;
                options.Retry.ShouldRetryAfterHeader = true;

                options.CircuitBreaker.ShouldHandle = CreateCircuitBreakerPredicate();
                options.CircuitBreaker.FailureRatio = 0.5;
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
                options.CircuitBreaker.MinimumThroughput = 10;
                options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);

                options.AttemptTimeout.Timeout = AttemptTimeout;
            });

        // Typed HttpClient is TheSportsDbApi; the facade resolves through it so BaseAddress
        // and TheSportsDbApiKeyHandler are applied.
        services.AddTransient<TheSportsDbClient>();
        services.AddTransient<IExternalFixturesClient>(sp => sp.GetRequiredService<TheSportsDbClient>());
        services.AddTransient<IExternalCatalogClient>(sp => sp.GetRequiredService<TheSportsDbClient>());

        return services;
    }

    /// <summary>
    /// Adds the Nominatim geocoding client and its rate limiter.
    /// Requires <c>AddCaching</c> so <see cref="IGeocodingService"/> can resolve <c>ICacheService</c>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The same <paramref name="services"/> instance for chaining.</returns>
    public static IServiceCollection AddGeocoding(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);

        services.AddOptions<NominatimOptions>()
            .Bind(configuration.GetSection(NominatimOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(
                options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var uri) &&
                           uri.Scheme == Uri.UriSchemeHttps,
                "ExternalProviders:Nominatim requires an absolute HTTPS BaseUrl.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.UserAgent) &&
                           !PlaceholderUserAgents.Any(placeholder =>
                               options.UserAgent.Contains(placeholder, StringComparison.OrdinalIgnoreCase)),
                "ExternalProviders:Nominatim requires a non-placeholder identifying User-Agent.")
            .Validate(
                options => options.MinRequestInterval >= TimeSpan.FromSeconds(1),
                "ExternalProviders:Nominatim MinRequestInterval must be at least 1 second.")
            .Validate(
                options => options.FoundCacheTtl > TimeSpan.Zero && options.MissCacheTtl > TimeSpan.Zero,
                "ExternalProviders:Nominatim cache TTLs must be greater than zero.")
            .ValidateOnStart();

        services.AddSingleton<IGeocodingRateLimiter, NominatimRateLimiter>();

        services.AddHttpClient<IGeocodingService, NominatimGeocodingService>((sp, client) =>
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
            });
        // No standard resilience retries: Nominatim cadence is owned by GeocodingRateLimiter.

        return services;
    }

    private static PredicateBuilder<HttpResponseMessage> CreateTransientHttpPredicate() =>
        new PredicateBuilder<HttpResponseMessage>()
            .Handle<HttpRequestException>()
            .HandleResult(static response => response.StatusCode is
                HttpStatusCode.InternalServerError or
                HttpStatusCode.BadGateway or
                HttpStatusCode.ServiceUnavailable or
                HttpStatusCode.GatewayTimeout or
                HttpStatusCode.TooManyRequests);

    private static PredicateBuilder<HttpResponseMessage> CreateCircuitBreakerPredicate() =>
        CreateTransientHttpPredicate()
            .Handle<TimeoutRejectedException>();
}

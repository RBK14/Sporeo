using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Sporeo.Fixtures.Application.Venues.Abstractions.Geocoding;
using Sporeo.Fixtures.Domain.Venues.ValueObjects;
using Sporeo.Fixtures.Infrastructure.Integration.Configuration;
using Sporeo.Fixtures.Infrastructure.Integration.Geocoding;

namespace Sporeo.Fixtures.Infrastructure.Integration.Tests.Geocoding;

public sealed class GeocodingServiceTests
{
    [Fact]
    public async Task GetVenueLocationAsync_WhenCacheHit_ShouldSkipHttpAndRateLimiter()
    {
        var coordinates = Coordinates.Create(52.2, 21.0).Value;
        var cached = new GeocodedLocation(coordinates, Address.Create(null, "Warsaw", "Poland").Value);
        var cache = Substitute.For<IGeocodingCache>();
        cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new GeocodingCacheLookup(true, cached));

        var rateLimiter = Substitute.For<IGeocodingRateLimiter>();
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var sut = CreateSut(handler, cache, rateLimiter);

        var result = await sut.GetVenueLocationAsync("National Stadium", null, "Warsaw", "Poland");

        result.IsSuccess.Should().BeTrue();
        result.Value.Coordinates.Should().Be(coordinates);
        handler.CallCount.Should().Be(0);
        await rateLimiter.DidNotReceiveWithAnyArgs().WaitAsync(default);
    }

    [Fact]
    public async Task GetVenueLocationAsync_WhenCacheMiss_ShouldSkipHttpAndReturnNotFound()
    {
        var cache = Substitute.For<IGeocodingCache>();
        cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new GeocodingCacheLookup(false, null));

        var rateLimiter = Substitute.For<IGeocodingRateLimiter>();
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var sut = CreateSut(handler, cache, rateLimiter);

        var result = await sut.GetVenueLocationAsync("Unknown Place", null, null, "Atlantis");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Geocoding.NotFound");
        handler.CallCount.Should().Be(0);
        await rateLimiter.DidNotReceiveWithAnyArgs().WaitAsync(default);
    }

    [Fact]
    public async Task GetVenueLocationAsync_WhenEmptyResults_ShouldCacheNegativeResult()
    {
        var cache = Substitute.For<IGeocodingCache>();
        cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((GeocodingCacheLookup?)null);

        var rateLimiter = Substitute.For<IGeocodingRateLimiter>();
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[]", Encoding.UTF8, "application/json")
        });
        var sut = CreateSut(handler, cache, rateLimiter);

        var result = await sut.GetVenueLocationAsync("Ghost Arena", null, "Nowhere", "Poland");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Geocoding.NotFound");
        await cache.Received(1).SetMissAsync(
            Arg.Is<string>(key => key.Contains("ghost arena", StringComparison.Ordinal)),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>());
        await cache.DidNotReceiveWithAnyArgs().SetFoundAsync(default!, default!, default, default);
    }

    [Fact]
    public async Task GetVenueLocationAsync_WhenFound_ShouldParseResultCacheAndSendUserAgent()
    {
        var cache = Substitute.For<IGeocodingCache>();
        cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((GeocodingCacheLookup?)null);

        var rateLimiter = Substitute.For<IGeocodingRateLimiter>();
        HttpRequestMessage? captured = null;
        var handler = new RecordingHandler(request =>
        {
            captured = request;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """[{"lat":"52.2297","lon":"21.0122","address":{"road":"Main","city":"Warsaw","country":"Poland"}}]""",
                    Encoding.UTF8,
                    "application/json")
            };
        });

        var sut = CreateSut(handler, cache, rateLimiter);
        var result = await sut.GetVenueLocationAsync("National Stadium", "Main", "Warsaw", "Poland");

        result.IsSuccess.Should().BeTrue();
        result.Value.Coordinates.Latitude.Should().BeApproximately(52.2297, 0.0001);
        handler.CallCount.Should().Be(1);
        captured.Should().NotBeNull();
        captured!.RequestUri!.ToString().Should().Contain("q=");
        await rateLimiter.Received(1).WaitAsync(Arg.Any<CancellationToken>());
        await cache.Received(1).SetFoundAsync(
            Arg.Any<string>(),
            Arg.Any<GeocodedLocation>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetVenueLocationAsync_With429_ShouldReturnRateLimitedWithoutCaching()
    {
        var cache = Substitute.For<IGeocodingCache>();
        cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((GeocodingCacheLookup?)null);

        var rateLimiter = Substitute.For<IGeocodingRateLimiter>();
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.TooManyRequests));
        var sut = CreateSut(handler, cache, rateLimiter);

        var result = await sut.GetVenueLocationAsync("National Stadium", null, "Warsaw", "Poland");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Geocoding.RateLimited");
        await cache.DidNotReceiveWithAnyArgs().SetMissAsync(default!, default, default);
        await cache.DidNotReceiveWithAnyArgs().SetFoundAsync(default!, default!, default, default);
    }

    private static NominatimGeocodingService CreateSut(
        HttpMessageHandler handler,
        IGeocodingCache cache,
        IGeocodingRateLimiter rateLimiter)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://nominatim.openstreetmap.org/")
        };
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Sporeo/1.0 (fixtures-worker; contact@sporeo.app)");

        var options = Options.Create(new NominatimOptions
        {
            BaseUrl = "https://nominatim.openstreetmap.org",
            UserAgent = "Sporeo/1.0 (fixtures-worker; contact@sporeo.app)",
            MinRequestInterval = TimeSpan.FromSeconds(1),
            FoundCacheTtl = TimeSpan.FromDays(90),
            MissCacheTtl = TimeSpan.FromDays(30)
        });

        return new NominatimGeocodingService(
            httpClient,
            cache,
            rateLimiter,
            options,
            new FakeTimeProvider(DateTimeOffset.UtcNow),
            NullLogger<NominatimGeocodingService>.Instance);
    }

    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> factory) : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(factory(request));
        }
    }
}

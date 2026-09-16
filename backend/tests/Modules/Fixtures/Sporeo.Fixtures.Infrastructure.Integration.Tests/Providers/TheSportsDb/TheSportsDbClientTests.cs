using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Sporeo.Fixtures.Application.Fixtures.Abstractions.Providers;
using Sporeo.Fixtures.Domain.Fixtures.Enums;
using Sporeo.Fixtures.Infrastructure.Integration.Providers.TheSportsDb;

namespace Sporeo.Fixtures.Infrastructure.Integration.Tests.Providers.TheSportsDb;

public sealed class TheSportsDbClientTests
{
    [Fact]
    public async Task FetchFixturesAsync_WithEmptyEvents_ShouldReturnEmptySuccess()
    {
        using var httpClient = CreateClient("""{"events":null}""", HttpStatusCode.OK);
        var sut = new TheSportsDbClient(httpClient, NullLogger<TheSportsDbClient>.Instance);

        var result = await sut.FetchFixturesAsync("4328", null, SyncMode.ShortTerm);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task FetchFixturesAsync_WithEmptyEventsArray_ShouldReturnEmptySuccess()
    {
        using var httpClient = CreateClient("""{"events":[]}""", HttpStatusCode.OK);
        var sut = new TheSportsDbClient(httpClient, NullLogger<TheSportsDbClient>.Instance);

        var result = await sut.FetchFixturesAsync("4328", "2025-2026", SyncMode.LongTerm);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task FetchFixturesAsync_WithUnauthorized_ShouldReturnTypedFailure()
    {
        using var httpClient = CreateClient("unauthorized", HttpStatusCode.Unauthorized);
        var sut = new TheSportsDbClient(httpClient, NullLogger<TheSportsDbClient>.Instance);

        var result = await sut.FetchFixturesAsync("4328", null, SyncMode.ShortTerm);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ExternalFixtures.Unauthorized");
    }

    [Fact]
    public async Task FetchFixturesAsync_WithTooManyRequests_ShouldReturnRateLimitedFailure()
    {
        using var httpClient = CreateClient("rate limited", HttpStatusCode.TooManyRequests);
        var sut = new TheSportsDbClient(httpClient, NullLogger<TheSportsDbClient>.Instance);

        var result = await sut.FetchFixturesAsync("4328", null, SyncMode.ShortTerm);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ExternalFixtures.RateLimited");
    }

    [Fact]
    public async Task FetchFixturesAsync_WithInvalidJson_ShouldReturnInvalidPayloadFailure()
    {
        using var httpClient = CreateClient("not-json", HttpStatusCode.OK);
        var sut = new TheSportsDbClient(httpClient, NullLogger<TheSportsDbClient>.Instance);

        var result = await sut.FetchFixturesAsync("4328", "2025-2026", SyncMode.LongTerm);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ExternalFixtures.InvalidPayload");
    }

    [Fact]
    public async Task FetchFixturesAsync_WhenHttpRequestException_ShouldReturnTransientFailure()
    {
        using var httpClient = new HttpClient(new ThrowingHandler())
        {
            BaseAddress = new Uri("https://example.test/")
        };
        var sut = new TheSportsDbClient(httpClient, NullLogger<TheSportsDbClient>.Instance);

        var result = await sut.FetchFixturesAsync("4328", "2025-2026", SyncMode.LongTerm);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ExternalFixtures.Transient");
    }

    [Fact]
    public async Task FetchFixturesAsync_WhenCanceled_ShouldPropagateCancellation()
    {
        using var httpClient = new HttpClient(new DelayedHandler())
        {
            BaseAddress = new Uri("https://example.test/")
        };
        var sut = new TheSportsDbClient(httpClient, NullLogger<TheSportsDbClient>.Instance);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var act = async () => await sut.FetchFixturesAsync("4328", null, SyncMode.ShortTerm, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task FetchFixturesAsync_WithValidStrTimestamp_ShouldPreferTimestampOverStrTime()
    {
        var payload = BuildEventsJson(BuildEvent(
            idEvent: "1",
            dateEvent: "2026-03-15",
            strTime: "19:30:00",
            strTimestamp: "2026-03-15T18:30:00+00:00"));

        using var httpClient = CreateClient(payload, HttpStatusCode.OK);
        var sut = new TheSportsDbClient(httpClient, NullLogger<TheSportsDbClient>.Instance);

        var result = await sut.FetchFixturesAsync("4328", "2025-2026", SyncMode.LongTerm);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value[0].StartDate.Should().Be(new DateTimeOffset(2026, 3, 15, 18, 30, 0, TimeSpan.Zero));
    }

    [Theory]
    [InlineData("2026-03-15T18:30:00Z")]
    [InlineData("2026-03-15T18:30:00")]
    [InlineData("2026-03-15 18:30:00")]
    public async Task FetchFixturesAsync_WithSupportedTimestampFormats_ShouldParseAsUtc(string strTimestamp)
    {
        var payload = BuildEventsJson(BuildEvent(strTimestamp: strTimestamp, strTime: null));

        using var httpClient = CreateClient(payload, HttpStatusCode.OK);
        var sut = new TheSportsDbClient(httpClient, NullLogger<TheSportsDbClient>.Instance);

        var result = await sut.FetchFixturesAsync("4328", "2025-2026", SyncMode.LongTerm);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value[0].StartDate.Should().Be(new DateTimeOffset(2026, 3, 15, 18, 30, 0, TimeSpan.Zero));
        result.Value[0].StartDate.Offset.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public async Task FetchFixturesAsync_WhenStrTimestampMissing_ShouldFallbackToDateEventAndStrTime()
    {
        var payload = BuildEventsJson(BuildEvent(strTimestamp: null, dateEvent: "2026-03-15", strTime: "18:30:00"));

        using var httpClient = CreateClient(payload, HttpStatusCode.OK);
        var sut = new TheSportsDbClient(httpClient, NullLogger<TheSportsDbClient>.Instance);

        var result = await sut.FetchFixturesAsync("4328", "2025-2026", SyncMode.LongTerm);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value[0].StartDate.Should().Be(new DateTimeOffset(2026, 3, 15, 18, 30, 0, TimeSpan.Zero));
    }

    [Fact]
    public async Task FetchFixturesAsync_WhenStrTimestampInvalid_ShouldFallbackToDateEventAndStrTime()
    {
        var payload = BuildEventsJson(BuildEvent(
            strTimestamp: "not-a-date",
            dateEvent: "2026-03-15",
            strTime: "18:30:00"));

        using var httpClient = CreateClient(payload, HttpStatusCode.OK);
        var sut = new TheSportsDbClient(httpClient, NullLogger<TheSportsDbClient>.Instance);

        var result = await sut.FetchFixturesAsync("4328", "2025-2026", SyncMode.LongTerm);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value[0].StartDate.Should().Be(new DateTimeOffset(2026, 3, 15, 18, 30, 0, TimeSpan.Zero));
    }

    [Fact]
    public async Task FetchFixturesAsync_WhenTimestampAndFallbackInvalid_ShouldReturnInvalidPayload()
    {
        var payload = BuildEventsJson(BuildEvent(
            strTimestamp: "bad",
            dateEvent: "bad",
            strTime: "bad"));

        using var httpClient = CreateClient(payload, HttpStatusCode.OK);
        var sut = new TheSportsDbClient(httpClient, NullLogger<TheSportsDbClient>.Instance);

        var result = await sut.FetchFixturesAsync("4328", "2025-2026", SyncMode.LongTerm);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ExternalFixtures.InvalidPayload");
    }

    [Fact]
    public async Task FetchFixturesAsync_WhenAllEventsInvalid_ShouldReturnInvalidPayloadFailure()
    {
        var payload = BuildEventsJson(
            BuildEvent(idEvent: null, strTimestamp: "2026-03-15T18:30:00Z"),
            BuildEvent(idEvent: "2", strTimestamp: "bad", dateEvent: "bad", strTime: "bad"));

        using var httpClient = CreateClient(payload, HttpStatusCode.OK);
        var sut = new TheSportsDbClient(httpClient, NullLogger<TheSportsDbClient>.Instance);

        var result = await sut.FetchFixturesAsync("4328", "2025-2026", SyncMode.LongTerm);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ExternalFixtures.InvalidPayload");
    }

    [Fact]
    public async Task FetchFixturesAsync_WhenMixedValidAndInvalidEvents_ShouldReturnValidOnly()
    {
        var payload = BuildEventsJson(
            BuildEvent(idEvent: "1", strTimestamp: "2026-03-15T18:30:00Z"),
            BuildEvent(idEvent: null, strTimestamp: "2026-03-16T18:30:00Z"));

        using var httpClient = CreateClient(payload, HttpStatusCode.OK);
        var sut = new TheSportsDbClient(httpClient, NullLogger<TheSportsDbClient>.Instance);

        var result = await sut.FetchFixturesAsync("4328", "2025-2026", SyncMode.LongTerm);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value[0].ProviderId.Should().Be("1");
    }

    [Fact]
    public async Task FetchFixturesAsync_WithVenueAndStrCountry_ShouldMapCountryToVenueDto()
    {
        var payload = BuildEventsJson(BuildEvent(
            idVenue: "v1",
            strVenue: "Emirates Stadium",
            strCountry: "England"));

        using var httpClient = CreateClient(payload, HttpStatusCode.OK);
        var sut = new TheSportsDbClient(httpClient, NullLogger<TheSportsDbClient>.Instance);

        var result = await sut.FetchFixturesAsync("4328", "2025-2026", SyncMode.LongTerm);

        result.IsSuccess.Should().BeTrue();
        var venue = result.Value[0].Venue;
        venue.Should().NotBeNull();
        venue!.Country.Should().Be("England");
        venue.City.Should().BeNull();
        venue.Street.Should().BeNull();
    }

    [Fact]
    public async Task FetchFixturesAsync_WithStrCountryOnlyAndNoVenueIds_ShouldReturnFixtureWithoutVenue()
    {
        var payload = BuildEventsJson(BuildEvent(strCountry: "England"));

        using var httpClient = CreateClient(payload, HttpStatusCode.OK);
        var sut = new TheSportsDbClient(httpClient, NullLogger<TheSportsDbClient>.Instance);

        var result = await sut.FetchFixturesAsync("4328", "2025-2026", SyncMode.LongTerm);

        result.IsSuccess.Should().BeTrue();
        result.Value[0].Venue.Should().BeNull();
    }

    [Theory]
    [InlineData("Match Finished", FixtureStatus.Finished)]
    [InlineData("Not Started", FixtureStatus.Scheduled)]
    [InlineData("Postponed", FixtureStatus.Postponed)]
    [InlineData("Cancelled", FixtureStatus.Cancelled)]
    [InlineData("Half Time", FixtureStatus.Scheduled)]
    [InlineData(null, FixtureStatus.Scheduled)]
    public async Task FetchFixturesAsync_WithStatus_ShouldMapCorrectly(string? strStatus, FixtureStatus expected)
    {
        var payload = BuildEventsJson(BuildEvent(strStatus: strStatus));

        using var httpClient = CreateClient(payload, HttpStatusCode.OK);
        var sut = new TheSportsDbClient(httpClient, NullLogger<TheSportsDbClient>.Instance);

        var result = await sut.FetchFixturesAsync("4328", "2025-2026", SyncMode.LongTerm);

        result.IsSuccess.Should().BeTrue();
        result.Value[0].Status.Should().Be(expected);
    }

    private static string BuildEventsJson(params string[] events) =>
        $$"""{"events":[{{string.Join(",", events)}}]}""";

    private static string BuildEvent(
        string? idEvent = "1",
        string? strEvent = "Home vs Away",
        string? dateEvent = "2026-03-15",
        string? strTimestamp = "2026-03-15T18:30:00Z",
        string? strTime = null,
        string? strStatus = "Not Started",
        string? idVenue = null,
        string? strVenue = null,
        string? strCountry = null)
    {
        var parts = new List<string>();

        if (idEvent is not null)
            parts.Add($""" "idEvent":"{idEvent}" """);
        if (strEvent is not null)
            parts.Add($""" "strEvent":"{strEvent}" """);
        if (dateEvent is not null)
            parts.Add($""" "dateEvent":"{dateEvent}" """);
        if (strTimestamp is not null)
            parts.Add($""" "strTimestamp":"{strTimestamp}" """);
        if (strTime is not null)
            parts.Add($""" "strTime":"{strTime}" """);
        if (strStatus is not null)
            parts.Add($""" "strStatus":"{strStatus}" """);
        if (idVenue is not null)
            parts.Add($""" "idVenue":"{idVenue}" """);
        if (strVenue is not null)
            parts.Add($""" "strVenue":"{strVenue}" """);
        if (strCountry is not null)
            parts.Add($""" "strCountry":"{strCountry}" """);

        return "{" + string.Join(",", parts) + "}";
    }

    private sealed class StubHandler(Func<HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(responseFactory());
    }

    private static HttpClient CreateClient(string content, HttpStatusCode statusCode)
    {
        var handler = new StubHandler(() => new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(content, Encoding.UTF8, "application/json")
        });

        return new HttpClient(handler)
        {
            BaseAddress = new Uri("https://example.test/")
        };
    }

    private sealed class DelayedHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            throw new HttpRequestException("network down");
    }
}

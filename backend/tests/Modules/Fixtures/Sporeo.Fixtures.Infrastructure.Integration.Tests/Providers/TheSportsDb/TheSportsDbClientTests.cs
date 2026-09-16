using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Sporeo.Fixtures.Application.Abstractions.Providers;
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
}

using FluentAssertions;
using Sporeo.Fixtures.Infrastructure.Integration.Configuration;
using Sporeo.Fixtures.Infrastructure.Integration.Providers.TheSportsDb;
using Sporeo.ServiceDefaults.Telemetry;

namespace Sporeo.Fixtures.Infrastructure.Integration.Tests.Security;

public sealed class SensitiveDataRedactionTests
{
    [Fact]
    public void SensitiveUrlRedactor_RedactsTheSportsDbApiKeyAndNominatimQuery()
    {
        var sportsDbUrl = "https://www.thesportsdb.com/api/v1/json/super-secret-key/eventspastleague.php?id=4328";
        var nominatimUrl = "https://nominatim.openstreetmap.org/search?q=National%20Stadium%2C%20Warsaw&format=json";

        SensitiveUrlRedactor.Redact(sportsDbUrl).Should().Be(
            "https://www.thesportsdb.com/api/v1/json/***/eventspastleague.php?id=4328");
        SensitiveUrlRedactor.Redact(nominatimUrl).Should().Be(
            "https://nominatim.openstreetmap.org/search?q=REDACTED&format=json");
    }

    [Fact]
    public async Task TheSportsDbApiKeyHandler_InsertsKeyIntoRelativePath()
    {
        HttpRequestMessage? captured = null;
        var inner = new CaptureHandler(request =>
        {
            captured = request;
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
        });

        var handler = new TheSportsDbApiKeyHandler(
            Microsoft.Extensions.Options.Options.Create(new TheSportsDbOptions
            {
                BaseUrl = "https://www.thesportsdb.com/api/v1/json",
                ApiKey = "super-secret-key"
            }))
        {
            InnerHandler = inner
        };

        using var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://www.thesportsdb.com/api/v1/json/")
        };

        await client.GetAsync("eventspastleague.php?id=4328");

        captured.Should().NotBeNull();
        captured!.RequestUri!.ToString().Should().Contain("super-secret-key/");
        client.BaseAddress!.AbsoluteUri.Should().NotContain("super-secret-key");
    }

    private sealed class CaptureHandler(Func<HttpRequestMessage, HttpResponseMessage> factory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(factory(request));
    }
}

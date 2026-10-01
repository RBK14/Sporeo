using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Sporeo.Fixtures.Api.Tests.Endpoints;

public sealed class HealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:fixtures-db", "Server=127.0.0.1,1433;Database=fixtures;User ID=sa;Password=placeholder;TrustServerCertificate=true");
            builder.UseSetting("ConnectionStrings:redis", "localhost:6379");
            builder.UseSetting("ExternalProviders:TheSportsDb:BaseUrl", "https://www.thesportsdb.com/api/v1/json/");
            builder.UseSetting("ExternalProviders:TheSportsDb:ApiKey", "test-api-key");
            builder.UseSetting("ExternalProviders:TheSportsDb:RequestsPerMinute", "30");
            builder.UseSetting("ExternalProviders:Nominatim:BaseUrl", "https://nominatim.openstreetmap.org");
            builder.UseSetting("ExternalProviders:Nominatim:UserAgent", "Sporeo/1.0 (fixtures-api; contact@sporeo.app)");
            builder.UseSetting("ExternalProviders:Nominatim:MinRequestInterval", "00:00:01");
            builder.UseSetting("ExternalProviders:Nominatim:FoundCacheTtl", "90.00:00:00");
            builder.UseSetting("ExternalProviders:Nominatim:MissCacheTtl", "30.00:00:00");
            builder.UseSetting("ApiDocumentation:Enabled", "false");
        });
    }

    [Fact]
    public async Task Alive_ShouldReturnSuccess()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/alive");

        response.IsSuccessStatusCode.Should().BeTrue();
    }
}

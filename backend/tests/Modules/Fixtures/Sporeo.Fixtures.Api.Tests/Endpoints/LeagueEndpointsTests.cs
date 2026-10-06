using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Sporeo.Fixtures.Api.Tests.Endpoints;

public sealed class LeagueEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public LeagueEndpointsTests(WebApplicationFactory<Program> factory)
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
    public async Task GetActiveLeagues_WithEmptySportId_ShouldReturnValidationProblem()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/leagues?sportId={Guid.Empty}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("errors").TryGetProperty("sportId", out _).Should().BeTrue();
        body.RootElement.GetProperty("codes").EnumerateArray()
            .Select(code => code.GetString())
            .Should().Contain("League.InvalidSportId");
    }
}

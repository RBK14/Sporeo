using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Sporeo.Fixtures.Api.OpenApi;
using Sporeo.Fixtures.Api.OpenApi.Transformers;

namespace Sporeo.Fixtures.Api.Tests.OpenApi;

public class OpenApiDocumentationTests
{
    [Fact]
    public async Task BearerSecuritySchemeDocumentTransformer_ShouldRegisterHttpBearerScheme()
    {
        var options = Options.Create(CreateValidOptions());
        var transformer = new BearerSecuritySchemeDocumentTransformer(options);
        var document = new OpenApiDocument();

        await transformer.TransformAsync(
            document,
            new OpenApiDocumentTransformerContext
            {
                DocumentName = OpenApiDocumentationExtensions.DocumentName,
                DescriptionGroups = [],
                ApplicationServices = new ServiceCollection().BuildServiceProvider()
            },
            CancellationToken.None);

        document.Components.Should().NotBeNull();
        document.Components!.SecuritySchemes.Should().ContainKey("BearerAuth");

        var scheme = document.Components.SecuritySchemes["BearerAuth"].Should().BeOfType<OpenApiSecurityScheme>().Subject;
        scheme.Type.Should().Be(SecuritySchemeType.Http);
        scheme.Scheme.Should().Be("bearer");
        scheme.BearerFormat.Should().Be("JWT");
        scheme.In.Should().Be(ParameterLocation.Header);
        scheme.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ApiDocumentInfoTransformer_ShouldApplyInfoAndServersFromOptions()
    {
        var options = Options.Create(new ApiDocumentationOptions
        {
            Title = "Fixtures Test API",
            Description = "Test description",
            ContactName = "Platform",
            ContactEmail = "contact@sporeo.app",
            ContactUrl = "https://sporeo.app",
            LicenseName = "Proprietary",
            LicenseUrl = "https://sporeo.app/license",
            Servers =
            [
                new ApiServerOptions
                {
                    Url = "https://api.sporeo.app",
                    Description = "Production"
                }
            ],
            Security = new SecurityOptions()
        });

        var transformer = new ApiDocumentInfoTransformer(options);
        var document = new OpenApiDocument
        {
            Info = new OpenApiInfo()
        };

        await transformer.TransformAsync(
            document,
            new OpenApiDocumentTransformerContext
            {
                DocumentName = OpenApiDocumentationExtensions.DocumentName,
                DescriptionGroups = [],
                ApplicationServices = new ServiceCollection().BuildServiceProvider()
            },
            CancellationToken.None);

        document.Info.Title.Should().Be("Fixtures Test API");
        document.Info.Description.Should().Be("Test description");
        document.Info.Contact.Should().NotBeNull();
        document.Info.Contact!.Name.Should().Be("Platform");
        document.Info.Contact.Email.Should().Be("contact@sporeo.app");
        document.Info.Contact.Url.Should().Be(new Uri("https://sporeo.app"));
        document.Info.License.Should().NotBeNull();
        document.Info.License!.Name.Should().Be("Proprietary");
        document.Info.License.Url.Should().Be(new Uri("https://sporeo.app/license"));
        document.Servers.Should().ContainSingle();
        document.Servers![0].Url.Should().Be("https://api.sporeo.app");
        document.Servers[0].Description.Should().Be("Production");
    }

    [Fact]
    public void ApiDocumentationOptions_ShouldFailValidation_WhenTitleIsMissing()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["ApiDocumentation:Title"] = "",
            ["ApiDocumentation:Security:SchemeName"] = "BearerAuth",
            ["ApiDocumentation:Security:BearerFormat"] = "JWT"
        });

        var services = new ServiceCollection();
        services.AddOpenApiDocumentation(configuration);
        using var provider = services.BuildServiceProvider();

        var act = () => provider.GetRequiredService<IOptions<ApiDocumentationOptions>>().Value;

        act.Should().Throw<OptionsValidationException>();
    }

    [Fact]
    public void ApiDocumentationOptions_ShouldFailValidation_WhenServerUrlIsInvalid()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["ApiDocumentation:Title"] = "Sporeo Fixtures API",
            ["ApiDocumentation:Servers:0:Url"] = "not-a-url",
            ["ApiDocumentation:Security:SchemeName"] = "BearerAuth",
            ["ApiDocumentation:Security:BearerFormat"] = "JWT"
        });

        var services = new ServiceCollection();
        services.AddOpenApiDocumentation(configuration);
        using var provider = services.BuildServiceProvider();

        var act = () => provider.GetRequiredService<IOptions<ApiDocumentationOptions>>().Value;

        act.Should().Throw<OptionsValidationException>();
    }

    [Fact]
    public async Task MapOpenApiDocumentation_ShouldExposeDocumentAndScalarUi()
    {
        await using var app = await CreateDocumentationHostAsync(environmentName: Environments.Production, enabled: true);
        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };

        using var openApiResponse = await client.GetAsync("/openapi/v1.json");
        openApiResponse.EnsureSuccessStatusCode();

        await using var stream = await openApiResponse.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        var root = document.RootElement;

        root.GetProperty("info").GetProperty("title").GetString().Should().Be("Sporeo Fixtures API");
        root.GetProperty("components")
            .GetProperty("securitySchemes")
            .GetProperty("BearerAuth")
            .GetProperty("scheme")
            .GetString()
            .Should().Be("bearer");

        using var scalarResponse = await client.GetAsync("/scalar/v1");
        scalarResponse.EnsureSuccessStatusCode();
        var scalarHtml = await scalarResponse.Content.ReadAsStringAsync();
        scalarHtml.Should().Contain("scalar", because: "Scalar UI should be served");
    }

    [Fact]
    public async Task MapOpenApiDocumentation_ShouldExposeByDefault_InDevelopment()
    {
        await using var app = await CreateDocumentationHostAsync(environmentName: Environments.Development, enabled: null);
        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };

        using var response = await client.GetAsync("/openapi/v1.json");
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task MapOpenApiDocumentation_ShouldStayHiddenByDefault_OutsideDevelopment()
    {
        await using var app = await CreateDocumentationHostAsync(environmentName: Environments.Production, enabled: null);
        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };

        using var response = await client.GetAsync("/openapi/v1.json");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    private static async Task<WebApplication> CreateDocumentationHostAsync(
        string environmentName,
        bool? enabled)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = environmentName
        });
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        var values = new Dictionary<string, string?>
        {
            ["ApiDocumentation:Title"] = "Sporeo Fixtures API",
            ["ApiDocumentation:Description"] = "Smoke test documentation",
            ["ApiDocumentation:Security:SchemeName"] = "BearerAuth",
            ["ApiDocumentation:Security:BearerFormat"] = "JWT"
        };

        if (enabled is not null)
        {
            values["ApiDocumentation:Enabled"] = enabled.Value ? "true" : "false";
        }

        builder.Configuration.AddInMemoryCollection(values);
        builder.Services.AddOpenApiDocumentation(builder.Configuration);

        var app = builder.Build();
        app.MapOpenApiDocumentation();
        app.MapGet("/api/v1/ping", () => Results.Ok(new { status = "ok" }))
            .WithName("Ping")
            .WithTags("Health");

        await app.StartAsync();
        return app;
    }

    private static ApiDocumentationOptions CreateValidOptions() =>
        new()
        {
            Title = "Sporeo Fixtures API",
            Security = new SecurityOptions
            {
                SchemeName = "BearerAuth",
                BearerFormat = "JWT",
                Description = "JWT Bearer token"
            }
        };

    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
}

using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Sporeo.BuildingBlocks.Application.Abstractions.Caching;
using Sporeo.BuildingBlocks.Infrastructure.Messaging;
using Sporeo.BuildingBlocks.Infrastructure.Messaging.Outbox.Abstractions;
using Sporeo.Fixtures.Application;
using Sporeo.Fixtures.Application.Fixtures.Abstractions.Providers;
using Sporeo.Fixtures.Application.Venues.Abstractions.Geocoding;
using Sporeo.Fixtures.Infrastructure.Integration;
using Sporeo.Fixtures.Infrastructure.Integration.Geocoding;
using Sporeo.Fixtures.Infrastructure.Persistence;
using Sporeo.Fixtures.Worker;
using Sporeo.Fixtures.Worker.Configuration;

namespace Sporeo.Fixtures.Worker.Tests.Composition;

public sealed class WorkerDependencyInjectionTests
{
    [Fact]
    public void ServiceProvider_WithValidateScopesAndValidateOnBuild_ShouldComposeSuccessfully()
    {
        using var host = CreateHost();

        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;

        services.GetRequiredService<ISender>().Should().NotBeNull();
        services.GetRequiredService<IGeocodingService>().Should().NotBeNull();
        services.GetRequiredService<ICacheService>().Should().NotBeNull();
        services.GetRequiredService<IGeocodingRateLimiter>().Should().NotBeNull();
        services.GetRequiredService<IExternalFixturesClient>().Should().NotBeNull();
        services.GetRequiredService<IOutboxProcessor>().Should().NotBeNull();
        services.GetRequiredService<IOptions<SyncJobsRootOptions>>().Value.Jobs.Should().BeEmpty();
    }

    [Fact]
    public void SyncJobs_WithUnknownProvider_ShouldFailValidation()
    {
        using var host = CreateHost(values =>
        {
            values["SyncJobs:Jobs:0:JobId"] = "premier-league";
            values["SyncJobs:Jobs:0:CronSchedule"] = "0 0/15 * * * ?";
            values["SyncJobs:Jobs:0:SyncMode"] = "ShortTerm";
            values["SyncJobs:Jobs:0:ProviderName"] = "UnknownProvider";
            values["SyncJobs:Jobs:0:ExternalLeagueId"] = "4328";
            values["SyncJobs:Jobs:0:SportId"] = "11111111-1111-1111-1111-111111111111";
        });

        var act = () => _ = host.Services.GetRequiredService<IOptions<SyncJobsRootOptions>>().Value;

        act.Should().Throw<OptionsValidationException>()
            .Which.Message.Should().Contain("UnknownProvider");
    }

    private static IHost CreateHost(Action<Dictionary<string, string?>>? configure = null)
    {
        var values = new Dictionary<string, string?>
        {
            ["ConnectionStrings:fixtures-db"] = "Server=127.0.0.1,1433;Database=fixtures;User ID=sa;Password=placeholder;TrustServerCertificate=true",
            ["ConnectionStrings:quartz-db"] = "Server=127.0.0.1,1433;Database=quartz;User ID=sa;Password=placeholder;TrustServerCertificate=true",
            ["ConnectionStrings:redis"] = "localhost:6379",
            ["ExternalProviders:TheSportsDb:BaseUrl"] = "https://www.thesportsdb.com/api/v1/json",
            ["ExternalProviders:TheSportsDb:ApiKey"] = "test-api-key",
            ["ExternalProviders:Nominatim:BaseUrl"] = "https://nominatim.openstreetmap.org",
            ["ExternalProviders:Nominatim:UserAgent"] = "Sporeo/1.0 (fixtures-worker; contact@sporeo.app)",
            ["ExternalProviders:Nominatim:MinRequestInterval"] = "00:00:01",
            ["ExternalProviders:Nominatim:FoundCacheTtl"] = "90.00:00:00",
            ["ExternalProviders:Nominatim:MissCacheTtl"] = "30.00:00:00"
        };

        configure?.Invoke(values);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddConfiguration(configuration);
        builder.ConfigureContainer(new DefaultServiceProviderFactory(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        }));

        builder.Services.AddWorkerConfiguration(builder.Configuration);
        builder.Services.AddApplication();
        builder.Services.AddPersistence(builder.Configuration);
        builder.Services.AddCaching(builder.Configuration);
        builder.Services.AddBuildingBlocksMessaging();
        builder.Services.AddExternalFixtures(builder.Configuration);
        builder.Services.AddGeocoding(builder.Configuration);
        builder.Services.AddWorkerServices(builder.Configuration);

        return builder.Build();
    }
}

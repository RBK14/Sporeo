using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sporeo.BuildingBlocks.Infrastructure.Messaging.Outbox.Abstractions;
using Sporeo.BuildingBlocks.Infrastructure.Messaging.Outbox.Serialization;
using Sporeo.BuildingBlocks.Infrastructure.Persistence.Interceptors;
using Sporeo.Fixtures.Application.Venues.Abstractions.Geocoding;
using Sporeo.Fixtures.Domain.Venues.Events;
using Sporeo.Fixtures.Domain.Venues.ValueObjects;
using Sporeo.Fixtures.Infrastructure.Persistence.Context;
using Sporeo.Fixtures.Infrastructure.Persistence.Geocoding;
using Sporeo.Fixtures.Infrastructure.Persistence.Interceptors;
using Sporeo.Fixtures.Infrastructure.Persistence.Outbox;

namespace Sporeo.Fixtures.Infrastructure.Persistence.Tests.Geocoding;

public sealed class EfCoreGeocodingCacheTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly FixturesDbContext _dbContext;
    private readonly EfCoreGeocodingCache _cache;

    public EfCoreGeocodingCacheTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IDomainEventTypeRegistry>(_ => new DomainEventTypeRegistry(
        [
            new KeyValuePair<string, Type>(FixturesOutboxTypeKeys.VenueCreatedDomainEvent, typeof(VenueCreatedDomainEvent))
        ]));
        services.AddSingleton<AuditableEntityInterceptor>();
        services.AddSingleton<VenueLocationInterceptor>();
        services.AddDbContext<FixturesDbContext>((sp, options) =>
        {
            options.UseInMemoryDatabase(Guid.NewGuid().ToString());
            options.AddInterceptors(
                sp.GetRequiredService<AuditableEntityInterceptor>(),
                sp.GetRequiredService<VenueLocationInterceptor>());
        });
        services.AddScoped<IGeocodingCache, EfCoreGeocodingCache>();

        _serviceProvider = services.BuildServiceProvider();
        _dbContext = _serviceProvider.GetRequiredService<FixturesDbContext>();
        _cache = (EfCoreGeocodingCache)_serviceProvider.GetRequiredService<IGeocodingCache>();
    }

    [Fact]
    public async Task SetFoundAsync_ThenGetAsync_ReturnsCachedLocation()
    {
        var location = new GeocodedLocation(
            Coordinates.Create(52.2297, 21.0122).Value,
            Address.Create("Main", "Warsaw", "Poland").Value);

        await _cache.SetFoundAsync("national stadium, warsaw, poland", location, DateTimeOffset.UtcNow.AddDays(30));

        var lookup = await _cache.GetAsync("national stadium, warsaw, poland");

        lookup.Should().NotBeNull();
        lookup!.IsFound.Should().BeTrue();
        lookup.Location.Should().NotBeNull();
        lookup.Location!.Coordinates.Latitude.Should().BeApproximately(52.2297, 0.0001);
    }

    [Fact]
    public async Task SetMissAsync_ThenGetAsync_ReturnsNegativeLookup()
    {
        await _cache.SetMissAsync("ghost arena, atlantis", DateTimeOffset.UtcNow.AddDays(7));

        var lookup = await _cache.GetAsync("ghost arena, atlantis");

        lookup.Should().NotBeNull();
        lookup!.IsFound.Should().BeFalse();
        lookup.Location.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_WhenExpired_ReturnsNull()
    {
        await _cache.SetMissAsync("expired address", DateTimeOffset.UtcNow.AddMinutes(-1));

        var lookup = await _cache.GetAsync("expired address");

        lookup.Should().BeNull();
    }

    [Fact]
    public async Task SetFoundAsync_UpsertsExistingMiss()
    {
        const string key = "stadium, warsaw, poland";
        await _cache.SetMissAsync(key, DateTimeOffset.UtcNow.AddDays(7));

        var location = new GeocodedLocation(Coordinates.Create(52.2, 21.0).Value, null);
        await _cache.SetFoundAsync(key, location, DateTimeOffset.UtcNow.AddDays(30));

        var entries = await _dbContext.GeocodingCacheEntries.CountAsync();
        var lookup = await _cache.GetAsync(key);

        entries.Should().Be(1);
        lookup!.IsFound.Should().BeTrue();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _serviceProvider.Dispose();
    }
}

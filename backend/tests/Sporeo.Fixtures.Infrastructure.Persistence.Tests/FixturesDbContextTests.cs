using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NetTopologySuite.Geometries;
using Sporeo.BuildingBlocks.Application.Abstractions.Data;
using Sporeo.BuildingBlocks.Domain.Time;
using Sporeo.BuildingBlocks.Infrastructure.Persistence;
using Sporeo.Fixtures.Application;
using Sporeo.Fixtures.Domain.Venues;
using Sporeo.Fixtures.Infrastructure.Persistence;
using DomainCoordinates = Sporeo.Fixtures.Domain.Venues.ValueObjects.Coordinates;

namespace Sporeo.Fixtures.Infrastructure.Persistence.Tests;

public sealed class FixturesDbContextTests : IDisposable
{
    private readonly TimeProvider _originalTimeProvider;
    private readonly ServiceProvider _serviceProvider;
    private readonly FixturesDbContext _dbContext;

    public FixturesDbContextTests()
    {
        _originalTimeProvider = SystemTimeProvider.Provider;

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(FixturesDbContextTests).Assembly));
        services.AddSingleton<AuditableEntityInterceptor>();
        services.AddSingleton<VenueLocationInterceptor>();
        services.AddDbContext<FixturesDbContext>((sp, options) =>
        {
            options.UseInMemoryDatabase(Guid.NewGuid().ToString());
            options.AddInterceptors(
                sp.GetRequiredService<AuditableEntityInterceptor>(),
                sp.GetRequiredService<VenueLocationInterceptor>());
        });
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<FixturesDbContext>());

        _serviceProvider = services.BuildServiceProvider();
        _dbContext = _serviceProvider.GetRequiredService<FixturesDbContext>();
    }

    [Fact]
    public void Model_HasLocationGeographyShadowProperty()
    {
        using var relationalContext = CreateRelationalModelContext();
        var entityType = relationalContext.Model.FindEntityType(typeof(Venue));
        entityType.Should().NotBeNull();

        var location = entityType!.FindProperty("Location");
        location.Should().NotBeNull();
        location!.ClrType.Should().Be(typeof(Point));
        location.GetColumnType().Should().Be("geography");
    }

    [Fact]
    public void Model_HasFilteredUniqueExternalProviderIndexes()
    {
        var venueIndexes = _dbContext.Model.FindEntityType(typeof(Venue))!.GetIndexes();
        venueIndexes.Should().Contain(index =>
            index.GetDatabaseName() == "IX_venues_ExternalProvider"
            && index.IsUnique
            && index.GetFilter()!.Contains("IsDeleted", StringComparison.Ordinal));
    }

    [Fact]
    public void SoftDelete_QueryFilter_HidesDeletedEntities()
    {
        var entityType = _dbContext.Model.FindEntityType(typeof(Venue));
        entityType.Should().NotBeNull();
        entityType!.GetDeclaredQueryFilters().Should().NotBeEmpty();
    }

    [Fact]
    public async Task SaveChangesAsync_SetsCreatedOn_AndSynchronizesLocation()
    {
        var fixedNow = new DateTimeOffset(2026, 9, 3, 12, 0, 0, TimeSpan.Zero);
        SystemTimeProvider.Provider = new FixedTimeProvider(fixedNow);

        var coordinates = DomainCoordinates.Create(52.2297, 21.0122).Value;
        var venue = Venue.CreateManually("National Stadium", coordinates: coordinates).Value;

        _dbContext.Venues.Add(venue);
        await _dbContext.SaveChangesAsync();

        venue.CreatedOn.Should().Be(fixedNow);
        venue.ModifiedOn.Should().BeNull();
        venue.DeletedOn.Should().BeNull();

        var location = _dbContext.Entry(venue).Property<Point?>("Location").CurrentValue;
        location.Should().NotBeNull();
        location!.SRID.Should().Be(4326);
        location.X.Should().Be(21.0122);
        location.Y.Should().Be(52.2297);
    }

    [Fact]
    public async Task SaveChangesAsync_SetsDeletedOn_WhenSoftDeleted()
    {
        var createdAt = new DateTimeOffset(2026, 9, 3, 12, 0, 0, TimeSpan.Zero);
        SystemTimeProvider.Provider = new FixedTimeProvider(createdAt);

        var venue = Venue.CreateManually("Arena").Value;
        _dbContext.Venues.Add(venue);
        await _dbContext.SaveChangesAsync();

        var deletedAt = createdAt.AddHours(1);
        SystemTimeProvider.Provider = new FixedTimeProvider(deletedAt);

        venue.Delete().IsSuccess.Should().BeTrue();
        await _dbContext.SaveChangesAsync();

        venue.IsDeleted.Should().BeTrue();
        venue.DeletedOn.Should().Be(deletedAt);
        venue.ModifiedOn.Should().Be(deletedAt);
    }

    [Fact]
    public async Task SaveChangesAsync_ClearsLocation_WhenCoordinatesRemoved()
    {
        var coordinates = DomainCoordinates.Create(50.0, 19.0).Value;
        var venue = Venue.CreateManually("Hall", coordinates: coordinates).Value;
        _dbContext.Venues.Add(venue);
        await _dbContext.SaveChangesAsync();

        venue.UpdateManually("Hall Updated", coordinates: null).IsSuccess.Should().BeTrue();
        venue.Coordinates.Should().BeNull();
        await _dbContext.SaveChangesAsync();

        _dbContext.Entry(venue).Property<Point?>("Location").CurrentValue.Should().BeNull();
    }

    [Fact]
    public void DependencyInjection_RegistersUnitOfWorkAndInterceptors()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:fixtures-db"] = "Server=(localdb)\\mssqllocaldb;Database=SporeoFixturesTests;Trusted_Connection=True;"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddPersistence(configuration);

        using var provider = services.BuildServiceProvider();

        provider.GetService<AuditableEntityInterceptor>().Should().NotBeNull();
        provider.GetService<VenueLocationInterceptor>().Should().NotBeNull();
        provider.GetService<IUnitOfWork>().Should().NotBeNull();
        provider.GetService<IPublisher>().Should().NotBeNull();
        provider.GetService<ISqlConnectionFactory>().Should().NotBeNull();
    }

    public void Dispose()
    {
        SystemTimeProvider.Provider = _originalTimeProvider;
        _dbContext.Dispose();
        _serviceProvider.Dispose();
    }

    private static FixturesDbContext CreateRelationalModelContext()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(FixturesDbContextTests).Assembly));
        services.AddSingleton<AuditableEntityInterceptor>();
        services.AddSingleton<VenueLocationInterceptor>();
        services.AddDbContext<FixturesDbContext>((sp, options) =>
        {
            options.UseSqlServer(
                "Server=(localdb)\\mssqllocaldb;Database=SporeoFixturesModelTests;Trusted_Connection=True;",
                sql => sql.UseNetTopologySuite());
            options.AddInterceptors(
                sp.GetRequiredService<AuditableEntityInterceptor>(),
                sp.GetRequiredService<VenueLocationInterceptor>());
        });

        return services.BuildServiceProvider().GetRequiredService<FixturesDbContext>();
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}

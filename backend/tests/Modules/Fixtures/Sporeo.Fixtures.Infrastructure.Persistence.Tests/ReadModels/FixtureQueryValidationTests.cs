using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sporeo.BuildingBlocks.Application.Pagination;
using Sporeo.BuildingBlocks.Domain.Results;
using Sporeo.Fixtures.Application;
using Sporeo.Fixtures.Application.Fixtures.Queries.Common;
using Sporeo.Fixtures.Application.Fixtures.Queries.GetFixtureDetails;
using Sporeo.Fixtures.Application.Fixtures.Queries.GetFixtures;
using Sporeo.Fixtures.Application.Fixtures.Queries.GetNearbyFixtures;
using Sporeo.Fixtures.Application.Venues.Queries.GetVenueDetails;
using Sporeo.Fixtures.Domain.Common;
using Sporeo.Fixtures.Infrastructure.Persistence;

namespace Sporeo.Fixtures.Infrastructure.Persistence.Tests;

[Collection(SqlServerCollection.Name)]
public sealed class FixtureQueryValidationTests
{
    [Fact]
    public async Task GetFixtures_WithInvalidPagination_ShouldReturnValidationFailure()
    {
        await using var provider = await CreateProviderAsync();
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new GetFixturesQuery(
            new PaginationParams(0, 0),
            new FixtureFilters()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationError>();
    }

    [Fact]
    public async Task GetFixtures_WithInvalidDateRange_ShouldReturnValidationFailure()
    {
        await using var provider = await CreateProviderAsync();
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new GetFixturesQuery(
            new PaginationParams(1, 10),
            new FixtureFilters(
                DateFrom: new DateTimeOffset(2026, 2, 2, 0, 0, 0, TimeSpan.Zero),
                DateTo: new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero))));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationError>();
    }

    [Fact]
    public async Task GetNearbyFixtures_WithInvalidCoordinates_ShouldReturnValidationFailure()
    {
        await using var provider = await CreateProviderAsync();
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new GetNearbyFixturesQuery(
            Latitude: 91,
            Longitude: 200,
            RadiusInMeters: 1000,
            Pagination: new PaginationParams(1, 10),
            Filters: new FixtureFilters()));

        result.IsFailure.Should().BeTrue();
        var validationError = result.Error.Should().BeOfType<ValidationError>().Subject;
        validationError.Errors.Should().Contain(error => error.Code == Errors.Venue.Coordinates.InvalidLatitude.Code);
        validationError.Errors.Should().Contain(error => error.Code == Errors.Venue.Coordinates.InvalidLongitude.Code);
    }

    [Fact]
    public async Task GetNearbyFixtures_WithNonPositiveRadius_ShouldReturnValidationFailure()
    {
        await using var provider = await CreateProviderAsync();
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new GetNearbyFixturesQuery(
            Latitude: 52.2297,
            Longitude: 21.0122,
            RadiusInMeters: 0,
            Pagination: new PaginationParams(1, 10),
            Filters: new FixtureFilters()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationError>();
    }

    [Fact]
    public async Task GetFixtureDetails_WithEmptyId_ShouldReturnValidationFailure()
    {
        await using var provider = await CreateProviderAsync();
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new GetFixtureDetailsQuery(Guid.Empty));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationError>();
    }

    [Fact]
    public async Task GetVenueDetails_WithEmptyId_ShouldReturnValidationFailure()
    {
        await using var provider = await CreateProviderAsync();
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new GetVenueDetailsQuery(Guid.Empty));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationError>();
    }

    private static async Task<ServiceProvider> CreateProviderAsync()
    {
        const string databaseName = "SporeoFixturesValidationTests";
        return await SqlServerTestDatabase.CreateInitializedProviderAsync(databaseName);
    }
}

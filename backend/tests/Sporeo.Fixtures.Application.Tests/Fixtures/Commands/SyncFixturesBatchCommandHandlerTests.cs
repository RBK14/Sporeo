using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Sporeo.Fixtures.Application.Abstractions.Providers;
using Sporeo.Fixtures.Application.Abstractions.Repositories;
using Sporeo.Fixtures.Application.Fixtures.Commands;
using Sporeo.Fixtures.Domain.Fixtures;
using Sporeo.Fixtures.Domain.Fixtures.Enums;
using Sporeo.Fixtures.Domain.Fixtures.ValueObjects;
using Sporeo.Fixtures.Domain.Sports.ValueObjects;
using Sporeo.Fixtures.Domain.Venues;
using Sporeo.Fixtures.Domain.Venues.ValueObjects;

namespace Sporeo.Fixtures.Application.Tests.Fixtures.Commands;

public sealed class SyncFixturesBatchCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithPartialFixtureFailures_ShouldSucceedAndReportFailures()
    {
        var sportId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var validFixture = new ExternalFixtureDto(
            "valid-1",
            "TheSportsDB",
            "Home vs Away",
            DateTimeOffset.UtcNow.AddDays(1),
            FixtureStatus.Scheduled,
            null);
        var invalidFixture = validFixture with
        {
            ProviderId = "invalid-1",
            Name = " "
        };

        var fixtureRepository = Substitute.For<IFixtureRepository>();
        fixtureRepository.GetByExternalProviderIdsAsync(
                Arg.Any<string>(),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>())
            .Returns([]);

        var venueRepository = Substitute.For<IVenueRepository>();
        venueRepository.GetByExternalProviderIdsAsync(
                Arg.Any<string>(),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = new SyncFixturesBatchCommandHandler(
            fixtureRepository,
            venueRepository,
            NullLogger<SyncFixturesBatchCommandHandler>.Instance);

        var result = await handler.Handle(
            new SyncFixturesBatchCommand(sportId, null, null, "TheSportsDB", [validFixture, invalidFixture]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Inserted.Should().Be(1);
        result.Value.Failed.Should().Be(1);
        result.Value.HasFailures.Should().BeTrue();
        fixtureRepository.Received(1).Add(Arg.Any<Fixture>());
    }

    [Fact]
    public async Task Handle_WithAllValidFixtures_ShouldReportNoFailures()
    {
        var sportId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var fixture = new ExternalFixtureDto(
            "1",
            "TheSportsDB",
            "Home vs Away",
            DateTimeOffset.UtcNow.AddDays(1),
            FixtureStatus.Scheduled,
            null);

        var fixtureRepository = Substitute.For<IFixtureRepository>();
        fixtureRepository.GetByExternalProviderIdsAsync(
                Arg.Any<string>(),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>())
            .Returns([]);

        var venueRepository = Substitute.For<IVenueRepository>();
        venueRepository.GetByExternalProviderIdsAsync(
                Arg.Any<string>(),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = new SyncFixturesBatchCommandHandler(
            fixtureRepository,
            venueRepository,
            NullLogger<SyncFixturesBatchCommandHandler>.Instance);

        var result = await handler.Handle(
            new SyncFixturesBatchCommand(sportId, null, null, "TheSportsDB", [fixture]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasFailures.Should().BeFalse();
        result.Value.Inserted.Should().Be(1);
    }
}

using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Sporeo.Fixtures.Application.Fixtures.Abstractions.Providers;
using Sporeo.Fixtures.Application.Fixtures.Abstractions.Repositories;
using Sporeo.Fixtures.Application.Venues.Abstractions.Repositories;
using Sporeo.Fixtures.Application.Fixtures.Commands.SyncFixturesBatch;
using Sporeo.Fixtures.Domain.Fixtures;
using Sporeo.Fixtures.Domain.Fixtures.Enums;
using Sporeo.Fixtures.Domain.Sports.ValueObjects;

namespace Sporeo.Fixtures.Application.Tests.Fixtures.Commands;

public sealed class SyncFixturesBatchChunkCommandHandlerTests
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

        var handler = CreateHandler(out var fixtureRepository, out _);

        var result = await handler.Handle(
            new SyncFixturesBatchChunkCommand(sportId, null, null, "TheSportsDB", [validFixture, invalidFixture]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Inserted.Should().Be(1);
        result.Value.Failed.Should().Be(1);
        result.Value.Status.Should().Be(SyncBatchStatus.PartialSuccess);
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

        var handler = CreateHandler(out var fixtureRepository, out _);

        var result = await handler.Handle(
            new SyncFixturesBatchChunkCommand(sportId, null, null, "TheSportsDB", [fixture]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasFailures.Should().BeFalse();
        result.Value.Status.Should().Be(SyncBatchStatus.Succeeded);
        result.Value.Inserted.Should().Be(1);
        fixtureRepository.Received(1).Add(Arg.Any<Fixture>());
    }

    [Fact]
    public async Task Handle_WithManuallyEditedFixture_ShouldSkip()
    {
        var sportId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var existing = Fixture.CreateFromProvider(
            SportId.FromValue(sportId),
            null,
            null,
            "Home vs Away",
            DateTimeOffset.UtcNow.AddDays(1),
            "TheSportsDB",
            "1").Value;
        existing.UpdateManually(SportId.FromValue(sportId), null, "Manual", DateTimeOffset.UtcNow.AddDays(2));

        var fixtureRepository = Substitute.For<IFixtureRepository>();
        fixtureRepository.GetByExternalProviderIdsAsync(
                "TheSportsDB",
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>())
            .Returns([existing]);

        var venueRepository = Substitute.For<IVenueRepository>();
        venueRepository.GetByExternalProviderIdsAsync(
                Arg.Any<string>(),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = new SyncFixturesBatchChunkCommandHandler(
            fixtureRepository,
            venueRepository,
            NullLogger<SyncFixturesBatchChunkCommandHandler>.Instance);

        var incoming = new ExternalFixtureDto(
            "1",
            "TheSportsDB",
            "Provider Name",
            DateTimeOffset.UtcNow.AddDays(3),
            FixtureStatus.Scheduled,
            null);

        var result = await handler.Handle(
            new SyncFixturesBatchChunkCommand(sportId, null, null, "TheSportsDB", [incoming]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Skipped.Should().Be(1);
        result.Value.Updated.Should().Be(0);
        result.Value.Status.Should().Be(SyncBatchStatus.PartialSuccess);
        existing.Name.Should().Be("Manual");
    }

    [Fact]
    public async Task Handle_WithDuplicateProviderIds_ShouldSkipDuplicates()
    {
        var sportId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var fixture = new ExternalFixtureDto(
            "1",
            "TheSportsDB",
            "Home vs Away",
            DateTimeOffset.UtcNow.AddDays(1),
            FixtureStatus.Scheduled,
            null);

        var handler = CreateHandler(out var fixtureRepository, out _);

        var result = await handler.Handle(
            new SyncFixturesBatchChunkCommand(sportId, null, null, "TheSportsDB", [fixture, fixture with { Name = "Other" }]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Inserted.Should().Be(1);
        result.Value.Skipped.Should().Be(1);
        (result.Value.Inserted + result.Value.Updated + result.Value.Skipped + result.Value.Failed)
            .Should().Be(2);
        fixtureRepository.Received(1).Add(Arg.Any<Fixture>());
    }

    [Fact]
    public async Task Handle_WithMixedProvider_ShouldFailItem()
    {
        var sportId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var fixture = new ExternalFixtureDto(
            "1",
            "OtherProvider",
            "Home vs Away",
            DateTimeOffset.UtcNow.AddDays(1),
            FixtureStatus.Scheduled,
            null);

        var handler = CreateHandler(out var fixtureRepository, out _);

        var result = await handler.Handle(
            new SyncFixturesBatchChunkCommand(sportId, null, null, "TheSportsDB", [fixture]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Failed.Should().Be(1);
        fixtureRepository.DidNotReceive().Add(Arg.Any<Fixture>());
    }

    [Fact]
    public async Task Handle_LookupUsesProviderNameAndProviderId()
    {
        var sportId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var fixture = new ExternalFixtureDto(
            "42",
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

        var handler = new SyncFixturesBatchChunkCommandHandler(
            fixtureRepository,
            venueRepository,
            NullLogger<SyncFixturesBatchChunkCommandHandler>.Instance);

        await handler.Handle(
            new SyncFixturesBatchChunkCommand(sportId, null, null, "TheSportsDB", [fixture]),
            CancellationToken.None);

        await fixtureRepository.Received(1).GetByExternalProviderIdsAsync(
            "TheSportsDB",
            Arg.Is<IEnumerable<string>>(ids => ids.Single() == "42"),
            Arg.Any<CancellationToken>());
    }

    private static SyncFixturesBatchChunkCommandHandler CreateHandler(
        out IFixtureRepository fixtureRepository,
        out IVenueRepository venueRepository)
    {
        fixtureRepository = Substitute.For<IFixtureRepository>();
        fixtureRepository.GetByExternalProviderIdsAsync(
                Arg.Any<string>(),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>())
            .Returns([]);

        venueRepository = Substitute.For<IVenueRepository>();
        venueRepository.GetByExternalProviderIdsAsync(
                Arg.Any<string>(),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>())
            .Returns([]);

        return new SyncFixturesBatchChunkCommandHandler(
            fixtureRepository,
            venueRepository,
            NullLogger<SyncFixturesBatchChunkCommandHandler>.Instance);
    }
}

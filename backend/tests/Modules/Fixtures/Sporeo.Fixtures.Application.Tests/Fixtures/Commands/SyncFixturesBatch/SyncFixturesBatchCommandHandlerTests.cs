using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Sporeo.Fixtures.Application.Fixtures.Abstractions;
using Sporeo.Fixtures.Application.Fixtures.Data;
using Sporeo.Fixtures.Application.Venues.Data;
using Sporeo.Fixtures.Application.Fixtures.Commands.SyncFixturesBatch;
using Sporeo.Fixtures.Application.Fixtures.Commands.SyncFixturesBatchChunk;
using Sporeo.Fixtures.Domain.Fixtures;
using Sporeo.Fixtures.Domain.Fixtures.Enums;
using Sporeo.Fixtures.Domain.Leagues.ValueObjects;
using Sporeo.Fixtures.Domain.Seasons.ValueObjects;
using Sporeo.Fixtures.Domain.Sports.ValueObjects;

namespace Sporeo.Fixtures.Application.Tests.Fixtures.Commands;

public sealed class SyncFixturesBatchChunkCommandHandlerTests
{
    private static readonly Guid SportGuid = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid LeagueGuid = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid SeasonGuid = Guid.Parse("33333333-3333-3333-3333-333333333333");

    [Fact]
    public async Task Handle_WithPartialFixtureFailures_ShouldSucceedAndReportFailures()
    {
        var validFixture = CreateExternalFixture("valid-1");
        var invalidFixture = validFixture with
        {
            ExternalId = "invalid-1",
            Name = " "
        };

        var handler = CreateHandler(out var fixtureRepository, out _);

        var result = await handler.Handle(
            CreateChunkCommand([validFixture, invalidFixture]),
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
        var fixture = CreateExternalFixture("1");

        var handler = CreateHandler(out var fixtureRepository, out _);

        var result = await handler.Handle(
            CreateChunkCommand([fixture]),
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
        var existing = Fixture.CreateFromProvider(
            SportId.FromValue(SportGuid),
            LeagueId.FromValue(LeagueGuid),
            null,
            "Home vs Away",
            DateTimeOffset.UtcNow.AddDays(1),
            "TheSportsDB",
            "1").Value;
        existing.UpdateManually(
            SportId.FromValue(SportGuid),
            LeagueId.FromValue(LeagueGuid),
            "Manual",
            DateTimeOffset.UtcNow.AddDays(2));

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

        var incoming = CreateExternalFixture("1") with { Name = "Provider Name", StartDate = DateTimeOffset.UtcNow.AddDays(3) };

        var result = await handler.Handle(
            CreateChunkCommand([incoming]),
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
        var fixture = CreateExternalFixture("1");

        var handler = CreateHandler(out var fixtureRepository, out _);

        var result = await handler.Handle(
            CreateChunkCommand([fixture, fixture with { Name = "Other" }]),
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
        var fixture = CreateExternalFixture("1") with { ProviderName = "OtherProvider" };

        var handler = CreateHandler(out var fixtureRepository, out _);

        var result = await handler.Handle(
            CreateChunkCommand([fixture]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Failed.Should().Be(1);
        fixtureRepository.DidNotReceive().Add(Arg.Any<Fixture>());
    }

    [Fact]
    public async Task Handle_LookupUsesProviderNameAndProviderId()
    {
        var fixture = CreateExternalFixture("42");

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
            CreateChunkCommand([fixture]),
            CancellationToken.None);

        await fixtureRepository.Received(1).GetByExternalProviderIdsAsync(
            "TheSportsDB",
            Arg.Is<IEnumerable<string>>(ids => ids.Single() == "42"),
            Arg.Any<CancellationToken>());
    }

    private static SyncFixturesBatchChunkCommand CreateChunkCommand(
        IReadOnlyList<ExternalFixtureDto> fixtures) =>
        new(
            "TheSportsDB",
            SportId.FromValue(SportGuid),
            LeagueId.FromValue(LeagueGuid),
            new Dictionary<string, SeasonId> { ["2025-2026"] = SeasonId.FromValue(SeasonGuid) },
            fixtures);

    private static ExternalFixtureDto CreateExternalFixture(string externalId) =>
        new(
            "TheSportsDB",
            externalId,
            "Home vs Away",
            "2025-2026",
            DateTimeOffset.UtcNow.AddDays(1),
            FixtureStatus.Scheduled,
            null);

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

using Microsoft.Extensions.Logging;
using Sporeo.BuildingBlocks.Application.Abstractions.Execution;
using Sporeo.BuildingBlocks.Domain.Results;
using Sporeo.Fixtures.Application.Fixtures.Abstractions;
using Sporeo.Fixtures.Application.Fixtures.Commands.SyncFixturesBatch;
using Sporeo.Fixtures.Application.Fixtures.Data;
using Sporeo.Fixtures.Application.Venues.Data;
using Sporeo.Fixtures.Domain.Venues;
using Sporeo.Fixtures.Domain.Venues.ValueObjects;

namespace Sporeo.Fixtures.Application.Fixtures.Commands.SyncFixturesBatchChunk;

/// <summary>
/// Upserts fixtures and venues for a single provider chunk inside one DI scope / DbContext.
/// </summary>
internal sealed class SyncFixturesBatchChunkCommandHandler(
    IFixtureRepository fixtureRepository,
    IVenueRepository venueRepository,
    ILogger<SyncFixturesBatchChunkCommandHandler> logger)
    : ICommandHandler<SyncFixturesBatchChunkCommand, SyncBatchResultDto>
{
    /// <inheritdoc />
    public async Task<Result<SyncBatchResultDto>> Handle(
        SyncFixturesBatchChunkCommand request,
        CancellationToken cancellationToken)
    {
        var prepared = Prepare(request.ProviderName, request.Fixtures);
        var venues = prepared.Fixtures
            .Where(fixture => fixture.Venue is not null &&
                              !string.IsNullOrWhiteSpace(fixture.Venue.ExternalId))
            .Select(fixture => fixture.Venue!)
            .DistinctBy(venue => venue.ExternalId, StringComparer.Ordinal)
            .ToList();

        var existingVenues = (await venueRepository.GetByExternalProviderIdsAsync(
                request.ProviderName,
                venues.Select(venue => venue.ExternalId),
                cancellationToken))
            .Where(venue => string.Equals(venue.ExternalProviderName, request.ProviderName, StringComparison.Ordinal))
            .GroupBy(venue => venue.ExternalProviderId!, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        var existingFixtures = (await fixtureRepository.GetByExternalProviderIdsAsync(
                request.ProviderName,
                prepared.Fixtures.Select(fixture => fixture.ExternalId),
                cancellationToken))
            .Where(fixture => string.Equals(fixture.ExternalProviderName, request.ProviderName, StringComparison.Ordinal))
            .GroupBy(fixture => fixture.ExternalProviderId!, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        var venueIdMap = new Dictionary<string, VenueId>(StringComparer.Ordinal);
        new VenueBatchUpsert(venueRepository, logger).Process(
            request.ProviderName,
            venues,
            existingVenues,
            venueIdMap);

        var fixtureReport = new FixtureBatchUpsert(fixtureRepository, logger).Process(
            request.SportId,
            request.LeagueId,
            request.SeasonMap,
            request.ProviderName,
            prepared.Fixtures,
            existingFixtures,
            venueIdMap);

        var report = SyncBatchResultDto.Create(
            fixtureReport.Inserted,
            fixtureReport.Updated,
            fixtureReport.Skipped + prepared.DuplicateSkipped,
            fixtureReport.Failed + prepared.Failed);

        if (report.HasWarnings)
        {
            logger.LogWarning(
                "Fixture chunk sync completed with partial success. Inserted={Inserted}, Updated={Updated}, Skipped={Skipped}, Failed={Failed}",
                report.Inserted,
                report.Updated,
                report.Skipped,
                report.Failed);
        }
        else
        {
            logger.LogInformation(
                "Fixture chunk sync succeeded. Inserted={Inserted}, Updated={Updated}",
                report.Inserted,
                report.Updated);
        }

        return Result.Success(report);
    }

    private static PreparedFixtures Prepare(
        string providerName,
        IReadOnlyList<ExternalFixtureDto> incoming)
    {
        var fixtures = new List<ExternalFixtureDto>(incoming.Count);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var duplicateSkipped = 0;
        var failed = 0;

        foreach (var fixture in incoming)
        {
            if (string.IsNullOrWhiteSpace(fixture.ExternalId) ||
                string.IsNullOrWhiteSpace(fixture.ProviderName) ||
                string.IsNullOrWhiteSpace(fixture.Name) ||
                !string.Equals(fixture.ProviderName, providerName, StringComparison.Ordinal) ||
                (fixture.Venue is not null &&
                 (!string.Equals(fixture.Venue.ProviderName, providerName, StringComparison.Ordinal) ||
                  string.IsNullOrWhiteSpace(fixture.Venue.ExternalId) ||
                  string.IsNullOrWhiteSpace(fixture.Venue.Name))))
            {
                failed++;
                continue;
            }

            if (!seen.Add(fixture.ExternalId))
            {
                duplicateSkipped++;
                continue;
            }

            fixtures.Add(fixture);
        }

        return new PreparedFixtures(fixtures, duplicateSkipped, failed);
    }

    private sealed record PreparedFixtures(
        IReadOnlyList<ExternalFixtureDto> Fixtures,
        int DuplicateSkipped,
        int Failed);
}

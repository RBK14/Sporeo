using Microsoft.Extensions.Logging;
using Sporeo.Fixtures.Application.Fixtures.Abstractions;
using Sporeo.Fixtures.Application.Fixtures.Commands.SyncFixturesBatch;
using Sporeo.Fixtures.Application.Fixtures.Data;
using Sporeo.Fixtures.Domain.Common;
using Sporeo.Fixtures.Domain.Fixtures;
using Sporeo.Fixtures.Domain.Fixtures.Enums;
using Sporeo.Fixtures.Domain.Leagues.ValueObjects;
using Sporeo.Fixtures.Domain.Seasons.ValueObjects;
using Sporeo.Fixtures.Domain.Sports.ValueObjects;
using Sporeo.Fixtures.Domain.Venues.ValueObjects;

namespace Sporeo.Fixtures.Application.Fixtures.Commands.SyncFixturesBatchChunk;

/// <summary>
/// Upserts fixtures from external payloads into the local fixture repository.
/// </summary>
internal sealed class FixtureBatchUpsert(
    IFixtureRepository fixtureRepository,
    ILogger logger)
{
    public SyncBatchResultDto Process(
        SportId sportId,
        LeagueId leagueId,
        IReadOnlyDictionary<string, SeasonId> seasonMap,
        string providerName,
        IReadOnlyList<ExternalFixtureDto> incomingFixtures,
        Dictionary<string, Fixture> existingFixtures,
        Dictionary<string, VenueId> venueIdMap)
    {
        var inserted = 0;
        var updated = 0;
        var skipped = 0;
        var failed = 0;

        foreach (var incomingFixture in incomingFixtures)
        {
            VenueId? venueId = null;
            if (incomingFixture.Venue is not null &&
                venueIdMap.TryGetValue(incomingFixture.Venue.ExternalId, out var mappedVenueId))
            {
                venueId = mappedVenueId;
            }

            SeasonId? seasonId = null;
            if (!string.IsNullOrWhiteSpace(incomingFixture.SeasonName) &&
                seasonMap.TryGetValue(incomingFixture.SeasonName, out var mappedSeasonId))
            {
                seasonId = mappedSeasonId;
            }

            if (existingFixtures.TryGetValue(incomingFixture.ExternalId, out var fixture))
            {
                if (fixture.Status == FixtureStatus.Finished)
                    continue;

                var syncResult = fixture.SyncFromProvider(
                    sportId,
                    leagueId,
                    seasonId,
                    incomingFixture.Name,
                    incomingFixture.StartDate,
                    incomingFixture.Status,
                    venueId);

                if (syncResult.IsFailure)
                {
                    if (syncResult.Error == Errors.Fixture.LockedForSync)
                    {
                        skipped++;
                        logger.LogInformation(
                            "Fixture {ProviderName}/{ExternalId} skipped during sync: {ErrorCode}",
                            providerName,
                            incomingFixture.ExternalId,
                            syncResult.Error.Code);
                    }
                    else
                    {
                        failed++;
                        logger.LogWarning(
                            "Fixture {ProviderName}/{ExternalId} sync failed: {ErrorCode}",
                            providerName,
                            incomingFixture.ExternalId,
                            syncResult.Error.Code);
                    }

                    continue;
                }

                updated++;
                continue;
            }

            var newFixtureResult = Fixture.CreateFromProvider(
                sportId,
                leagueId,
                seasonId,
                incomingFixture.Name,
                incomingFixture.StartDate,
                providerName,
                incomingFixture.ExternalId);

            if (newFixtureResult.IsFailure)
            {
                failed++;
                logger.LogWarning(
                    "Failed to create fixture {ProviderName}/{ExternalId}: {ErrorCode}",
                    providerName,
                    incomingFixture.ExternalId,
                    newFixtureResult.Error.Code);
                continue;
            }

            var newFixture = newFixtureResult.Value;
            var applyResult = newFixture.SyncFromProvider(
                sportId,
                leagueId,
                seasonId,
                incomingFixture.Name,
                incomingFixture.StartDate,
                incomingFixture.Status,
                venueId);

            if (applyResult.IsFailure)
            {
                failed++;
                logger.LogWarning(
                    "Fixture {ProviderName}/{ExternalId} initial sync failed: {ErrorCode}",
                    providerName,
                    incomingFixture.ExternalId,
                    applyResult.Error.Code);
                continue;
            }

            fixtureRepository.Add(newFixture);
            inserted++;
        }

        return SyncBatchResultDto.Create(inserted, updated, skipped, failed);
    }
}

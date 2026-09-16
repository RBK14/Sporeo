using Microsoft.Extensions.Logging;
using Sporeo.BuildingBlocks.Application.Abstractions.Execution;
using Sporeo.BuildingBlocks.Domain.Results;
using Sporeo.Fixtures.Application.Abstractions.Providers;
using Sporeo.Fixtures.Application.Abstractions.Repositories;
using Sporeo.Fixtures.Domain.Fixtures;
using Sporeo.Fixtures.Domain.Leagues.ValueObjects;
using Sporeo.Fixtures.Domain.Seasons.ValueObjects;
using Sporeo.Fixtures.Domain.Sports.ValueObjects;
using Sporeo.Fixtures.Domain.Venues;
using Sporeo.Fixtures.Domain.Venues.ValueObjects;

namespace Sporeo.Fixtures.Application.Fixtures.Commands;

/// <summary>
/// Upserts fixtures and venues for a single provider batch.
/// </summary>
internal sealed class SyncFixturesBatchCommandHandler(
    IFixtureRepository fixtureRepository,
    IVenueRepository venueRepository,
    ILogger<SyncFixturesBatchCommandHandler> logger)
    : ICommandHandler<SyncFixturesBatchCommand, SyncFixturesBatchReport>
{
    /// <inheritdoc />
    public async Task<Result<SyncFixturesBatchReport>> Handle(
        SyncFixturesBatchCommand request,
        CancellationToken cancellationToken)
    {
        var sportId = SportId.FromValue(request.SportId);
        var leagueId = request.LeagueId.HasValue ? LeagueId.FromValue(request.LeagueId.Value) : null;
        var seasonId = request.SeasonId.HasValue ? SeasonId.FromValue(request.SeasonId.Value) : null;

        var fixtures = request.Fixtures
            .DistinctBy(fixture => fixture.ProviderId, StringComparer.Ordinal)
            .ToList();

        var venues = fixtures
            .Where(fixture => fixture.Venue is not null)
            .Select(fixture => fixture.Venue!)
            .DistinctBy(venue => venue.ProviderId, StringComparer.Ordinal)
            .ToList();

        var existingVenues = (await venueRepository.GetByExternalProviderIdsAsync(
                request.ProviderName,
                venues.Select(venue => venue.ProviderId),
                cancellationToken))
            .GroupBy(venue => venue.ExternalProviderId!, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        var existingFixtures = (await fixtureRepository.GetByExternalProviderIdsAsync(
                request.ProviderName,
                fixtures.Select(fixture => fixture.ProviderId),
                cancellationToken))
            .GroupBy(fixture => fixture.ExternalProviderId!, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        var venueIdMap = new Dictionary<string, VenueId>(StringComparer.Ordinal);
        var venueFailures = ProcessVenues(request.ProviderName, venues, existingVenues, venueIdMap);
        var fixtureReport = ProcessFixtures(
            sportId,
            leagueId,
            seasonId,
            request.ProviderName,
            fixtures,
            existingFixtures,
            venueIdMap);

        var report = fixtureReport with { VenueFailed = venueFailures };

        if (report.HasFailures)
        {
            logger.LogWarning(
                "Fixture batch sync completed with partial failures. VenueFailures={VenueFailures}, Inserted={Inserted}, Updated={Updated}, Skipped={Skipped}, Failed={Failed}",
                report.VenueFailed,
                report.Inserted,
                report.Updated,
                report.Skipped,
                report.Failed);
        }
        else
        {
            logger.LogInformation(
                "Fixture batch sync succeeded. Inserted={Inserted}, Updated={Updated}, Skipped={Skipped}",
                report.Inserted,
                report.Updated,
                report.Skipped);
        }

        return Result.Success(report);
    }

    private int ProcessVenues(
        string providerName,
        IReadOnlyList<ExternalFixtureVenueDto> incomingVenues,
        Dictionary<string, Venue> existingVenues,
        Dictionary<string, VenueId> venueIdMap)
    {
        var failures = 0;

        foreach (var incomingVenue in incomingVenues)
        {
            if (existingVenues.TryGetValue(incomingVenue.ProviderId, out var existingVenue))
            {
                venueIdMap[incomingVenue.ProviderId] = existingVenue.Id;

                Coordinates? coordinates = null;
                if (incomingVenue.Latitude.HasValue && incomingVenue.Longitude.HasValue)
                {
                    var coordResult = Coordinates.Create(incomingVenue.Latitude.Value, incomingVenue.Longitude.Value);
                    if (coordResult.IsFailure)
                    {
                        failures++;
                        logger.LogWarning(
                            "Skipping venue sync for {ProviderId}: {ErrorCode}",
                            incomingVenue.ProviderId,
                            coordResult.Error.Code);
                        continue;
                    }

                    coordinates = coordResult.Value;
                }

                Address? address = null;
                if (!string.IsNullOrWhiteSpace(incomingVenue.City) && !string.IsNullOrWhiteSpace(incomingVenue.Country))
                {
                    var addressResult = Address.Create(incomingVenue.Street, incomingVenue.City, incomingVenue.Country);
                    if (addressResult.IsFailure)
                    {
                        failures++;
                        logger.LogWarning(
                            "Skipping venue sync for {ProviderId}: {ErrorCode}",
                            incomingVenue.ProviderId,
                            addressResult.Error.Code);
                        continue;
                    }

                    address = addressResult.Value;
                }

                var syncResult = existingVenue.SyncExternalData(incomingVenue.Name, address, coordinates);
                if (syncResult.IsFailure)
                {
                    // Manually edited venues are intentionally skipped.
                    logger.LogInformation(
                        "Venue {ProviderId} was skipped during sync: {ErrorCode}",
                        incomingVenue.ProviderId,
                        syncResult.Error.Code);
                }

                continue;
            }

            Coordinates? newCoordinates = null;
            if (incomingVenue.Latitude.HasValue && incomingVenue.Longitude.HasValue)
            {
                var coordResult = Coordinates.Create(incomingVenue.Latitude.Value, incomingVenue.Longitude.Value);
                if (coordResult.IsSuccess)
                    newCoordinates = coordResult.Value;
            }

            Address? newAddress = null;
            if (!string.IsNullOrWhiteSpace(incomingVenue.City) && !string.IsNullOrWhiteSpace(incomingVenue.Country))
            {
                var addressResult = Address.Create(incomingVenue.Street, incomingVenue.City, incomingVenue.Country);
                if (addressResult.IsSuccess)
                    newAddress = addressResult.Value;
            }

            var newVenueResult = Venue.CreateFromProvider(
                incomingVenue.Name,
                providerName,
                incomingVenue.ProviderId,
                newAddress,
                newCoordinates);

            if (newVenueResult.IsFailure)
            {
                failures++;
                logger.LogWarning(
                    "Failed to create venue {ProviderId}: {ErrorCode}",
                    incomingVenue.ProviderId,
                    newVenueResult.Error.Code);
                continue;
            }

            venueRepository.Add(newVenueResult.Value);
            venueIdMap[incomingVenue.ProviderId] = newVenueResult.Value.Id;
        }

        return failures;
    }

    private SyncFixturesBatchReport ProcessFixtures(
        SportId sportId,
        LeagueId? leagueId,
        SeasonId? seasonId,
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
            if (existingFixtures.TryGetValue(incomingFixture.ProviderId, out var fixture))
            {
                var syncResult = fixture.SyncExternalData(
                    sportId,
                    leagueId,
                    seasonId,
                    incomingFixture.Name,
                    incomingFixture.StartDate);

                if (syncResult.IsFailure)
                {
                    skipped++;
                    logger.LogInformation(
                        "Fixture {ProviderId} skipped during sync: {ErrorCode}",
                        incomingFixture.ProviderId,
                        syncResult.Error.Code);
                    continue;
                }

                var statusResult = fixture.ChangeStatus(incomingFixture.Status);
                if (statusResult.IsFailure)
                {
                    failed++;
                    logger.LogWarning(
                        "Fixture {ProviderId} status update failed: {ErrorCode}",
                        incomingFixture.ProviderId,
                        statusResult.Error.Code);
                    continue;
                }

                if (incomingFixture.Venue is not null &&
                    venueIdMap.TryGetValue(incomingFixture.Venue.ProviderId, out var venueId))
                {
                    var assignResult = fixture.AssignVenue(venueId);
                    if (assignResult.IsFailure)
                    {
                        failed++;
                        logger.LogWarning(
                            "Fixture {ProviderId} venue assignment failed: {ErrorCode}",
                            incomingFixture.ProviderId,
                            assignResult.Error.Code);
                        continue;
                    }
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
                incomingFixture.ProviderId);

            if (newFixtureResult.IsFailure)
            {
                failed++;
                logger.LogWarning(
                    "Failed to create fixture {ProviderId}: {ErrorCode}",
                    incomingFixture.ProviderId,
                    newFixtureResult.Error.Code);
                continue;
            }

            var newFixture = newFixtureResult.Value;
            var createStatusResult = newFixture.ChangeStatus(incomingFixture.Status);
            if (createStatusResult.IsFailure)
            {
                failed++;
                logger.LogWarning(
                    "Fixture {ProviderId} initial status update failed: {ErrorCode}",
                    incomingFixture.ProviderId,
                    createStatusResult.Error.Code);
                continue;
            }

            if (incomingFixture.Venue is not null &&
                venueIdMap.TryGetValue(incomingFixture.Venue.ProviderId, out var newVenueId))
            {
                var assignResult = newFixture.AssignVenue(newVenueId);
                if (assignResult.IsFailure)
                {
                    failed++;
                    logger.LogWarning(
                        "Fixture {ProviderId} venue assignment failed: {ErrorCode}",
                        incomingFixture.ProviderId,
                        assignResult.Error.Code);
                    continue;
                }
            }

            fixtureRepository.Add(newFixture);
            inserted++;
        }

        return new SyncFixturesBatchReport(inserted, updated, skipped, failed);
    }
}

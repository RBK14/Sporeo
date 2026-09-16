using Microsoft.Extensions.Logging;
using Sporeo.BuildingBlocks.Application.Abstractions.Execution;
using Sporeo.BuildingBlocks.Domain.Results;
using Sporeo.Fixtures.Application.Fixtures.Abstractions.Providers;
using Sporeo.Fixtures.Application.Fixtures.Abstractions.Repositories;
using Sporeo.Fixtures.Application.Venues.Abstractions.Repositories;
using Sporeo.Fixtures.Domain.Common;
using Sporeo.Fixtures.Domain.Fixtures;
using Sporeo.Fixtures.Domain.Leagues.ValueObjects;
using Sporeo.Fixtures.Domain.Seasons.ValueObjects;
using Sporeo.Fixtures.Domain.Sports.ValueObjects;
using Sporeo.Fixtures.Domain.Venues;
using Sporeo.Fixtures.Domain.Venues.ValueObjects;

namespace Sporeo.Fixtures.Application.Fixtures.Commands.SyncFixturesBatch;

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
        var sportId = SportId.FromValue(request.SportId);
        var leagueId = request.LeagueId.HasValue ? LeagueId.FromValue(request.LeagueId.Value) : null;
        var seasonId = request.SeasonId.HasValue ? SeasonId.FromValue(request.SeasonId.Value) : null;

        var prepared = PrepareFixtures(request.ProviderName, request.Fixtures);
        var venues = prepared.Fixtures
            .Where(fixture => fixture.Venue is not null &&
                              !string.IsNullOrWhiteSpace(fixture.Venue.ProviderId))
            .Select(fixture => fixture.Venue!)
            .DistinctBy(venue => venue.ProviderId, StringComparer.Ordinal)
            .ToList();

        var existingVenues = (await venueRepository.GetByExternalProviderIdsAsync(
                request.ProviderName,
                venues.Select(venue => venue.ProviderId),
                cancellationToken))
            .Where(venue => string.Equals(venue.ExternalProviderName, request.ProviderName, StringComparison.Ordinal))
            .GroupBy(venue => venue.ExternalProviderId!, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        var existingFixtures = (await fixtureRepository.GetByExternalProviderIdsAsync(
                request.ProviderName,
                prepared.Fixtures.Select(fixture => fixture.ProviderId),
                cancellationToken))
            .Where(fixture => string.Equals(fixture.ExternalProviderName, request.ProviderName, StringComparison.Ordinal))
            .GroupBy(fixture => fixture.ExternalProviderId!, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        var venueIdMap = new Dictionary<string, VenueId>(StringComparer.Ordinal);
        ProcessVenues(request.ProviderName, venues, existingVenues, venueIdMap);
        var fixtureReport = ProcessFixtures(
            sportId,
            leagueId,
            seasonId,
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

    private static PreparedFixtures PrepareFixtures(
        string providerName,
        IReadOnlyList<ExternalFixtureDto> incoming)
    {
        var fixtures = new List<ExternalFixtureDto>(incoming.Count);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var duplicateSkipped = 0;
        var failed = 0;

        foreach (var fixture in incoming)
        {
            if (string.IsNullOrWhiteSpace(fixture.ProviderId) ||
                string.IsNullOrWhiteSpace(fixture.ProviderName) ||
                string.IsNullOrWhiteSpace(fixture.Name) ||
                !string.Equals(fixture.ProviderName, providerName, StringComparison.Ordinal) ||
                (fixture.Venue is not null &&
                 (!string.Equals(fixture.Venue.ProviderName, providerName, StringComparison.Ordinal) ||
                  string.IsNullOrWhiteSpace(fixture.Venue.ProviderId) ||
                  string.IsNullOrWhiteSpace(fixture.Venue.Name))))
            {
                failed++;
                continue;
            }

            if (!seen.Add(fixture.ProviderId))
            {
                duplicateSkipped++;
                continue;
            }

            fixtures.Add(fixture);
        }

        return new PreparedFixtures(fixtures, duplicateSkipped, failed);
    }

    private void ProcessVenues(
        string providerName,
        IReadOnlyList<ExternalFixtureVenueDto> incomingVenues,
        Dictionary<string, Venue> existingVenues,
        Dictionary<string, VenueId> venueIdMap)
    {
        foreach (var incomingVenue in incomingVenues)
        {
            if (existingVenues.TryGetValue(incomingVenue.ProviderId, out var existingVenue))
            {
                venueIdMap[incomingVenue.ProviderId] = existingVenue.Id;

                if (!TryResolveCoordinates(incomingVenue, existingVenue.Coordinates, out var coordinates, out var coordError))
                {
                    logger.LogWarning(
                        "Skipping venue sync for {ProviderName}/{ProviderId}: {ErrorCode}",
                        providerName,
                        incomingVenue.ProviderId,
                        coordError);
                    continue;
                }

                if (!TryResolveAddress(incomingVenue, existingVenue.Address, out var address, out var addressError))
                {
                    logger.LogWarning(
                        "Skipping venue sync for {ProviderName}/{ProviderId}: {ErrorCode}",
                        providerName,
                        incomingVenue.ProviderId,
                        addressError);
                    continue;
                }

                var syncResult = existingVenue.SyncExternalData(incomingVenue.Name, address, coordinates);
                if (syncResult.IsFailure)
                {
                    logger.LogInformation(
                        "Venue {ProviderName}/{ProviderId} was skipped during sync: {ErrorCode}",
                        providerName,
                        incomingVenue.ProviderId,
                        syncResult.Error.Code);
                }

                continue;
            }

            TryResolveCoordinates(incomingVenue, existing: null, out var newCoordinates, out _);
            TryResolveAddress(incomingVenue, existing: null, out var newAddress, out _);

            var newVenueResult = Venue.CreateFromProvider(
                incomingVenue.Name,
                providerName,
                incomingVenue.ProviderId,
                newAddress,
                newCoordinates);

            if (newVenueResult.IsFailure)
            {
                logger.LogWarning(
                    "Failed to create venue {ProviderName}/{ProviderId}: {ErrorCode}",
                    providerName,
                    incomingVenue.ProviderId,
                    newVenueResult.Error.Code);
                continue;
            }

            venueRepository.Add(newVenueResult.Value);
            venueIdMap[incomingVenue.ProviderId] = newVenueResult.Value.Id;
        }
    }

    /// <summary>
    /// Resolves venue coordinates from the provider payload, preserving existing values when the payload omits them.
    /// </summary>
    private static bool TryResolveCoordinates(
        ExternalFixtureVenueDto incomingVenue,
        Coordinates? existing,
        out Coordinates? coordinates,
        out string? errorCode)
    {
        errorCode = null;

        if (incomingVenue.Latitude.HasValue && incomingVenue.Longitude.HasValue)
        {
            var coordResult = Coordinates.Create(incomingVenue.Latitude.Value, incomingVenue.Longitude.Value);
            if (coordResult.IsFailure)
            {
                coordinates = null;
                errorCode = coordResult.Error.Code;
                return false;
            }

            coordinates = coordResult.Value;
            return true;
        }

        coordinates = existing;
        return true;
    }

    /// <summary>
    /// Resolves a venue address, merging provider fields with any richer existing address
    /// so a country-only payload cannot wipe city/street.
    /// </summary>
    private static bool TryResolveAddress(
        ExternalFixtureVenueDto incomingVenue,
        Address? existing,
        out Address? address,
        out string? errorCode)
    {
        errorCode = null;

        var street = FirstNonWhiteSpace(incomingVenue.Street, existing?.Street);
        var city = FirstNonWhiteSpace(incomingVenue.City, existing?.City);
        var country = FirstNonWhiteSpace(incomingVenue.Country, existing?.Country);

        if (country is null)
        {
            address = existing;
            return true;
        }

        var addressResult = Address.Create(street, city, country);
        if (addressResult.IsFailure)
        {
            address = null;
            errorCode = addressResult.Error.Code;
            return false;
        }

        address = addressResult.Value;
        return true;
    }

    private static string? FirstNonWhiteSpace(string? primary, string? fallback)
    {
        if (!string.IsNullOrWhiteSpace(primary))
            return primary.Trim();

        if (!string.IsNullOrWhiteSpace(fallback))
            return fallback;

        return null;
    }

    private SyncBatchResultDto ProcessFixtures(
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
            VenueId? venueId = null;
            if (incomingFixture.Venue is not null &&
                venueIdMap.TryGetValue(incomingFixture.Venue.ProviderId, out var mappedVenueId))
            {
                venueId = mappedVenueId;
            }

            if (existingFixtures.TryGetValue(incomingFixture.ProviderId, out var fixture))
            {
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
                            "Fixture {ProviderName}/{ProviderId} skipped during sync: {ErrorCode}",
                            providerName,
                            incomingFixture.ProviderId,
                            syncResult.Error.Code);
                    }
                    else
                    {
                        failed++;
                        logger.LogWarning(
                            "Fixture {ProviderName}/{ProviderId} sync failed: {ErrorCode}",
                            providerName,
                            incomingFixture.ProviderId,
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
                incomingFixture.ProviderId);

            if (newFixtureResult.IsFailure)
            {
                failed++;
                logger.LogWarning(
                    "Failed to create fixture {ProviderName}/{ProviderId}: {ErrorCode}",
                    providerName,
                    incomingFixture.ProviderId,
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
                    "Fixture {ProviderName}/{ProviderId} initial sync failed: {ErrorCode}",
                    providerName,
                    incomingFixture.ProviderId,
                    applyResult.Error.Code);
                continue;
            }

            fixtureRepository.Add(newFixture);
            inserted++;
        }

        return SyncBatchResultDto.Create(inserted, updated, skipped, failed);
    }

    private sealed record PreparedFixtures(
        IReadOnlyList<ExternalFixtureDto> Fixtures,
        int DuplicateSkipped,
        int Failed);
}

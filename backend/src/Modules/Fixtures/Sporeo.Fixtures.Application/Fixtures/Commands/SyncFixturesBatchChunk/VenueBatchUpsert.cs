using Microsoft.Extensions.Logging;
using Sporeo.Fixtures.Application.Fixtures.Abstractions;
using Sporeo.Fixtures.Application.Venues.Data;
using Sporeo.Fixtures.Domain.Venues;
using Sporeo.Fixtures.Domain.Venues.ValueObjects;

namespace Sporeo.Fixtures.Application.Fixtures.Commands.SyncFixturesBatchChunk;

/// <summary>
/// Upserts venues from external fixture payloads into the local venue repository.
/// </summary>
internal sealed class VenueBatchUpsert(
    IVenueRepository venueRepository,
    ILogger logger)
{
    public void Process(
        string providerName,
        IReadOnlyList<ExternalFixtureVenueDto> incomingVenues,
        Dictionary<string, Venue> existingVenues,
        Dictionary<string, VenueId> venueIdMap)
    {
        foreach (var incomingVenue in incomingVenues)
        {
            if (existingVenues.TryGetValue(incomingVenue.ExternalId, out var existingVenue))
            {
                venueIdMap[incomingVenue.ExternalId] = existingVenue.Id;

                if (!TryResolveCoordinates(incomingVenue, existingVenue.Coordinates, out var coordinates, out var coordError))
                {
                    logger.LogWarning(
                        "Skipping venue sync for {ProviderName}/{ProviderId}: {ErrorCode}",
                        providerName,
                        incomingVenue.ExternalId,
                        coordError);
                    continue;
                }

                if (!TryResolveAddress(incomingVenue, existingVenue.Address, out var address, out var addressError))
                {
                    logger.LogWarning(
                        "Skipping venue sync for {ProviderName}/{ProviderId}: {ErrorCode}",
                        providerName,
                        incomingVenue.ExternalId,
                        addressError);
                    continue;
                }

                var syncResult = existingVenue.SyncExternalData(incomingVenue.Name, address, coordinates);
                if (syncResult.IsFailure)
                {
                    logger.LogInformation(
                        "Venue {ProviderName}/{ProviderId} was skipped during sync: {ErrorCode}",
                        providerName,
                        incomingVenue.ExternalId,
                        syncResult.Error.Code);
                }

                continue;
            }

            TryResolveCoordinates(incomingVenue, existing: null, out var newCoordinates, out _);
            TryResolveAddress(incomingVenue, existing: null, out var newAddress, out _);

            var newVenueResult = Venue.CreateFromProvider(
                incomingVenue.Name,
                providerName,
                incomingVenue.ExternalId,
                newAddress,
                newCoordinates);

            if (newVenueResult.IsFailure)
            {
                logger.LogWarning(
                    "Failed to create venue {ProviderName}/{ProviderId}: {ErrorCode}",
                    providerName,
                    incomingVenue.ExternalId,
                    newVenueResult.Error.Code);
                continue;
            }

            venueRepository.Add(newVenueResult.Value);
            venueIdMap[incomingVenue.ExternalId] = newVenueResult.Value.Id;
        }
    }

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
}

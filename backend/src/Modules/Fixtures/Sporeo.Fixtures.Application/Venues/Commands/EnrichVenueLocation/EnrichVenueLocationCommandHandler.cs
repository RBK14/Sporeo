using Microsoft.Extensions.Logging;
using Sporeo.BuildingBlocks.Application.Abstractions.Execution;
using Sporeo.BuildingBlocks.Domain.Results;
using Sporeo.Fixtures.Application.Venues.Abstractions;
using Sporeo.Fixtures.Application.Venues.Data;
using Sporeo.Fixtures.Domain.Venues.Enums;
using DomainErrors = Sporeo.Fixtures.Domain.Common.Errors;
using Errors = Sporeo.Fixtures.Application.Common.Errors;

namespace Sporeo.Fixtures.Application.Venues.Commands.EnrichVenueLocation;

internal sealed class EnrichVenueLocationCommandHandler(
    IVenueRepository venueRepository,
    IGeocodingService geocodingService,
    ILogger<EnrichVenueLocationCommandHandler> logger) : ICommandHandler<EnrichVenueLocationCommand>
{
    public async Task<Result> Handle(EnrichVenueLocationCommand request, CancellationToken cancellationToken)
    {
        var venue = await venueRepository.GetByIdAsync(request.VenueId, cancellationToken);

        if (venue is null)
            return Result.Failure(DomainErrors.Venue.NotFound(request.VenueId));

        if (venue.Coordinates is not null)
            return Result.Success();

        var locationResult = await geocodingService.GetVenueLocationAsync(
            venue.Name,
            venue.Address?.Street,
            venue.Address?.City,
            venue.Address?.Country,
            cancellationToken);

        if (locationResult.IsFailure)
        {
            logger.LogWarning(
                "Geocoding failed for venue {VenueId}. Reason: {Error}",
                request.VenueId,
                locationResult.Error);

            // Returning success commits the recorded failure so administrators can review the venue.
            var status = locationResult.Error.Code == Errors.Geocoding.NotFound.Code
                ? GeocodingStatus.NotFound
                : GeocodingStatus.Failed;

            return venue.MarkGeocodingFailed(status, locationResult.Error.Code);
        }

        var locationData = locationResult.Value;
        var addressToUpdate = locationData.Address ?? venue.Address;
        var updateResult = venue.UpdateLocation(addressToUpdate, locationData.Coordinates);

        return updateResult.IsFailure ? updateResult : Result.Success();
    }
}

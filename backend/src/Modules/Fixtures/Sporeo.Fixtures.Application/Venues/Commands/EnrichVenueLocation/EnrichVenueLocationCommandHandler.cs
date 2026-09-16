using Sporeo.BuildingBlocks.Application.Abstractions.Execution;
using Sporeo.BuildingBlocks.Domain.Results;
using Sporeo.Fixtures.Application.Abstractions.Geocoding;
using Sporeo.Fixtures.Application.Abstractions.Repositories;
using Sporeo.Fixtures.Domain.Venues.ValueObjects;

namespace Sporeo.Fixtures.Application.Venues.Commands.EnrichVenueLocation;

internal sealed class EnrichVenueLocationCommandHandler(
    IVenueRepository venueRepository,
    IGeocodingService geocodingService) : ICommandHandler<EnrichVenueLocationCommand>
{
    public async Task<Result> Handle(EnrichVenueLocationCommand request, CancellationToken cancellationToken)
    {
        var venueId = VenueId.FromValue(request.VenueId);
        var venue = await venueRepository.GetByIdAsync(venueId, cancellationToken);

        if (venue is null || venue.Coordinates is not null)
            return Result.Success();

        var locationResult = await geocodingService.GetVenueLocationAsync(
            venue.Name,
            venue.Address?.Street,
            venue.Address?.City,
            venue.Address?.Country,
            cancellationToken);

        if (locationResult.IsFailure)
            return locationResult;

        var locationData = locationResult.Value;
        var addressToUpdate = locationData.Address ?? venue.Address;
        var updateResult = venue.UpdateLocation(addressToUpdate, locationData.Coordinates);

        return updateResult.IsFailure ? updateResult : Result.Success();
    }
}

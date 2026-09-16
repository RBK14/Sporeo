using Sporeo.BuildingBlocks.Domain.Models;
using Sporeo.BuildingBlocks.Domain.Results;
using Sporeo.Fixtures.Domain.Common;

namespace Sporeo.Fixtures.Domain.Venues.ValueObjects;

/// <summary>
/// Represents geographic coordinates for a venue location.
/// </summary>
public sealed class Coordinates : ValueObject
{
    /// <summary>
    /// Gets the latitude in decimal degrees, in the range -90 to 90.
    /// </summary>
    public double Latitude { get; }

    /// <summary>
    /// Gets the longitude in decimal degrees, in the range -180 to 180.
    /// </summary>
    public double Longitude { get; }

    private Coordinates(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }

    /// <summary>
    /// Creates validated <see cref="Coordinates"/> from the specified latitude and longitude.
    /// </summary>
    /// <param name="latitude">The latitude in decimal degrees. Must be between -90 and 90.</param>
    /// <param name="longitude">The longitude in decimal degrees. Must be between -180 and 180.</param>
    /// <returns>A successful result containing the coordinates, or a failure when values are out of range.</returns>
    public static Result<Coordinates> Create(double latitude, double longitude)
    {
        if (latitude < -90 || latitude > 90)
            return Result.Failure<Coordinates>(Errors.Venue.Coordinates.InvalidLatitude);

        if (longitude < -180 || longitude > 180)
            return Result.Failure<Coordinates>(Errors.Venue.Coordinates.InvalidLongitude);

        return new Coordinates(latitude, longitude);
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Latitude;
        yield return Longitude;
    }

#pragma warning disable CS8618
    /// <summary>
    /// Initializes a new instance of the <see cref="Coordinates"/> class.
    /// Intended for use by object-relational mappers.
    /// </summary>
    private Coordinates() { }
#pragma warning restore CS8618
}

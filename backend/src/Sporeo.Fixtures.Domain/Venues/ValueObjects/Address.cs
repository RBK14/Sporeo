using Sporeo.BuildingBlocks.Domain.Models;
using Sporeo.BuildingBlocks.Domain.Results;
using Sporeo.Fixtures.Domain.Common;

namespace Sporeo.Fixtures.Domain.Venues.ValueObjects;

/// <summary>
/// Represents a venue postal address composed of optional street, city, and country components.
/// </summary>
public sealed class Address : ValueObject
{
    /// <summary>
    /// Gets the street name and number, if specified.
    /// </summary>
    public string? Street { get; }

    /// <summary>
    /// Gets the city name, if specified.
    /// </summary>
    public string? City { get; }

    /// <summary>
    /// Gets the country name, if specified.
    /// </summary>
    public string? Country { get; }

    private Address(string? street, string? city, string? country)
    {
        Street = street;
        City = city;
        Country = country;
    }

    /// <summary>
    /// Creates a validated <see cref="Address"/> from the specified components.
    /// </summary>
    /// <param name="street">The street name and number, or <see langword="null"/> when not provided.</param>
    /// <param name="city">The city name, or <see langword="null"/> when not provided.</param>
    /// <param name="country">The country name, or <see langword="null"/> when not provided.</param>
    /// <returns>A successful result containing the address, or a failure when a provided component is empty or whitespace.</returns>
    public static Result<Address> Create(string? street, string? city, string? country)
    {
        if (street is not null && string.IsNullOrWhiteSpace(street))
            return Result.Failure<Address>(Errors.Venue.Address.EmptyStreet);

        if (city is not null && string.IsNullOrWhiteSpace(city))
            return Result.Failure<Address>(Errors.Venue.Address.EmptyCity);

        if (country is not null && string.IsNullOrWhiteSpace(country))
            return Result.Failure<Address>(Errors.Venue.Address.EmptyCountry);

        return new Address(street, city, country);
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return Country;
    }

#pragma warning disable CS8618
    /// <summary>
    /// Initializes a new instance of the <see cref="Address"/> class.
    /// Intended for use by object-relational mappers.
    /// </summary>
    private Address() { }
#pragma warning restore CS8618
}

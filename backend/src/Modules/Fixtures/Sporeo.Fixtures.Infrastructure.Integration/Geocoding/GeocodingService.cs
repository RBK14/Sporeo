using System.Globalization;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Sporeo.BuildingBlocks.Domain.Results;
using Sporeo.Fixtures.Application.Abstractions.Geocoding;
using Sporeo.Fixtures.Domain.Venues.ValueObjects;

namespace Sporeo.Fixtures.Infrastructure.Integration.Geocoding;

/// <summary>
/// Nominatim-backed geocoding service with process-wide rate limiting.
/// </summary>
internal sealed class GeocodingService(
    HttpClient httpClient,
    IGeocodingRateLimiter rateLimiter,
    ILogger<GeocodingService> logger) : IGeocodingService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly Error HttpError = new("Geocoding.HttpError", "Failed to contact the geocoding API.");
    private static readonly Error NotFound = new("Geocoding.NotFound", "Location not found.");
    private static readonly Error RateLimited = new("Geocoding.RateLimited", "Geocoding rate limit was exceeded.");
    private static readonly Error ParseError = new("Geocoding.ParseError", "Failed to parse geocoding API data.");

    /// <inheritdoc />
    public async Task<Result<GeocodedLocation>> GetVenueLocationAsync(
        string venueName,
        string? street,
        string? city,
        string? country,
        CancellationToken cancellationToken = default)
    {
        var searchParts = new[] { venueName, street, city, country }
            .Where(part => !string.IsNullOrWhiteSpace(part));

        var searchQuery = string.Join(", ", searchParts);
        var requestUri =
            $"search?q={Uri.EscapeDataString(searchQuery)}&format=json&limit=1&addressdetails=1";

        try
        {
            await rateLimiter.WaitAsync(cancellationToken);

            using var response = await httpClient.GetAsync(
                requestUri,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
                return Result.Failure<GeocodedLocation>(RateLimited);

            if (!response.IsSuccessStatusCode)
                return Result.Failure<GeocodedLocation>(HttpError);

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var results = await JsonSerializer.DeserializeAsync<NominatimResponse[]>(stream, JsonOptions, cancellationToken);
            var location = results?.FirstOrDefault();

            if (location is null)
                return Result.Failure<GeocodedLocation>(NotFound);

            if (!double.TryParse(location.Latitude, CultureInfo.InvariantCulture, out var latitude) ||
                !double.TryParse(location.Longitude, CultureInfo.InvariantCulture, out var longitude))
            {
                return Result.Failure<GeocodedLocation>(ParseError);
            }

            var coordinatesResult = Coordinates.Create(latitude, longitude);
            if (coordinatesResult.IsFailure)
                return Result.Failure<GeocodedLocation>(coordinatesResult.Error);

            Address? domainAddress = null;
            if (location.Address is not null)
            {
                var resolvedCity = location.Address.City ?? location.Address.Town ?? location.Address.Village;
                if (!string.IsNullOrWhiteSpace(resolvedCity) &&
                    !string.IsNullOrWhiteSpace(location.Address.Country))
                {
                    var addressResult = Address.Create(
                        location.Address.Road,
                        resolvedCity,
                        location.Address.Country);

                    if (addressResult.IsSuccess)
                        domainAddress = addressResult.Value;
                }
            }

            return Result.Success(new GeocodedLocation(coordinatesResult.Value, domainAddress));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Unexpected geocoding failure for venue {VenueName}.", venueName);
            return Result.Failure<GeocodedLocation>(
                new Error("Geocoding.Exception", "An unexpected error occurred while processing the geocoding request."));
        }
    }
}

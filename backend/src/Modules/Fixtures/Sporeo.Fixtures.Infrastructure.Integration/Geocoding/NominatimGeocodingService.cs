using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sporeo.BuildingBlocks.Application.Abstractions.Caching;
using Sporeo.BuildingBlocks.Domain.Results;
using Sporeo.Fixtures.Application.Venues.Abstractions.Geocoding;
using Sporeo.Fixtures.Domain.Venues.ValueObjects;
using Sporeo.Fixtures.Infrastructure.Integration.Configuration;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Sporeo.Fixtures.Infrastructure.Integration.Geocoding;

/// <summary>
/// Nominatim-backed geocoding service with Redis caching and process-wide rate limiting.
/// </summary>
internal sealed class NominatimGeocodingService(
    HttpClient httpClient,
    ICacheService cacheService,
    IGeocodingRateLimiter rateLimiter,
    IOptions<NominatimOptions> options,
    ILogger<NominatimGeocodingService> logger) : IGeocodingService
{
    private const string CacheKeyPrefix = "geocoding:";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly Error HttpError = new("Geocoding.HttpError", "Failed to contact the geocoding API.");
    private static readonly Error NotFound = new("Geocoding.NotFound", "Location not found.");
    private static readonly Error RateLimited = new("Geocoding.RateLimited", "Geocoding rate limit was exceeded.");
    private static readonly Error ParseError = new("Geocoding.ParseError", "Failed to parse geocoding API data.");

    private static readonly string[] WordsToRemove =
    [
        "The",
    ];

    /// <inheritdoc />
    public async Task<Result<GeocodedLocation>> GetVenueLocationAsync(
        string venueName,
        string? street,
        string? city,
        string? country,
        CancellationToken cancellationToken = default)
    {
        var sanitizedVenueName = SanitizeVenueName(venueName);
        var normalizedAddress = NormalizeAddress(sanitizedVenueName, street, city, country);

        var cacheKey = $"{CacheKeyPrefix}{normalizedAddress}";

        var cached = await cacheService.GetAsync<GeocodingCacheEntry>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            if (!cached.IsFound)
                return Result.Failure<GeocodedLocation>(NotFound);

            var cachedLocation = TryMapCachedLocation(cached);
            if (cachedLocation is null)
                return Result.Failure<GeocodedLocation>(ParseError);

            return Result.Success(cachedLocation);
        }

        var requestUri =
            $"search?q={Uri.EscapeDataString(normalizedAddress)}&format=json&limit=1&addressdetails=1";

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
            {
                await cacheService.SetAsync(
                    cacheKey,
                    GeocodingCacheEntry.Miss(),
                    options.Value.MissCacheTtl,
                    cancellationToken);
                return Result.Failure<GeocodedLocation>(NotFound);
            }

            if (!double.TryParse(location.Latitude, CultureInfo.InvariantCulture, out var latitude) ||
                !double.TryParse(location.Longitude, CultureInfo.InvariantCulture, out var longitude))
            {
                return Result.Failure<GeocodedLocation>(ParseError);
            }

            var coordinatesResult = Coordinates.Create(latitude, longitude);
            if (coordinatesResult.IsFailure)
                return Result.Failure<GeocodedLocation>(coordinatesResult.Error);

            Address? domainAddress = null;
            if (location.Address is not null &&
                !string.IsNullOrWhiteSpace(location.Address.Country))
            {
                var resolvedCity = location.Address.City ?? location.Address.Town ?? location.Address.Village;
                var addressResult = Address.Create(
                    location.Address.Road,
                    string.IsNullOrWhiteSpace(resolvedCity) ? null : resolvedCity,
                    location.Address.Country);

                if (addressResult.IsSuccess)
                    domainAddress = addressResult.Value;
            }

            var geocoded = new GeocodedLocation(coordinatesResult.Value, domainAddress);

            await cacheService.SetAsync(
                cacheKey,
                GeocodingCacheEntry.Found(geocoded),
                options.Value.FoundCacheTtl,
                cancellationToken);

            return Result.Success(geocoded);
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

    /// <summary>
    /// Primitive cache payload for Redis JSON serialization.
    /// Domain value objects (<see cref="Coordinates"/>, <see cref="Address"/>) are not System.Text.Json-friendly.
    /// </summary>
    private sealed record GeocodingCacheEntry(
        bool IsFound,
        double? Latitude,
        double? Longitude,
        string? Street,
        string? City,
        string? Country)
    {
        public static GeocodingCacheEntry Miss() =>
            new(false, null, null, null, null, null);

        public static GeocodingCacheEntry Found(GeocodedLocation location) =>
            new(
                true,
                location.Coordinates.Latitude,
                location.Coordinates.Longitude,
                location.Address?.Street,
                location.Address?.City,
                location.Address?.Country);
    }

    private static GeocodedLocation? TryMapCachedLocation(GeocodingCacheEntry cached)
    {
        if (cached.Latitude is null || cached.Longitude is null)
            return null;

        var coordinatesResult = Coordinates.Create(cached.Latitude.Value, cached.Longitude.Value);
        if (coordinatesResult.IsFailure)
            return null;

        Address? address = null;
        if (!string.IsNullOrWhiteSpace(cached.Country))
        {
            var addressResult = Address.Create(cached.Street, cached.City, cached.Country);
            if (addressResult.IsSuccess)
                address = addressResult.Value;
        }

        return new GeocodedLocation(coordinatesResult.Value, address);
    }

    private static string SanitizeVenueName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return name;

        var sanitized = name.Trim();

        foreach (var word in WordsToRemove)
        {
            var prefix = word + " ";
            if (sanitized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                sanitized = sanitized.Substring(prefix.Length).TrimStart();
            }

            var suffix = " " + word;
            if (sanitized.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                sanitized = sanitized.Substring(0, sanitized.Length - suffix.Length).TrimEnd();
            }
        }

        return sanitized;
    }

    private static string NormalizeAddress(
        string venueName,
        string? street,
        string? city,
        string? country)
    {
        var parts = new[] { venueName, street, city, country }
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Select(part => CollapseWhitespace(part!).ToLowerInvariant());

        return string.Join(", ", parts);
    }

    private static string CollapseWhitespace(string value)
    {
        var builder = new StringBuilder(value.Length);
        var previousWasWhitespace = false;

        foreach (var character in value.Trim())
        {
            if (char.IsWhiteSpace(character))
            {
                if (previousWasWhitespace)
                    continue;

                builder.Append(' ');
                previousWasWhitespace = true;
                continue;
            }

            builder.Append(character);
            previousWasWhitespace = false;
        }

        return builder.ToString();
    }
}

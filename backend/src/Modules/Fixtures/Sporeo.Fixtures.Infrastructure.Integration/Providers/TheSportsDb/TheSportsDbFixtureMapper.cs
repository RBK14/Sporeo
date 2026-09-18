using System.Globalization;
using Microsoft.Extensions.Logging;
using Sporeo.Fixtures.Application.Fixtures.Abstractions;
using Sporeo.Fixtures.Domain.Fixtures.Enums;
using Sporeo.Fixtures.Infrastructure.Integration.Observability;

namespace Sporeo.Fixtures.Infrastructure.Integration.Providers.TheSportsDb;

/// <summary>
/// Maps TheSportsDB event payloads to application fixture DTOs.
/// </summary>
internal sealed class TheSportsDbFixtureMapper(ILogger<TheSportsDbFixtureMapper> logger)
{
    private const string ProviderName = "TheSportsDB";
    private const string DropReasonMissingId = "missing_id";
    private const string DropReasonInvalidDate = "invalid_date";

    public MapResult MapToDto(TheSportsDbEvent apiEvent, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(apiEvent.IdEvent))
            return MapResult.Dropped(DropReasonMissingId);

        if (!TryParseStartDate(apiEvent.StrTimestamp, apiEvent.DateEvent, apiEvent.StrTime, out var startDate))
            return MapResult.Dropped(DropReasonInvalidDate);

        // todo: consider remove veanue ID from mapping, as it is not reliable and we are not using it for venue upsert anyway
        ExternalFixtureVenueDto? venueDto = null;
        if (!string.IsNullOrWhiteSpace(apiEvent.StrVenue))
        {
            var venueProviderId = string.IsNullOrWhiteSpace(apiEvent.IdVenue)
                ? $"fallback-{apiEvent.StrVenue.Trim().Replace(" ", "-").ToLowerInvariant()}"
                : apiEvent.IdVenue;

            var country = string.IsNullOrWhiteSpace(apiEvent.StrCountry) ? null : apiEvent.StrCountry.Trim();

            venueDto = new ExternalFixtureVenueDto(
                ExternalId: venueProviderId,
                ProviderName: ProviderName,
                Name: apiEvent.StrVenue,
                Street: null,
                City: null,
                Country: country,
                Latitude: null,
                Longitude: null);
        }

        return MapResult.Mapped(new ExternalFixtureDto(
            ExternalId: apiEvent.IdEvent,
            ProviderName: ProviderName,
            Name: string.IsNullOrWhiteSpace(apiEvent.StrEvent) ? "Unknown Match" : apiEvent.StrEvent,
            SeasonName: string.IsNullOrWhiteSpace(apiEvent.StrSeason) ? "Unknown Season" : apiEvent.StrSeason,
            StartDate: startDate,
            Status: MapFixtureStatus(apiEvent.StrStatus, apiEvent.IdEvent, relativePath),
            Venue: venueDto));
    }

    private static bool TryParseStartDate(
        string? strTimestamp,
        string? dateEvent,
        string? strTime,
        out DateTimeOffset startDate)
    {
        if (!string.IsNullOrWhiteSpace(strTimestamp) &&
            TryParseAsUtc(strTimestamp.Trim(), out startDate))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(dateEvent) &&
            !string.IsNullOrWhiteSpace(strTime))
        {
            var raw = $"{dateEvent.Trim()} {strTime.Trim()}";
            if (TryParseAsUtc(raw, out startDate))
                return true;
        }

        startDate = default;
        return false;
    }

    private static bool TryParseAsUtc(string value, out DateTimeOffset startDate)
    {
        if (DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out startDate))
        {
            return true;
        }

        string[] formats =
        [
            "yyyy-MM-dd'T'HH:mm:ss",
            "yyyy-MM-dd'T'HH:mm:ssK",
            "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFK",
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-dd HH:mm"
        ];

        if (DateTimeOffset.TryParseExact(
                value,
                formats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out startDate))
        {
            return true;
        }

        startDate = default;
        return false;
    }

    private FixtureStatus MapFixtureStatus(string? apiStatus, string eventId, string relativePath)
    {
        switch (apiStatus)
        {
            case "FT":
            case "AET":
            case "PEN":
                return FixtureStatus.Finished;

            case "NS":
                return FixtureStatus.Scheduled;

            case "PST":
            case "POST":
                return FixtureStatus.Postponed;

            case "CANC":
            case "ABD":
                return FixtureStatus.Cancelled;

            default:
                FixturesIntegrationMetrics.UnknownStatuses.Add(
                    1,
                    new KeyValuePair<string, object?>("provider", ProviderName));

                logger.LogWarning(
                    "Unknown TheSportsDb status '{ApiStatus}' for event {EventId} on {Path}; defaulting to Scheduled.",
                    apiStatus ?? "(null)",
                    eventId,
                    relativePath);

                return FixtureStatus.Scheduled;
        }
    }

    internal readonly record struct MapResult(ExternalFixtureDto? Dto, string? DropReason)
    {
        public static MapResult Mapped(ExternalFixtureDto dto) => new(dto, null);
        public static MapResult Dropped(string reason) => new(null, reason);
    }
}

using Sporeo.BuildingBlocks.Domain.Results;

namespace Sporeo.Fixtures.Application.Common;

/// <summary>
/// Defines stable application-layer error codes for the fixtures bounded context.
/// </summary>
public static class Errors
{
    /// <summary>
    /// Errors related to the admin catalog use cases.
    /// </summary>
    public static class Catalog
    {
        /// <summary>
        /// Returned when cached catalog data is missing and the client must refresh.
        /// </summary>
        public static readonly Error CacheExpired = new(
            "Catalog.CacheExpired",
            "Catalog data expired. Please refresh the page.");

        /// <summary>
        /// Returned when a catalog mutation request is missing required provider identity.
        /// </summary>
        public static readonly Error InvalidRequest = new(
            "Catalog.InvalidRequest",
            "Provider name is missing in the request.");
    }

    /// <summary>
    /// Errors related to external fixtures provider calls.
    /// </summary>
    public static class ExternalFixtures
    {
        /// <summary>
        /// Returned when the provider rejects API credentials.
        /// </summary>
        public static readonly Error Unauthorized = new(
            "ExternalFixtures.Unauthorized",
            "Provider rejected the API credentials.");

        /// <summary>
        /// Returned when the provider rate limit is exceeded.
        /// </summary>
        public static readonly Error RateLimited = new(
            "ExternalFixtures.RateLimited",
            "Provider rate limit was exceeded.");

        /// <summary>
        /// Returned when the provider reports a transient failure.
        /// </summary>
        public static readonly Error Transient = new(
            "ExternalFixtures.Transient",
            "Provider returned a transient failure.");

        /// <summary>
        /// Returned when the provider reports a permanent failure.
        /// </summary>
        public static readonly Error Permanent = new(
            "ExternalFixtures.Permanent",
            "Provider returned a permanent failure.");

        /// <summary>
        /// Returned when the provider payload cannot be parsed or is empty when required.
        /// </summary>
        public static readonly Error InvalidPayload = new(
            "ExternalFixtures.InvalidPayload",
            "Provider returned an invalid payload.");

        /// <summary>
        /// Returned when a long-term sync is requested without an external season identifier.
        /// </summary>
        public static readonly Error MissingSeasonId = new(
            "ExternalFixtures.MissingSeasonId",
            "Long term sync requires ExternalSeasonId.");
    }
}

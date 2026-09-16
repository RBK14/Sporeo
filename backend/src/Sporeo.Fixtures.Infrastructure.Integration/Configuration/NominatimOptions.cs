using System.ComponentModel.DataAnnotations;

namespace Sporeo.Fixtures.Infrastructure.Integration.Configuration;

/// <summary>
/// Configuration options for the Nominatim geocoding client.
/// </summary>
public sealed class NominatimOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "ExternalProviders:Nominatim";

    /// <summary>
    /// Gets the geocoding endpoint base URL. Prefer a self-hosted or commercial instance for production batch enrichment.
    /// </summary>
    [Required, Url]
    public string BaseUrl { get; init; } = "https://nominatim.openstreetmap.org";

    /// <summary>
    /// Gets the identifying User-Agent required by Nominatim usage policy.
    /// </summary>
    [Required, MinLength(8)]
    public string UserAgent { get; init; } = "Sporeo/1.0 (fixtures-worker; contact@example.com)";

    /// <summary>
    /// Gets the minimum interval between geocoding requests for the process-wide limiter.
    /// </summary>
    [Required]
    public TimeSpan MinRequestInterval { get; init; } = TimeSpan.FromMilliseconds(1100);
}

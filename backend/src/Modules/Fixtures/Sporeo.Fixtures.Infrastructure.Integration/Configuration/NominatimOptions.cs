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
    public string UserAgent { get; init; } = "Sporeo/1.0 (fixtures-worker; contact@sporeo.app)";

    /// <summary>
    /// Gets the minimum interval between geocoding requests for the process-wide limiter.
    /// Must be at least one second to comply with Nominatim usage policy.
    /// </summary>
    [Required]
    public TimeSpan MinRequestInterval { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Gets how long successful geocoding results remain cached.
    /// </summary>
    [Required]
    public TimeSpan FoundCacheTtl { get; init; } = TimeSpan.FromDays(90);

    /// <summary>
    /// Gets how long negative geocoding results (not found) remain cached.
    /// </summary>
    [Required]
    public TimeSpan MissCacheTtl { get; init; } = TimeSpan.FromDays(30);
}

using System.ComponentModel.DataAnnotations;

namespace Sporeo.Fixtures.Infrastructure.Integration.Configuration;

/// <summary>
/// Configuration options for TheSportsDB HTTP client.
/// </summary>
public sealed class TheSportsDbOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "ExternalProviders:TheSportsDb";

    /// <summary>
    /// Gets the HTTPS base URL of TheSportsDB API (without API key segment).
    /// </summary>
    [Required]
    public string BaseUrl { get; init; } = string.Empty;

    /// <summary>
    /// Gets the API key. Must be supplied via environment variables, user secrets, or a secret store.
    /// </summary>
    [Required]
    public string ApiKey { get; init; } = string.Empty;
}

using System.ComponentModel.DataAnnotations;

namespace Sporeo.Fixtures.Api.OpenApi;

public sealed class ApiDocumentationOptions
{
    public const string SectionName = "ApiDocumentation";

    public bool? Enabled { get; init; }

    public bool RequireAuthorization { get; init; }

    [Required]
    [MinLength(1)]
    public string Title { get; init; } = "Sporeo Fixtures API";

    public string? Description { get; init; }

    public string? ContactName { get; init; }

    [EmailAddress]
    public string? ContactEmail { get; init; }

    [Url]
    public string? ContactUrl { get; init; }

    public string? LicenseName { get; init; }

    [Url]
    public string? LicenseUrl { get; init; }

    public IReadOnlyList<ApiServerOptions> Servers { get; init; } = [];

    [Required]
    public SecurityOptions Security { get; init; } = new();
}

public sealed class ApiServerOptions
{
    [Required]
    [Url]
    public string Url { get; init; } = string.Empty;

    public string? Description { get; init; }
}

public sealed class SecurityOptions
{
    [Required]
    [MinLength(1)]
    public string SchemeName { get; init; } = "BearerAuth";

    [Required]
    [MinLength(1)]
    public string BearerFormat { get; init; } = "JWT";

    public string? Description { get; init; } =
        "JWT Bearer token issued by the Sporeo Identity service. " +
        "Paste the access token without the 'Bearer ' prefix.";
}

using System.Diagnostics;
using System.Text.RegularExpressions;
using OpenTelemetry;

namespace Sporeo.ServiceDefaults.Telemetry;

/// <summary>
/// Redacts sensitive HTTP URL fragments from OpenTelemetry activities before export.
/// </summary>
internal sealed partial class SensitiveHttpUrlRedactingProcessor : BaseProcessor<Activity>
{
    /// <inheritdoc />
    public override void OnEnd(Activity activity)
    {
        RedactTag(activity, "url.full");
        RedactTag(activity, "http.url");
        RedactTag(activity, "url.query");
        RedactTag(activity, "http.target");
        RedactTag(activity, "url.path");
    }

    private static void RedactTag(Activity activity, string tagName)
    {
        var value = activity.GetTagItem(tagName)?.ToString();
        if (string.IsNullOrWhiteSpace(value))
            return;

        var redacted = SensitiveUrlRedactor.Redact(value);
        if (!string.Equals(value, redacted, StringComparison.Ordinal))
            activity.SetTag(tagName, redacted);
    }
}

/// <summary>
/// Shared URL redaction helpers for HTTP telemetry and diagnostics.
/// </summary>
public static partial class SensitiveUrlRedactor
{
    /// <summary>
    /// Redacts TheSportsDB API key path segments and Nominatim query parameters.
    /// </summary>
    /// <param name="value">The raw URL or path fragment.</param>
    /// <returns>A redacted value safe for logs and telemetry.</returns>
    public static string Redact(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        var redacted = TheSportsDbApiKeyRegex().Replace(value, "$1***$3");
        redacted = NominatimQueryRegex().Replace(redacted, "$1q=REDACTED");
        return redacted;
    }

    [GeneratedRegex(@"(https?://[^/\s]+/api/v1/json/)([^/\s]+)(/)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex TheSportsDbApiKeyRegex();

    [GeneratedRegex(@"([?&])q=[^&]*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex NominatimQueryRegex();
}

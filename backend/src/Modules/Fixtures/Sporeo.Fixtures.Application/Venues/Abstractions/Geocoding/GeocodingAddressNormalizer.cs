using System.Text;

namespace Sporeo.Fixtures.Application.Venues.Abstractions.Geocoding;

/// <summary>
/// Builds a deterministic cache key from venue name and address parts.
/// </summary>
public static class GeocodingAddressNormalizer
{
    /// <summary>
    /// Creates a normalized, culture-invariant address key used for cache lookups.
    /// </summary>
    /// <param name="venueName">The venue display name.</param>
    /// <param name="street">The street address, if known.</param>
    /// <param name="city">The city, if known.</param>
    /// <param name="country">The country, if known.</param>
    /// <returns>A trimmed, lower-invariant, comma-separated address key.</returns>
    public static string Normalize(
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

using Microsoft.Extensions.Options;
using Sporeo.Fixtures.Infrastructure.Integration.Configuration;

namespace Sporeo.Fixtures.Infrastructure.Integration.Providers.TheSportsDb;

/// <summary>
/// Inserts the TheSportsDB API key into the request path immediately before sending,
/// keeping the key out of <see cref="HttpClient.BaseAddress"/> and configuration snapshots used by telemetry.
/// </summary>
internal sealed class TheSportsDbApiKeyHandler(IOptions<TheSportsDbOptions> options) : DelegatingHandler
{
    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var apiKey = options.Value.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("TheSportsDB API key is not configured.");

        if (request.RequestUri is null)
            throw new InvalidOperationException("TheSportsDB request URI is missing.");

        // Podmieniamy placeholder na właściwy klucz API
        var builder = new UriBuilder(request.RequestUri);
        builder.Path = builder.Path.Replace("[API_KEY]", apiKey.Trim('/'));
        request.RequestUri = builder.Uri;

        return base.SendAsync(request, cancellationToken);
    }
}
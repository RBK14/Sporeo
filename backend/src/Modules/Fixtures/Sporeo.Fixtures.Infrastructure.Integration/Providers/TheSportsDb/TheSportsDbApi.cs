using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Sporeo.BuildingBlocks.Domain.Results;

namespace Sporeo.Fixtures.Infrastructure.Integration.Providers.TheSportsDb;

/// <summary>
/// HTTP transport for TheSportsDB API with typed error mapping.
/// </summary>
internal sealed class TheSportsDbApi(
    HttpClient httpClient,
    ILogger<TheSportsDbApi> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    internal static readonly Error Unauthorized = new("ExternalFixtures.Unauthorized", "Provider rejected the API credentials.");
    internal static readonly Error RateLimited = new("ExternalFixtures.RateLimited", "Provider rate limit was exceeded.");
    internal static readonly Error Transient = new("ExternalFixtures.Transient", "Provider returned a transient failure.");
    internal static readonly Error Permanent = new("ExternalFixtures.Permanent", "Provider returned a permanent failure.");
    internal static readonly Error InvalidPayload = new("ExternalFixtures.InvalidPayload", "Provider returned an invalid payload.");

    public async Task<Result<T>> FetchAsync<T>(string requestUri, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.GetAsync(
                requestUri,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                return Result.Failure<T>(Unauthorized);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
                return Result.Failure<T>(RateLimited);

            if ((int)response.StatusCode >= 500)
                return Result.Failure<T>(Transient);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "TheSportsDb API error: {Path} returned status {StatusCode}",
                    requestUri,
                    (int)response.StatusCode);
                return Result.Failure<T>(Permanent);
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var data = await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken);

            if (data is null)
                return Result.Failure<T>(InvalidPayload);

            return Result.Success(data);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError("Transient HTTP failure while fetching {Path}: {ExceptionType}.", requestUri, ex.GetType().Name);
            return Result.Failure<T>(Transient);
        }
        catch (JsonException ex)
        {
            logger.LogError("Invalid JSON payload while fetching {Path}: {ExceptionType}.", requestUri, ex.GetType().Name);
            return Result.Failure<T>(InvalidPayload);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError("Unexpected error while fetching {Path}: {ExceptionType}.", requestUri, ex.GetType().Name);
            return Result.Failure<T>(Permanent);
        }
    }
}

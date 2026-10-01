using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Sporeo.BuildingBlocks.Domain.Results;
using Errors = Sporeo.Fixtures.Application.Common.Errors;

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

    public async Task<Result<T>> FetchAsync<T>(string requestUri, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.GetAsync(
                requestUri,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                return Result.Failure<T>(Errors.ExternalFixtures.Unauthorized);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
                return Result.Failure<T>(Errors.ExternalFixtures.RateLimited);

            if ((int)response.StatusCode >= 500)
                return Result.Failure<T>(Errors.ExternalFixtures.Transient);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "TheSportsDb API error: {Path} returned status {StatusCode}",
                    requestUri,
                    (int)response.StatusCode);
                return Result.Failure<T>(Errors.ExternalFixtures.Permanent);
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var data = await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken);

            if (data is null)
                return Result.Failure<T>(Errors.ExternalFixtures.InvalidPayload);

            return Result.Success(data);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError("Transient HTTP failure while fetching {Path}: {ExceptionType}.", requestUri, ex.GetType().Name);
            return Result.Failure<T>(Errors.ExternalFixtures.Transient);
        }
        catch (JsonException ex)
        {
            logger.LogError("Invalid JSON payload while fetching {Path}: {ExceptionType}.", requestUri, ex.GetType().Name);
            return Result.Failure<T>(Errors.ExternalFixtures.InvalidPayload);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError("Unexpected error while fetching {Path}: {ExceptionType}.", requestUri, ex.GetType().Name);
            return Result.Failure<T>(Errors.ExternalFixtures.Permanent);
        }
    }
}

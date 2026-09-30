using Sporeo.BuildingBlocks.Domain.Results;
using AppErrors = Sporeo.Fixtures.Application.Common.Errors;
using DomainErrors = Sporeo.Fixtures.Domain.Common.Errors;

namespace Sporeo.Fixtures.Api.Extensions;

internal static class ResultExtensions
{
    public static IResult ToProblemResult(this Error error)
    {
        if (error is ValidationError validationError)
        {
            var errors = validationError.Errors
                .GroupBy(e => e.Code)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.Message).ToArray());

            return Results.ValidationProblem(errors);
        }

        var statusCode = error.Code switch
        {
            var code when code is
                DomainErrors.Fixture.NotFoundCode or
                DomainErrors.Venue.NotFoundCode or
                DomainErrors.Sport.NotFoundCode or
                DomainErrors.League.NotFoundCode or
                DomainErrors.Season.NotFoundCode
                => StatusCodes.Status404NotFound,

            var code when code == AppErrors.Catalog.InvalidRequest.Code
                => StatusCodes.Status400BadRequest,

            var code when code == AppErrors.Catalog.CacheExpired.Code
                => StatusCodes.Status409Conflict,

            var code when code == AppErrors.ExternalFixtures.Transient.Code
                || code == AppErrors.ExternalFixtures.RateLimited.Code
                => StatusCodes.Status503ServiceUnavailable,

            var code when code == AppErrors.ExternalFixtures.Unauthorized.Code
                || code == AppErrors.ExternalFixtures.Permanent.Code
                || code == AppErrors.ExternalFixtures.InvalidPayload.Code
                => StatusCodes.Status502BadGateway,

            _ => StatusCodes.Status500InternalServerError
        };

        return Results.Problem(
            detail: error.Message,
            statusCode: statusCode,
            extensions: new Dictionary<string, object?>
            {
                ["code"] = error.Code
            });
    }
}

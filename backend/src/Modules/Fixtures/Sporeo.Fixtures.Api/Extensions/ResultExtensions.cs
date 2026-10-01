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
                .GroupBy(e => string.IsNullOrWhiteSpace(e.PropertyName) ? e.Code : e.PropertyName!)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.Message).ToArray());

            return Results.ValidationProblem(
                errors,
                extensions: new Dictionary<string, object?>
                {
                    ["codes"] = validationError.Errors
                        .Select(e => e.Code)
                        .Distinct()
                        .ToArray()
                });
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
                || code == AppErrors.Catalog.IdentityMismatch.Code
                || code == AppErrors.Catalog.ItemNotFoundInCache.Code
                || code == DomainErrors.Sport.EmptyName.Code
                || code == DomainErrors.League.EmptyName.Code
                || code == DomainErrors.Season.EmptyName.Code
                || code == DomainErrors.Venue.EmptyName.Code
                || code == DomainErrors.Fixture.EmptyName.Code
                || code == DomainErrors.League.InconsistentHierarchy.Code
                || code == DomainErrors.Season.InconsistentHierarchy.Code
                || code == DomainErrors.League.EmptyProviderName.Code
                || code == DomainErrors.League.EmptyProviderId.Code
                => StatusCodes.Status400BadRequest,

            var code when code == AppErrors.Catalog.CacheExpired.Code
                || code == DomainErrors.Fixture.Deleted.Code
                || code == DomainErrors.Venue.Deleted.Code
                || code == DomainErrors.Sport.Deleted.Code
                || code == DomainErrors.League.Deleted.Code
                || code == DomainErrors.Season.Deleted.Code
                || code == DomainErrors.Fixture.LockedForSync.Code
                || code == DomainErrors.Venue.LockedForSync.Code
                || code == DomainErrors.League.LockedForSync.Code
                || code == DomainErrors.Season.LockedForSync.Code
                || code == DomainErrors.Fixture.Finished.Code
                || code == DomainErrors.Fixture.InvalidStatusTransition.Code
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

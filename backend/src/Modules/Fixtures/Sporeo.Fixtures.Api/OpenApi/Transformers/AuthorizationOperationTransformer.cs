using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;

namespace Sporeo.Fixtures.Api.OpenApi.Transformers;

internal sealed class AuthorizationOperationTransformer(
    IOptions<ApiDocumentationOptions> options) : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        var metadata = context.Description.ActionDescriptor.EndpointMetadata;

        if (metadata.OfType<IAllowAnonymous>().Any())
        {
            return Task.CompletedTask;
        }

        if (!metadata.OfType<IAuthorizeData>().Any())
        {
            return Task.CompletedTask;
        }

        var schemeName = options.Value.Security.SchemeName;

        operation.Security ??= [];
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(schemeName)] = []
        });

        operation.Responses ??= new OpenApiResponses();
        AddProblemResponse(operation.Responses, "401", "Unauthorized");
        AddProblemResponse(operation.Responses, "403", "Forbidden");

        return Task.CompletedTask;
    }

    private static void AddProblemResponse(
        OpenApiResponses responses,
        string statusCode,
        string description)
    {
        if (responses.ContainsKey(statusCode))
        {
            return;
        }

        responses[statusCode] = new OpenApiResponse
        {
            Description = description,
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["application/problem+json"] = new OpenApiMediaType
                {
                    Schema = new OpenApiSchemaReference(nameof(ProblemDetails))
                }
            }
        };
    }
}

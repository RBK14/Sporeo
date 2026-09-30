using Microsoft.Extensions.Options;
using Scalar.AspNetCore;
using Sporeo.Fixtures.Api.OpenApi.Transformers;

namespace Sporeo.Fixtures.Api.OpenApi;

public static class OpenApiDocumentationExtensions
{
    public const string DocumentName = "v1";

    public static IServiceCollection AddOpenApiDocumentation(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<ApiDocumentationOptions>()
            .Bind(configuration.GetSection(ApiDocumentationOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(
                options => options.Servers.All(server =>
                    Uri.TryCreate(server.Url, UriKind.Absolute, out var uri)
                    && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)),
                "ApiDocumentation:Servers entries require absolute HTTP(S) URLs.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.Security.SchemeName),
                "ApiDocumentation:Security:SchemeName is required.")
            .ValidateOnStart();

        services.AddOpenApi(DocumentName, options =>
        {
            options.AddDocumentTransformer<ApiDocumentInfoTransformer>();
            options.AddDocumentTransformer<BearerSecuritySchemeDocumentTransformer>();
            options.AddOperationTransformer<AuthorizationOperationTransformer>();
        });

        return services;
    }

    public static WebApplication MapOpenApiDocumentation(this WebApplication app)
    {
        var settings = app.Services.GetRequiredService<IOptions<ApiDocumentationOptions>>().Value;
        var isExposed = settings.Enabled ?? app.Environment.IsDevelopment();

        if (!isExposed)
        {
            return app;
        }

        var openApi = app.MapOpenApi("/openapi/{documentName}.json")
            .ExcludeFromDescription();

        var scalar = app.MapScalarApiReference("/scalar", options =>
            {
                options
                    .WithTitle(settings.Title)
                    .AddDocument(DocumentName)
                    .AddPreferredSecuritySchemes(settings.Security.SchemeName);
            })
            .ExcludeFromDescription();

        if (settings.RequireAuthorization)
        {
            openApi.RequireAuthorization();
            scalar.RequireAuthorization();
        }

        return app;
    }
}

using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;

namespace Sporeo.Fixtures.Api.OpenApi.Transformers;

internal sealed class ApiDocumentInfoTransformer(
    IOptions<ApiDocumentationOptions> options) : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        var settings = options.Value;

        document.Info ??= new OpenApiInfo();
        document.Info.Title = settings.Title;

        if (!string.IsNullOrWhiteSpace(settings.Description))
        {
            document.Info.Description = settings.Description;
        }

        if (!string.IsNullOrWhiteSpace(settings.ContactName)
            || !string.IsNullOrWhiteSpace(settings.ContactEmail)
            || !string.IsNullOrWhiteSpace(settings.ContactUrl))
        {
            document.Info.Contact = new OpenApiContact
            {
                Name = settings.ContactName,
                Email = settings.ContactEmail,
                Url = TryCreateUri(settings.ContactUrl)
            };
        }

        if (!string.IsNullOrWhiteSpace(settings.LicenseName)
            || !string.IsNullOrWhiteSpace(settings.LicenseUrl))
        {
            document.Info.License = new OpenApiLicense
            {
                Name = settings.LicenseName,
                Url = TryCreateUri(settings.LicenseUrl)
            };
        }

        if (settings.Servers.Count > 0)
        {
            document.Servers = settings.Servers
                .Select(server => new OpenApiServer
                {
                    Url = server.Url,
                    Description = server.Description
                })
                .ToList();
        }

        return Task.CompletedTask;
    }

    private static Uri? TryCreateUri(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) ? uri : null;
}

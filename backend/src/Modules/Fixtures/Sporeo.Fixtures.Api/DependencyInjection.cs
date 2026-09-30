using Mapster;
using MapsterMapper;
using Sporeo.Fixtures.Api.Extensions;
using Sporeo.Fixtures.Api.OpenApi;
using System.Reflection;

namespace Sporeo.Fixtures.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddPresentation(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddEndpoints(Assembly.GetExecutingAssembly());
        services.AddMappings();
        services.AddOpenApiDocumentation(configuration);

        return services;
    }

    private static IServiceCollection AddMappings(this IServiceCollection services)
    {
        var config = TypeAdapterConfig.GlobalSettings;
        config.Scan(typeof(DependencyInjection).Assembly);

        services.AddSingleton(config);
        services.AddScoped<IMapper, ServiceMapper>();

        return services;
    }
}

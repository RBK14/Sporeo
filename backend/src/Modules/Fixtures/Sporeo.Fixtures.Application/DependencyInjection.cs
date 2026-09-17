using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Sporeo.BuildingBlocks.Application.Behaviors;

namespace Sporeo.Fixtures.Application;

/// <summary>
/// Registers Fixtures application-layer services, including MediatR and pipeline behaviors.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds MediatR handlers from this assembly and the shared BuildingBlocks pipeline behaviors.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The same <paramref name="services"/> instance for chaining.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            configuration.AddOpenBehavior(typeof(LoggingBehavior<,>));
            configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
            configuration.AddOpenBehavior(typeof(PerformanceBehavior<,>));
            configuration.AddOpenBehavior(typeof(QueryCachingBehavior<,>));
            configuration.AddOpenBehavior(typeof(CommitBehavior<,>));
        });

        services.AddValidatorsFromAssembly(
            typeof(DependencyInjection).Assembly,
            includeInternalTypes: true);

        return services;
    }
}

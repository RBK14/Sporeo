using Microsoft.Extensions.DependencyInjection;
using Sporeo.BuildingBlocks.Infrastructure.Messaging.Outbox.Abstractions;
using Sporeo.BuildingBlocks.Infrastructure.Messaging.Outbox.Processing;

namespace Sporeo.BuildingBlocks.Infrastructure.Messaging;

/// <summary>
/// Registers shared messaging infrastructure services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds the shared outbox processor and MediatR dispatcher.
    /// Module-specific stores and event type registries must be registered separately.
    /// </summary>
    public static IServiceCollection AddBuildingBlocksMessaging(this IServiceCollection services)
    {

        services.AddScoped<IOutboxMessageDispatcher, MediatROutboxMessageDispatcher>();
        services.AddScoped<IOutboxProcessor, OutboxProcessor>();
        return services;
    }
}

using Sporeo.BuildingBlocks.Infrastructure.Messaging;
using Sporeo.Fixtures.Application;
using Sporeo.Fixtures.Infrastructure.Integration;
using Sporeo.Fixtures.Infrastructure.Persistence;
using Sporeo.Fixtures.Worker;
using Sporeo.Fixtures.Worker.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

var isMigrate = args.Contains("--migrate");

if (isMigrate)
{
    builder.Services.AddFixturesDatabase(builder.Configuration);
}
else
{    
    builder.Services
        .AddWorkerConfiguration(builder.Configuration)
        .AddApplication()
        .AddPersistence(builder.Configuration)
        .AddCaching(builder.Configuration)
        .AddBuildingBlocksMessaging()
        .AddIntegration(builder.Configuration)
        .AddWorkerServices(builder.Configuration);
}

var host = builder.Build();

if (isMigrate)
{
    var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseInitialization");
    await DatabaseInitializer.InitializeAsync(host.Services, logger);
    
    return;
}

host.Run();

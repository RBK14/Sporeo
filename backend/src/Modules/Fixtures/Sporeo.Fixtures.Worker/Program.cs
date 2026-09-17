using Sporeo.BuildingBlocks.Infrastructure.Messaging;
using Sporeo.Fixtures.Application;
using Sporeo.Fixtures.Infrastructure.Integration;
using Sporeo.Fixtures.Infrastructure.Persistence;
using Sporeo.Fixtures.Worker;
using Sporeo.Fixtures.Worker.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddWorkerConfiguration(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddCaching(builder.Configuration);
builder.Services.AddBuildingBlocksMessaging();
builder.Services.AddIntegration(builder.Configuration);
builder.Services.AddWorkerServices(builder.Configuration);

var host = builder.Build();

if (args.Contains("--migrate"))
{
    var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseInitialization");
    await DatabaseInitializer.InitializeAsync(host.Services, logger);
    
    return;
}

host.Run();

using Sporeo.Fixtures.Application;
using Sporeo.Fixtures.Infrastructure.Integration;
using Sporeo.Fixtures.Infrastructure.Persistence;
using Sporeo.Fixtures.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddWorkerConfiguration(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddIntegration(builder.Configuration);
builder.Services.AddWorkerServices(builder.Configuration);

var host = builder.Build();

await host.Services.InitializeFixturesDatabaseAsync();

host.Run();

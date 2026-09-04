using Sporeo.Fixtures.Application;
using Sporeo.Fixtures.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddApplication();
builder.Services.AddPersistence(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
    await app.Services.InitializeFixturesDatabaseAsync();

app.MapDefaultEndpoints();
app.MapGet("/", () => "Hello World!");

app.Run();

using Sporeo.Fixtures.Application;
using Sporeo.Fixtures.Infrastructure.Integration;
using Sporeo.Fixtures.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddApplication();
builder.Services.AddGeocoding(builder.Configuration);
builder.Services.AddPersistence(builder.Configuration);

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapGet("/", () => "Hello World!");

app.Run();

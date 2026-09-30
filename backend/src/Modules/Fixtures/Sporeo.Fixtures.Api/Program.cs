using Sporeo.Fixtures.Api;
using Sporeo.Fixtures.Api.Extensions;
using Sporeo.Fixtures.Api.OpenApi;
using Sporeo.Fixtures.Application;
using Sporeo.Fixtures.Infrastructure.Integration;
using Sporeo.Fixtures.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services
    .AddApplication()
    .AddPersistence(builder.Configuration)
    .AddIntegration(builder.Configuration)
    .AddCaching(builder.Configuration)
    .AddPresentation(builder.Configuration);

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapOpenApiDocumentation();
app.MapEndpoints();

app.Run();

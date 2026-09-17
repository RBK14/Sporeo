using MediatR;
using Sporeo.BuildingBlocks.Application.Pagination;
using Sporeo.Fixtures.Application;
using Sporeo.Fixtures.Application.Catalogs.Queries.GetCatalog;
using Sporeo.Fixtures.Infrastructure.Integration;
using Sporeo.Fixtures.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddApplication();
builder.Services.AddIntegration(builder.Configuration);
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddCaching(builder.Configuration);

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapGet("/", () => "Hello World!");

app.MapGet("/catalog", async (ISender sender) =>
{
    var pagination = new PaginationParams(1, 10);
    var query = new GetCatalogQuery(pagination);
    var result = await sender.Send(query);

    if (result.IsFailure)
    {
        return Results.BadRequest(new
        {
            error = result.Error.Code,
            message = result.Error.Message
        });
    }

    return Results.Ok(result.Value);
});

app.Run();

using MediatR;
using Sporeo.BuildingBlocks.Application.Pagination;
using Sporeo.Fixtures.Application;
using Sporeo.Fixtures.Application.Catalogs.Commands.UpdateMonitoring;
using Sporeo.Fixtures.Application.Catalogs.Queries.GetCatalog;
using Sporeo.Fixtures.Infrastructure.Integration;
using Sporeo.Fixtures.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddApplication()
    .AddIntegration(builder.Configuration)
    .AddPersistence(builder.Configuration)
    .AddCaching(builder.Configuration);

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

app.MapGet("/monitoring", async (ISender sender) =>
{
    var sports = new List<UpdateMonitoringSportDto>
    {
        new UpdateMonitoringSportDto(
            ProviderId: "102",
            ProviderName: "TheSportsDB",
            Leagues: new List<UpdateMonitoringLeagueDto>
            {
                new UpdateMonitoringLeagueDto(
                    ProviderId: "4330",
                    ProviderName: "TheSportsDB",
                    IsMonitored: true
                )
            }
        )
    };

var result = await sender.Send(new UpdateMonitoringCommand(sports));
    if (result.IsFailure)
    {
        return Results.BadRequest(new
        {
            error = result.Error.Code,
            message = result.Error.Message
        });
    }

    return Results.Ok();
});

app.Run();

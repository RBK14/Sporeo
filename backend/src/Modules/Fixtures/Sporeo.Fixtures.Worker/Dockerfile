FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Sporeo.slnx Directory.Packages.props Directory.Build.props ./
COPY .editorconfig ./
COPY src/BuildingBlocks ./src/BuildingBlocks
COPY src/Modules/Fixtures ./src/Modules/Fixtures
COPY src/Platform/Sporeo.ServiceDefaults ./src/Platform/Sporeo.ServiceDefaults

RUN dotnet restore src/Modules/Fixtures/Sporeo.Fixtures.Worker/Sporeo.Fixtures.Worker.csproj
RUN dotnet publish src/Modules/Fixtures/Sporeo.Fixtures.Worker/Sporeo.Fixtures.Worker.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Sporeo.Fixtures.Worker.dll"]

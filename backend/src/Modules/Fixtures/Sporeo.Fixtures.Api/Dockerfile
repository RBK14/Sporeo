FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Sporeo.slnx Directory.Packages.props Directory.Build.props ./
COPY .editorconfig ./
COPY src/BuildingBlocks ./src/BuildingBlocks
COPY src/Modules/Fixtures ./src/Modules/Fixtures
COPY src/Platform/Sporeo.ServiceDefaults ./src/Platform/Sporeo.ServiceDefaults

RUN dotnet restore src/Modules/Fixtures/Sporeo.Fixtures.Api/Sporeo.Fixtures.Api.csproj
RUN dotnet publish src/Modules/Fixtures/Sporeo.Fixtures.Api/Sporeo.Fixtures.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Sporeo.Fixtures.Api.dll"]

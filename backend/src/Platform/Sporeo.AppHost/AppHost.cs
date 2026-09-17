var builder = DistributedApplication.CreateBuilder(args);

var sqlPassword = builder.AddParameter("sql-password", secret: true);
var theSportsDbApiKey = builder.AddParameter("thesportsdb-apikey", secret: true);

var redis = builder.AddRedis("redis");

var sqlServer = builder.AddSqlServer("sql-server", password: sqlPassword)
    .WithDataVolume("sql-server-data");

var fixturesDb = sqlServer.AddDatabase("fixtures-db");
var quartzDb = sqlServer.AddDatabase("quartz-db");

var migrationTask = builder.AddProject<Projects.Sporeo_Fixtures_Worker>("migration-task")
    .WithArgs("--migrate")
    .WithReference(fixturesDb)
    .WithReference(quartzDb)
    .WaitFor(fixturesDb)
    .WaitFor(quartzDb);

builder.AddProject<Projects.Sporeo_Fixtures_Api>("fixtures-api")
    .WithReference(fixturesDb)
    .WithReference(redis)
    .WithEnvironment("ExternalProviders__TheSportsDb__ApiKey", theSportsDbApiKey)
    .WaitForCompletion(migrationTask);

builder.AddProject<Projects.Sporeo_Fixtures_Worker>("fixtures-worker")
    .WithReference(fixturesDb)
    .WithReference(quartzDb)
    .WithReference(redis)
    .WithEnvironment("ExternalProviders__TheSportsDb__ApiKey", theSportsDbApiKey)
    .WaitForCompletion(migrationTask);

builder.Build().Run();

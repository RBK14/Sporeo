var builder = DistributedApplication.CreateBuilder(args);

var sqlPassword = builder.AddParameter("sql-password", "SQLP@ssw0rd");

var sqlServer = builder.AddSqlServer("sql-server", password: sqlPassword)
    .WithDataVolume("sql-server-data");

var fixturesDb = sqlServer.AddDatabase("fixtures-db");
var quartzDb = sqlServer.AddDatabase("quartz-db");

var fixturesApi = builder.AddProject<Projects.Sporeo_Fixtures_Api>("fixtures-api")
    .WithReference(fixturesDb)
    .WaitFor(fixturesDb);

builder.AddProject<Projects.Sporeo_Fixtures_Worker>("fixtures-worker")
    .WithReference(fixturesDb)
    .WithReference(quartzDb)
    .WaitFor(fixturesDb)
    .WaitFor(quartzDb);

builder.Build().Run();

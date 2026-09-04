var builder = DistributedApplication.CreateBuilder(args);

var sqlPassword = builder.AddParameter("sql-password", "SQLP@ssw0rd");

var sqlServer = builder.AddSqlServer("sql-server", password: sqlPassword)
    .WithDataVolume("sql-server-data");

var fixturesDb = sqlServer.AddDatabase("fixtures-db");

var fixturesApi = builder.AddProject<Projects.Sporeo_Fixtures_API>("fixtures-api")
    .WithReference(fixturesDb)
    .WaitFor(fixturesDb);

builder.Build().Run();

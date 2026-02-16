var builder = DistributedApplication.CreateBuilder(args);

var env = builder.AddDockerComposeEnvironment("env");

var sqlserver = builder.AddSqlServer("sql-server")
    .WithDataVolume("sql-server")
    .WithLifetime(ContainerLifetime.Persistent);

var database = sqlserver.AddDatabase("what-you-did");

var webapp = builder.AddProject<Projects.WhatYouDid>("webapp")
    .WaitFor(database)
    .WithReference(database);

builder.Build().Run();

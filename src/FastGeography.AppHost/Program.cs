var builder = DistributedApplication.CreateBuilder(args);

var server = builder.AddProject<Projects.FastGeography_Server>("fastgeography-api");
var client = builder.AddProject<Projects.FastGeography_Client>("fastgeography-client")
    .WithReference(server);

builder.Build().Run();
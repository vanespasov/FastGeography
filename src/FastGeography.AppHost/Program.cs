var builder = DistributedApplication.CreateBuilder(args);

// Hosted Blazor WASM: the Server serves both the UI and the API on one origin.
// Do not AddProject the Client — that would start a second WASM host and break same-origin HttpClient.
var bingMapsKey = builder.AddParameter("bingmaps-apikey", secret: true);

var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin();

var db = postgres.AddDatabase("fastgeography-db");

builder.AddProject<Projects.FastGeography_Server>("fastgeography-server")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/alive")
    .WithEnvironment("BingMaps__ApiKey", bingMapsKey)
    .WithReference(db)
    .WaitFor(db);

builder.Build().Run();

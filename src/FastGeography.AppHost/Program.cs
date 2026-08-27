var builder = DistributedApplication.CreateBuilder(args);

// Hosted Blazor WASM: the Server serves both the UI and the API on one origin.
// Do not AddProject the Client — that would start a second WASM host and break same-origin HttpClient.
var bingMapsKey = builder.AddParameter("bingmaps-apikey", secret: true);

builder.AddProject<Projects.FastGeography_Server>("fastgeography-server")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/alive")
    .WithEnvironment("BingMaps__ApiKey", bingMapsKey);

builder.Build().Run();

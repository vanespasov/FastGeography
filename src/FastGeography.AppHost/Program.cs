var builder = DistributedApplication.CreateBuilder(args);

// FastGeography.Client is a Blazor WASM app hosted by the Server project.
// Only the Server needs to be registered with Aspire.
builder.AddProject<Projects.FastGeography_Server>("fastgeography-server");

builder.Build().Run();

var builder = DistributedApplication.CreateBuilder(args);

// Hosted Blazor WASM: the Server serves both the UI and the API on one origin.
// Do not AddProject the Client — that would start a second WASM host and break same-origin HttpClient.

// Optional provider secrets — leave empty if the default Nominatim provider is used.
var geoNamesUsername = builder.AddParameter("geonames-username", secret: true);
var bingMapsApiKey   = builder.AddParameter("bingmaps-apikey",   secret: true);

var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin();

var db = postgres.AddDatabase("fastgeography-db");

builder.AddProject<Projects.FastGeography_Server>("fastgeography-server")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/alive")
    .WithEnvironment("Geocoding__GeoNames__Username", geoNamesUsername)
    .WithEnvironment("Geocoding__BingMaps__ApiKey",   bingMapsApiKey)
    .WithReference(db)
    .WaitFor(db);

builder.Build().Run();

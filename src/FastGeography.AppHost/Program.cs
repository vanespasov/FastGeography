var builder = DistributedApplication.CreateBuilder(args);

// Hosted Blazor WASM: the Server serves both the UI and the API on one origin.
// Do not AddProject the Client — that would start a second WASM host and break same-origin HttpClient.

// Optional provider secrets — set via AppHost user-secrets (never commit keys).
//   dotnet user-secrets set "Parameters:openai-apikey" "<key>" --project src/FastGeography.AppHost
//   dotnet user-secrets set "Parameters:destination-ai-provider" "OpenAI" --project src/FastGeography.AppHost
var geoNamesUsername = builder.AddParameter("geonames-username", secret: true);
var bingMapsApiKey   = builder.AddParameter("bingmaps-apikey",   secret: true);
var openAiApiKey     = builder.AddParameter("openai-apikey",     secret: true);
var anthropicApiKey  = builder.AddParameter("anthropic-apikey",  secret: true);
var grokApiKey       = builder.AddParameter("grok-apikey",       secret: true);
var destinationAiProvider = builder.AddParameter("destination-ai-provider", "OpenAI");
var destinationAiModel    = builder.AddParameter("destination-ai-model", "gpt-4o-mini");

// Local LLM via Ollama (optional). First run: docker exec -it <ollama> ollama pull llama3.2:3b
// Set Parameters:destination-ai-provider = Ollama to use. Needs ~8 GB RAM for llama3.2:3b on CPU.
var ollama = builder.AddContainer("ollama", "ollama/ollama", tag: "latest")
    .WithHttpEndpoint(port: 11434, targetPort: 11434, name: "http")
    .WithVolume("ollama-models", "/root/.ollama");

var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin();

var db = postgres.AddDatabase("fastgeography-db");

var server = builder.AddProject<Projects.FastGeography_Server>("fastgeography-server")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/alive")
    .WithEnvironment("Geocoding__GeoNames__Username", geoNamesUsername)
    .WithEnvironment("Geocoding__BingMaps__ApiKey", bingMapsApiKey)
    .WithEnvironment("DestinationAi__Provider", destinationAiProvider)
    .WithEnvironment("DestinationAi__Model", destinationAiModel)
    .WithEnvironment("DestinationAi__ApiKey", openAiApiKey)
    .WithEnvironment("OpenAI__ApiKey", openAiApiKey)
    .WithEnvironment("ANTHROPIC_API_KEY", anthropicApiKey)
    .WithEnvironment("GROK_API_KEY", grokApiKey)
    .WithEnvironment("DestinationAi__BaseUrl", $"{ollama.GetEndpoint("http")}/v1")
    .WithReference(db)
    .WaitFor(db)
    .WaitFor(ollama);

builder.Build().Run();

namespace FastGeography.IntegrationTests.Support;

using System.Net.Http.Json;

using FastGeography.Server.Services;
using FastGeography.Shared;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Per-scenario test context injected into step definitions by Reqnroll.
/// Creates a <see cref="WebApplicationFactory{TEntryPoint}"/> with a fake geocoding
/// service so scenarios run without any network calls to Bing Maps.
/// Implements <see cref="IDisposable"/> so Reqnroll disposes it after each scenario.
/// </summary>
public sealed class GameApiContext : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;

    public HttpClient Client { get; }

    // Mutable scenario state written by When/Given steps and read by Then steps
    public HttpResponseMessage? LastResponse { get; set; }
    public GeocodeResult? LastGeocodeResult { get; set; }
    public string? OverlongLocation { get; set; }

    public GameApiContext()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(b => b.ConfigureServices(s =>
                s.AddSingleton<IGeocodingService, FakeGeocodingService>()));

        Client = _factory.CreateClient();
    }

    /// <summary>
    /// Sends GET /bingmaps/{location}/{locationType} and stores the response.
    /// Deserialises the body into <see cref="LastGeocodeResult"/> on success.
    /// </summary>
    public async Task ValidateAsync(string location, string locationType)
    {
        LastResponse = await Client.GetAsync($"/bingmaps/{location}/{locationType}");

        if (LastResponse.IsSuccessStatusCode)
            LastGeocodeResult = await LastResponse.Content.ReadFromJsonAsync<GeocodeResult>();
    }

    public void Dispose()
    {
        Client.Dispose();
        _factory.Dispose();
    }
}

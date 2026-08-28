namespace FastGeography.IntegrationTests.Support;

using System.Net.Http.Json;

using FastGeography.Server.Data;
using FastGeography.Server.Services;
using FastGeography.Shared;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Per-scenario test context injected into step definitions by Reqnroll.
/// Uses InMemory EF Core so no real PostgreSQL is required.
/// </summary>
public sealed class GameApiContext : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;

    public HttpClient Client { get; }

    public HttpResponseMessage? LastResponse { get; set; }
    public GeocodeResult? LastGeocodeResult { get; set; }
    public string? OverlongLocation { get; set; }

    public GameApiContext()
    {
        var dbName = $"IntegrationTestDb-{Guid.NewGuid()}";
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(b =>
            {
                b.UseEnvironment("Testing");
                b.ConfigureTestServices(s =>
                {
                    s.AddSingleton<IGeocodingService, FakeGeocodingService>();

                    // Replace PostgreSQL with InMemory
                    var dbContextDescriptors = s
                        .Where(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>)
                                 || d.ServiceType == typeof(ApplicationDbContext))
                        .ToList();
                    foreach (var d in dbContextDescriptors) s.Remove(d);

                    s.AddDbContext<ApplicationDbContext>(options =>
                        options.UseInMemoryDatabase(dbName));
                });
            });

        Client = _factory.CreateClient();

        // Ensure Identity tables are created in the in-memory DB
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.EnsureCreated();
    }

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

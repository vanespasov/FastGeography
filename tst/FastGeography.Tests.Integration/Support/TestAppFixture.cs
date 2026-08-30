namespace FastGeography.IntegrationTests.Support;

using FastGeography.Server.Data;
using FastGeography.Server.Services;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// xUnit class fixture that creates a single <see cref="WebApplicationFactory"/> shared
/// across all tests in one class. Each test gets its own <see cref="HttpClient"/>
/// so cookies (and therefore auth) are isolated.
/// </summary>
public sealed class TestAppFixture : IDisposable
{
    public WebApplicationFactory<Program> Factory { get; }

    public TestAppFixture()
    {
        var dbName = $"TestFixture-{Guid.NewGuid()}";

        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(b =>
            {
                b.UseEnvironment("Testing");
                b.ConfigureTestServices(s =>
                {
                    s.AddSingleton<IGeocodingService, FakeGeocodingService>();
                    s.AddSingleton<IDestinationStoryService, FakeDestinationStoryService>();

                    var toRemove = s
                        .Where(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>)
                                 || d.ServiceType == typeof(ApplicationDbContext))
                        .ToList();
                    foreach (var d in toRemove) s.Remove(d);

                    s.AddDbContext<ApplicationDbContext>(options =>
                        options.UseInMemoryDatabase(dbName));
                });
            });

        // Initialise schema once
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.EnsureCreated();
    }

    /// <summary>Creates a fresh <see cref="HttpClient"/> with its own cookie jar.</summary>
    public HttpClient NewClient() =>
        Factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    public void Dispose() => Factory.Dispose();
}

namespace FastGeography.Server.Data;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

/// <summary>
/// Used exclusively by <c>dotnet ef</c> tooling (migrations add / update / script).
/// Not referenced at runtime — the real context is configured in Program.cs.
///
/// The connection string is read from
/// <c>src/FastGeography.AppHost/appsettings.json</c> so there is a single place
/// to manage the local development database URL.
/// </summary>
internal sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // dotnet-ef runs with CWD = the Server project folder.
        // One level up lands at src/, then into the AppHost project.
        var appHostSettings = Path.GetFullPath(
            Path.Combine(Directory.GetCurrentDirectory(), "..", "FastGeography.AppHost", "appsettings.json"));

        var configuration = new ConfigurationBuilder()
            .AddJsonFile(appHostSettings, optional: false, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("fastgeography-db")
            ?? throw new InvalidOperationException(
                $"Connection string 'fastgeography-db' was not found. " +
                $"Expected it in: {appHostSettings}");

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(
                connectionString,
                o => o.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.GetName().Name))
            .Options;

        return new ApplicationDbContext(options);
    }
}

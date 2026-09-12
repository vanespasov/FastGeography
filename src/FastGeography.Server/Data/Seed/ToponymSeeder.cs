namespace FastGeography.Server.Data.Seed;

using FastGeography.Server.Data.Entities;

using Microsoft.EntityFrameworkCore;

/// <summary>
/// Idempotent runtime seeder that inserts any <see cref="WellKnownToponyms"/> entries
/// that are not yet present in the <c>Toponyms</c> table.
///
/// Called from <c>Program.cs</c> after <c>MigrateAsync</c> / <c>EnsureCreatedAsync</c>.
/// On PostgreSQL the migration already ran <c>ON CONFLICT DO NOTHING</c>, so this is
/// effectively a no-op.  On InMemory (dev / tests that use <c>EnsureCreated</c>)
/// migrations never run, so this seeder does the initial population.
/// </summary>
public static class ToponymSeeder
{
    public static async Task SeedAsync(
        ApplicationDbContext db,
        CancellationToken cancellationToken = default)
    {
        var seeds = WellKnownToponyms.All;
        if (seeds.Count == 0) return;

        // Load all existing lookup keys in one round-trip.
        var rawKeys = await db.Toponyms
            .Select(t => new { t.NormalizedName, Cat = (int)t.Category, t.LanguageCode })
            .ToListAsync(cancellationToken);

        var existing = rawKeys
            .Select(x => (x.NormalizedName, x.Cat, x.LanguageCode))
            .ToHashSet();

        var toAdd = seeds
            .Where(r => !existing.Contains((r.NormalizedName, (int)r.Category, r.LanguageCode)))
            .Select(r => new Toponym
            {
                Id             = r.Id,
                NormalizedName = r.NormalizedName,
                DisplayName    = r.DisplayName,
                Category       = r.Category,
                LanguageCode   = r.LanguageCode,
                Latitude       = r.Latitude,
                Longitude      = r.Longitude,
                Provider       = r.Provider,
                VerifiedAtUtc  = r.VerifiedAtUtc,
            })
            .ToList();

        if (toAdd.Count == 0) return;

        db.Toponyms.AddRange(toAdd);
        await db.SaveChangesAsync(cancellationToken);
    }
}

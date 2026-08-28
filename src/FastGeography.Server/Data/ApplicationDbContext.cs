namespace FastGeography.Server.Data;

using FastGeography.Server.Data.Entities;

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public sealed class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<PlayerProfile> PlayerProfiles => Set<PlayerProfile>();
    public DbSet<GameRound> GameRounds => Set<GameRound>();
    public DbSet<RoundSubmission> RoundSubmissions => Set<RoundSubmission>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<PlayerProfile>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasOne(p => p.User)
             .WithOne(u => u.Profile)
             .HasForeignKey<PlayerProfile>(p => p.UserId);
            e.HasIndex(p => p.UserId).IsUnique();
        });

        builder.Entity<GameRound>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.Letter).HasColumnType("char(1)");
        });

        builder.Entity<RoundSubmission>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasOne(s => s.Round)
             .WithMany(r => r.Submissions)
             .HasForeignKey(s => s.RoundId);
            e.HasOne(s => s.User)
             .WithMany()
             .HasForeignKey(s => s.UserId);
            e.Ignore(s => s.TotalPoints);
        });
    }
}

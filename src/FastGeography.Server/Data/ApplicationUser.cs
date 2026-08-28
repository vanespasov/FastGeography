namespace FastGeography.Server.Data;

using FastGeography.Server.Data.Entities;

using Microsoft.AspNetCore.Identity;

public sealed class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;

    public PlayerProfile? Profile { get; set; }
}

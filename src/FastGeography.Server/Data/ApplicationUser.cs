namespace FastGeography.Server.Data;

using FastGeography.Server.Data.Entities;

using Microsoft.AspNetCore.Identity;

public sealed class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// ISO 639-1 UI + game language preference ("en" or "mk"). Persisted so the
    /// picker survives sign-in across devices.
    /// </summary>
    public string PreferredLanguage { get; set; } = "en";

    public PlayerProfile? Profile { get; set; }
}

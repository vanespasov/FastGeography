namespace FastGeography.Server.Services;

using System.Security.Claims;

using FastGeography.Server.Data;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

/// <summary>
/// Adds a "display_name" claim to the user's cookie so that server-side hubs
/// (GameHub) can present the player's chosen display name rather than their email.
/// </summary>
public sealed class ApplicationUserClaimsPrincipalFactory
    : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>
{
    public ApplicationUserClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IOptions<IdentityOptions> options)
        : base(userManager, roleManager, options) { }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        if (!string.IsNullOrWhiteSpace(user.DisplayName))
            identity.AddClaim(new Claim("display_name", user.DisplayName));

        return identity;
    }
}

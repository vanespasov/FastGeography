namespace FastGeography.Server.Services;

using FastGeography.Server.Data;
using FastGeography.Server.Data.Entities;

using Microsoft.AspNetCore.Identity;

public sealed class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ApplicationDbContext _db;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ApplicationDbContext db)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _db = db;
    }

    public async Task<IdentityResult> RegisterAsync(string email, string password, string displayName)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            DisplayName = displayName
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded) return result;

        _db.PlayerProfiles.Add(new PlayerProfile
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            CareerPoints = 0,
            GamesPlayed = 0
        });
        await _db.SaveChangesAsync();

        await _signInManager.SignInAsync(user, isPersistent: true);
        return result;
    }

    public async Task<bool> LoginAsync(string email, string password)
    {
        var result = await _signInManager.PasswordSignInAsync(email, password, isPersistent: true, lockoutOnFailure: false);
        return result.Succeeded;
    }

    public async Task LogoutAsync()
    {
        await _signInManager.SignOutAsync();
    }
}

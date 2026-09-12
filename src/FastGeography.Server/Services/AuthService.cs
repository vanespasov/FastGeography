namespace FastGeography.Server.Services;

using FastGeography.Server.Data;
using FastGeography.Server.Data.Entities;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;

public sealed class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ApplicationDbContext _db;
    private readonly IEmailSender _emailSender;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ApplicationDbContext db,
        IEmailSender emailSender,
        IHttpContextAccessor httpContextAccessor)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _db = db;
        _emailSender = emailSender;
        _httpContextAccessor = httpContextAccessor;
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

    public async Task ForgotPasswordAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null) return;

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(System.Text.Encoding.UTF8.GetBytes(token));

        var request = _httpContextAccessor.HttpContext?.Request;
        var baseUrl = request is null
            ? "https://localhost"
            : $"{request.Scheme}://{request.Host}";

        var resetLink =
            $"{baseUrl}/reset-password?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(encodedToken)}";

        await _emailSender.SendAsync(
            email,
            "Reset your FastGeography password",
            $"Use this link to reset your password (valid for 2 hours):\n\n{resetLink}\n\nIf you did not request this, you can ignore this email.");
    }

    public async Task<IdentityResult> ResetPasswordAsync(string email, string token, string newPassword)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
            return IdentityResult.Failed(new IdentityError { Description = "Invalid reset request." });

        string decodedToken;
        try
        {
            decodedToken = System.Text.Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
        }
        catch (FormatException)
        {
            return IdentityResult.Failed(new IdentityError { Description = "Invalid reset token." });
        }

        return await _userManager.ResetPasswordAsync(user, decodedToken, newPassword);
    }
}

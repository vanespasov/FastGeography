namespace FastGeography.Server.Services;

using Microsoft.AspNetCore.Identity;

public interface IAuthService
{
    Task<IdentityResult> RegisterAsync(string email, string password, string displayName);
    Task<bool> LoginAsync(string email, string password);
    Task LogoutAsync();
}

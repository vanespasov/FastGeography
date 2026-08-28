namespace FastGeography.Client.Auth;

using System.Net.Http.Json;
using System.Security.Claims;

using FastGeography.Shared.Dtos;

using Microsoft.AspNetCore.Components.Authorization;

public sealed class CookieAuthenticationStateProvider : AuthenticationStateProvider
{
    private static readonly AuthenticationState Anonymous =
        new(new ClaimsPrincipal(new ClaimsIdentity()));

    private readonly HttpClient _http;
    private UserInfoResponse? _cachedUser;

    public CookieAuthenticationStateProvider(HttpClient http) => _http = http;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        // Return cached state when available to avoid an HTTP round-trip on every
        // component render and to prevent the auth state from appearing as
        // "anonymous" while an async check is in flight.
        if (_cachedUser is not null)
            return BuildState(_cachedUser);

        try
        {
            _cachedUser = await _http.GetFromJsonAsync<UserInfoResponse>("api/auth/userinfo");
        }
        catch
        {
            _cachedUser = null;
        }

        return _cachedUser is null ? Anonymous : BuildState(_cachedUser);
    }

    public async Task<bool> LoginAsync(string email, string password)
    {
        var response = await _http.PostAsJsonAsync(
            "api/auth/login", new LoginRequest(email, password));

        if (!response.IsSuccessStatusCode) return false;

        // Clear the cache so GetAuthenticationStateAsync fetches fresh data.
        _cachedUser = null;
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        return true;
    }

    public async Task<(bool Success, string? Error)> RegisterAsync(
        string email, string password, string displayName)
    {
        var response = await _http.PostAsJsonAsync(
            "api/auth/register", new RegisterRequest(email, password, displayName));

        if (response.IsSuccessStatusCode)
        {
            // Auto-login after registration by calling login
            await LoginAsync(email, password);
            return (true, null);
        }

        var body = await response.Content.ReadFromJsonAsync<ErrorBody>();
        return (false, string.Join("; ", body?.Errors ?? ["Registration failed."]));
    }

    public async Task LogoutAsync()
    {
        await _http.PostAsync("api/auth/logout", null);
        _cachedUser = null;
        NotifyAuthenticationStateChanged(Task.FromResult(Anonymous));
    }

    public UserInfoResponse? CurrentUser => _cachedUser;

    private static AuthenticationState BuildState(UserInfoResponse user)
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, user.UserId),
            new Claim(ClaimTypes.Name, user.DisplayName),
            new Claim(ClaimTypes.Email, user.Email),
        ], "cookie");

        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    private sealed record ErrorBody(List<string>? Errors);
}

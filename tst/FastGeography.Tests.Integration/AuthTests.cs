namespace FastGeography.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;

using FastGeography.IntegrationTests.Support;
using FastGeography.Shared.Dtos;

public sealed class AuthTests : IClassFixture<TestAppFixture>
{
    private readonly TestAppFixture _fixture;

    public AuthTests(TestAppFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Register_WithValidCredentials_Returns200()
    {
        var client = _fixture.NewClient();
        var resp = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("auth1@test.com", "Pass123", "Tester1"));

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task Register_ThenGetUserInfo_ReturnsDisplayName()
    {
        var client = _fixture.NewClient();
        await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("userinfo@test.com", "Pass123", "InfoGuy"));

        var info = await client.GetFromJsonAsync<UserInfoResponse>("/api/auth/userinfo");

        Assert.NotNull(info);
        Assert.Equal("InfoGuy", info!.DisplayName);
    }

    [Fact]
    public async Task Login_WithCorrectPassword_Returns200()
    {
        var client = _fixture.NewClient();
        await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("login1@test.com", "Pass123", "LoginUser"));

        var resp = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("login1@test.com", "Pass123"));

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var client = _fixture.NewClient();
        await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("bad@test.com", "Pass123", "BadUser"));

        var resp = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("bad@test.com", "WrongPassword!"));

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task Logout_ClearsSession_UserInfoReturns401()
    {
        var client = _fixture.NewClient();
        await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("logout@test.com", "Pass123", "LogoutUser"));

        await client.PostAsync("/api/auth/logout", null);

        var resp = await client.GetAsync("/api/auth/userinfo");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task ForgotPassword_UnknownEmail_Returns200_AndSendsNoEmail()
    {
        _fixture.EmailSender.Clear();
        var client = _fixture.NewClient();

        var resp = await client.PostAsJsonAsync("/api/auth/forgot-password",
            new ForgotPasswordRequest($"missing-{Guid.NewGuid():N}@test.com"));

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Empty(_fixture.EmailSender.Sent);
    }

    [Fact]
    public async Task ForgotPassword_KnownEmail_Returns200_AndSendsResetLink()
    {
        _fixture.EmailSender.Clear();
        var client = _fixture.NewClient();
        var email = $"reset-{Guid.NewGuid():N}@test.com";

        await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(email, "Pass123", "ResetUser"));

        var resp = await client.PostAsJsonAsync("/api/auth/forgot-password",
            new ForgotPasswordRequest(email));

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Single(_fixture.EmailSender.Sent);
        Assert.Equal(email, _fixture.EmailSender.Sent[0].To);
        Assert.Contains("reset-password", _fixture.EmailSender.Sent[0].Body);
    }

    [Fact]
    public async Task ResetPassword_WithValidToken_AllowsLoginWithNewPassword()
    {
        _fixture.EmailSender.Clear();
        var client = _fixture.NewClient();
        var email = $"resetflow-{Guid.NewGuid():N}@test.com";
        const string oldPassword = "Pass123";
        const string newPassword = "NewPass456";

        await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(email, oldPassword, "ResetFlow"));

        await client.PostAsJsonAsync("/api/auth/forgot-password", new ForgotPasswordRequest(email));

        var token = ExtractTokenFromEmail(_fixture.EmailSender.Sent.Single().Body);
        Assert.NotNull(token);

        var resetResp = await client.PostAsJsonAsync("/api/auth/reset-password",
            new ResetPasswordRequest(email, token, newPassword));
        Assert.Equal(HttpStatusCode.OK, resetResp.StatusCode);

        var oldLogin = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(email, oldPassword));
        Assert.Equal(HttpStatusCode.Unauthorized, oldLogin.StatusCode);

        var newLogin = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(email, newPassword));
        Assert.Equal(HttpStatusCode.OK, newLogin.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_WithInvalidToken_Returns400()
    {
        var client = _fixture.NewClient();
        var email = $"badtoken-{Guid.NewGuid():N}@test.com";

        await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(email, "Pass123", "BadToken"));

        var resp = await client.PostAsJsonAsync("/api/auth/reset-password",
            new ResetPasswordRequest(email, "not-a-valid-token", "NewPass456"));

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    private static string? ExtractTokenFromEmail(string body)
    {
        var match = Regex.Match(body, @"token=([^\s&]+)");
        return match.Success ? Uri.UnescapeDataString(match.Groups[1].Value) : null;
    }
}

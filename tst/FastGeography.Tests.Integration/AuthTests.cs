namespace FastGeography.IntegrationTests;

using System.Net;
using System.Net.Http.Json;

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
}

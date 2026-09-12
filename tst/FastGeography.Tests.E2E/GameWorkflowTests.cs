using Microsoft.Playwright;

namespace FastGeography.Tests.E2E;

/// <summary>
/// Playwright end-to-end tests for the Fast Geography game workflow.
///
/// These tests require the application to be running at <see cref="AppBaseUrl"/>.
/// Run the server first with: dotnet run --project src/FastGeography.Server
///
/// They are skipped in CI because the hosted Blazor WASM cannot be served
/// through <c>WebApplicationFactory</c> alone (it needs the published static files).
/// To execute locally, remove the [Fact(Skip=...)] attribute and start the server.
/// </summary>
public class GameWorkflowTests
{
    private const string AppBaseUrl = "https://localhost:7002";
    private const string StartButtonText = "Start New Adventure!";

    [Fact(Skip = "Requires a running dev server at https://localhost:7002")]
    public async Task PageTitle_ShouldContainFastGeography()
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(
            new BrowserTypeLaunchOptions { Headless = true });

        var page = await browser.NewPageAsync();
        await page.GotoAsync(AppBaseUrl);

        var title = await page.TitleAsync();
        Assert.Contains("FastGeography", title);
    }

    [Fact(Skip = "Requires a running dev server at https://localhost:7002")]
    public async Task StartGame_ShouldShowGameTableAndTimer()
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(
            new BrowserTypeLaunchOptions { Headless = true });

        var page = await browser.NewPageAsync();
        await page.GotoAsync($"{AppBaseUrl}/fastgeography");

        // Wait for Blazor to initialise
        await page.WaitForSelectorAsync($"button:has-text('{StartButtonText}')");

        // Start a game
        await page.ClickAsync($"button:has-text('{StartButtonText}')");

        // Game table should appear
        var table = await page.QuerySelectorAsync("table[aria-label='Geography game answers']");
        Assert.NotNull(table);

        // Timer should be visible and running
        var timer = await page.QuerySelectorAsync(".timer-text");
        Assert.NotNull(timer);
        var timerText = await timer.TextContentAsync();
        Assert.NotNull(timerText);
        Assert.Matches(@"\d{2}:\d{2}", timerText);
    }
}

using System.Threading.Tasks;
using Microsoft.Playwright;
using FastGeography.Server;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FastGeography.Tests.E2E
{
    public class GameWorkflowTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public GameWorkflowTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task StartGame_ShouldInitializeCorrectly()
        {
            // Arrange: Start the application and get the base address
            var client = _factory.CreateClient(); 
            var baseAddress = client.BaseAddress?.ToString() ?? "http://localhost:5000";


            // Act: Use Playwright to navigate to the application
            using var playwright = await Playwright.CreateAsync();
            var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            var page = await browser.NewPageAsync();

            await page.GotoAsync(baseAddress);

            // Assert: Verify the page loads correctly
            var title = await page.TitleAsync();
            Assert.Contains("Fast Geography", title);

            // Act: Start a new game
            await page.ClickAsync("button:has-text('New game')");

            // Assert: Verify the game table is displayed
            var table = await page.QuerySelectorAsync("table");
            Assert.NotNull(table);

            // Assert: Verify the timer starts
            var timer = await page.TextContentAsync("#timer");
            Assert.NotNull(timer);
        }

        [Fact]
        public async Task StartGame_ShouldInitializeCorrectly2()
        {
            using var playwright = await Playwright.CreateAsync();
            var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            var page = await browser.NewPageAsync();

            // Navigate to the app
            await page.GotoAsync("http://localhost:7002");

            // Start a new game
            await page.ClickAsync("button:has-text('New game')");

            // Verify the game table is displayed
            var table = await page.QuerySelectorAsync("table");
            Assert.NotNull(table);

            // Verify the timer starts
            var timer = await page.TextContentAsync("Countdown");
            Assert.NotNull(timer);
        }
    }
}
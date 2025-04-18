using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using FastGeography.Server;

namespace FastGeography.IntegrationTests
{
    public class BingMapsControllerTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public BingMapsControllerTests(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetLocationType_ShouldReturnCorrectPoints()
        {
            // Act
            var response = await _client.GetAsync("/bingmaps/London/City");
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Contains("City:20", result);
        }
    }
}



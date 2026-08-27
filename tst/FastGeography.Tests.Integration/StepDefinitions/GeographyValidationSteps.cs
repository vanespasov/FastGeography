namespace FastGeography.IntegrationTests.StepDefinitions;

using System.Net;

using FastGeography.IntegrationTests.Support;
using FastGeography.Shared;

/// <summary>
/// Step definitions for <c>Features/BingMapsValidation.feature</c>.
/// <see cref="GameApiContext"/> is injected per-scenario by Reqnroll's context injection.
/// </summary>
[Binding]
public sealed class GeographyValidationSteps
{
    private readonly GameApiContext _ctx;

    public GeographyValidationSteps(GameApiContext ctx) => _ctx = ctx;

    // -------------------------------------------------------------------------
    // Given
    // -------------------------------------------------------------------------

    [Given("a location name {int} characters long")]
    public void GivenALocationNameOfLength(int length)
    {
        _ctx.OverlongLocation = new string('A', length);
    }

    // -------------------------------------------------------------------------
    // When
    // -------------------------------------------------------------------------

    [When("I submit {string} as location type {string}")]
    public async Task WhenISubmitLocationAsType(string location, string locationType)
    {
        await _ctx.ValidateAsync(location, locationType);
    }

    [When("I submit that overlong location as location type {string}")]
    public async Task WhenISubmitOverlongLocation(string locationType)
    {
        Assert.NotNull(_ctx.OverlongLocation);
        await _ctx.ValidateAsync(_ctx.OverlongLocation, locationType);
    }

    // -------------------------------------------------------------------------
    // Then
    // -------------------------------------------------------------------------

    [Then("the response is successful")]
    public void ThenResponseIsSuccessful()
    {
        Assert.NotNull(_ctx.LastResponse);
        _ctx.LastResponse.EnsureSuccessStatusCode();
        Assert.NotNull(_ctx.LastGeocodeResult);
    }

    [Then("the awarded points are {int}")]
    public void ThenAwardedPointsAre(int expectedPoints)
    {
        Assert.NotNull(_ctx.LastGeocodeResult);
        Assert.Equal(expectedPoints, _ctx.LastGeocodeResult.Points);
    }

    [Then("the response includes coordinates")]
    public void ThenResponseIncludesCoordinates()
    {
        Assert.NotNull(_ctx.LastGeocodeResult?.Coordinates);
    }

    [Then("the response has no coordinates")]
    public void ThenResponseHasNoCoordinates()
    {
        Assert.Null(_ctx.LastGeocodeResult?.Coordinates);
    }

    [Then("the request is rejected with status code {int}")]
    public void ThenRequestIsRejectedWithStatus(int statusCode)
    {
        Assert.NotNull(_ctx.LastResponse);
        Assert.Equal((HttpStatusCode)statusCode, _ctx.LastResponse.StatusCode);
    }
}

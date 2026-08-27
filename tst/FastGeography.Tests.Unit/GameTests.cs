namespace FastGeography.Tests;

using FastGeography.Shared;

public class GameTests
{
    [Theory]
    [InlineData(0, "table-light")]
    [InlineData(10, "table-success")]
    [InlineData(-5, "table-danger")]
    public void SetCssClass_ReturnsCorrectBootstrapClass(int points, string expected)
    {
        var game = new Game();
        Assert.Equal(expected, game.SetCssClass(points));
    }

    [Fact]
    public void TotalPoints_IsNullSafe_WhenLocationsAreNull()
    {
        var game = new Game(); // all location properties null
        Assert.Equal(0, game.TotalPoints);
    }

    [Fact]
    public void TotalPoints_SumsAllLocationPoints()
    {
        var game = new Game
        {
            City     = new GameLocation { Points = 20 },
            Village  = new GameLocation { Points = -5 },
            Country  = new GameLocation { Points = 20 },
            River    = new GameLocation { Points = 0 },
            Mountain = new GameLocation { Points = -10 }
        };

        Assert.Equal(25, game.TotalPoints);
    }
}

public class ScoringRulesTests
{
    [Fact]
    public void Constants_HaveExpectedValues()
    {
        Assert.Equal(20, ScoringRules.ValidPoints);
        Assert.Equal(-5, ScoringRules.InvalidPoints);
        Assert.Equal(-10, ScoringRules.WrongLetterPoints);
        Assert.Equal(0, ScoringRules.EmptyPoints);
        Assert.Equal(60, ScoringRules.DefaultTimerSeconds);
    }

    [Theory]
    [InlineData(0, "table-light")]
    [InlineData(20, "table-success")]
    [InlineData(-5, "table-danger")]
    public void CssRowClass_ReturnsCorrectClass(int points, string expected)
    {
        Assert.Equal(expected, ScoringRules.CssRowClass(points));
    }
}

public class BadgeCalculatorTests
{
    [Theory]
    [InlineData(-100, Badge.Junior)]
    [InlineData(0,    Badge.Junior)]
    [InlineData(1,    Badge.Junior)]
    [InlineData(100,  Badge.Junior)]
    [InlineData(101,  Badge.Cadet)]
    [InlineData(200,  Badge.Cadet)]
    [InlineData(201,  Badge.Explorer)]
    [InlineData(300,  Badge.Explorer)]
    [InlineData(301,  Badge.Traveller)]
    [InlineData(400,  Badge.Traveller)]
    [InlineData(401,  Badge.Jumper)]
    [InlineData(500,  Badge.Jumper)]
    [InlineData(501,  Badge.EarthSurfer)]
    [InlineData(600,  Badge.EarthSurfer)]
    [InlineData(601,  Badge.EarthConqueror)]
    [InlineData(700,  Badge.EarthConqueror)]
    [InlineData(701,  Badge.SolarSpectre)]
    [InlineData(800,  Badge.SolarSpectre)]
    [InlineData(801,  Badge.GalacticSurfer)]
    [InlineData(900,  Badge.GalacticSurfer)]
    [InlineData(901,  Badge.GalacticConqueror)]
    [InlineData(1000, Badge.GalacticConqueror)]
    [InlineData(9999, Badge.GalacticConqueror)]
    public void Calculate_ReturnsCorrectBadge(int totalPoints, Badge expected)
    {
        Assert.Equal(expected, BadgeCalculator.Calculate(totalPoints));
    }
}

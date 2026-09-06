namespace FastGeography.Tests.Unit;

using FastGeography.Server.Services;
using FastGeography.Shared;

public sealed class DestinationStoryPromptTests
{
    [Fact]
    public void System_English_RequiresEnglishOutput()
    {
        var prompt = DestinationStoryPrompt.System(GameLanguage.En);
        Assert.Contains("English", prompt, StringComparison.Ordinal);
        Assert.Contains("educational", prompt, StringComparison.Ordinal);
        Assert.DoesNotContain("македонски", prompt, StringComparison.Ordinal);
    }

    [Fact]
    public void System_Macedonian_RequiresCyrillicMacedonian()
    {
        var prompt = DestinationStoryPrompt.System(GameLanguage.Mk);
        Assert.Contains("македонски", prompt, StringComparison.Ordinal);
        Assert.Contains("кирилица", prompt, StringComparison.Ordinal);
        Assert.Contains("поучно", prompt, StringComparison.Ordinal);
        Assert.DoesNotContain("Write vivid", prompt, StringComparison.Ordinal);
    }

    [Fact]
    public void UserPrompt_Macedonian_NamesPlaceAndForbidsEnglish()
    {
        var prompt = DestinationStoryPrompt.BuildUserPrompt(
            "Скопје", LocationType.City, "North Macedonia", GameLanguage.Mk);

        Assert.Contains("Скопје", prompt, StringComparison.Ordinal);
        Assert.Contains("градот", prompt, StringComparison.Ordinal);
        Assert.Contains("македонски", prompt, StringComparison.Ordinal);
        Assert.Contains("Не користи англиски", prompt, StringComparison.Ordinal);
    }

    [Fact]
    public void UserPrompt_English_UsesEnglishCategoryName()
    {
        var prompt = DestinationStoryPrompt.BuildUserPrompt(
            "Skopje", LocationType.River, "North Macedonia", GameLanguage.En);

        Assert.Contains("River", prompt, StringComparison.Ordinal);
        Assert.Contains("English", prompt, StringComparison.Ordinal);
        Assert.Contains("Skopje", prompt, StringComparison.Ordinal);
    }

    [Fact]
    public void UserPrompt_English_IncludesAngleAndExistingStories()
    {
        var prompt = DestinationStoryPrompt.BuildUserPrompt(
            "Skopje",
            LocationType.City,
            "North Macedonia",
            GameLanguage.En,
            StoryAngle.Food,
            ["Old opening about the stone bridge."]);

        Assert.Contains("food", prompt, StringComparison.Ordinal);
        Assert.Contains("Do not repeat these openings or facts", prompt, StringComparison.Ordinal);
        Assert.Contains("Old opening about the stone bridge.", prompt, StringComparison.Ordinal);
    }

    [Fact]
    public void UserPrompt_Macedonian_IncludesAngleAndExistingStories()
    {
        var prompt = DestinationStoryPrompt.BuildUserPrompt(
            "Скопје",
            LocationType.City,
            "Северна Македонија",
            GameLanguage.Mk,
            StoryAngle.Nature,
            ["Стар текст за Камениот мост."]);

        Assert.Contains("природа", prompt, StringComparison.Ordinal);
        Assert.Contains("Не ги повторувај овие воведи или факти", prompt, StringComparison.Ordinal);
        Assert.Contains("Стар текст за Камениот мост.", prompt, StringComparison.Ordinal);
    }

    [Fact]
    public void ToKey_RoundTripsThroughTryParseAngle()
    {
        foreach (var angle in DestinationStoryPrompt.AllAngles)
        {
            Assert.True(DestinationStoryPrompt.TryParseAngle(angle.ToKey(), out var parsed));
            Assert.Equal(angle, parsed);
        }
    }
}

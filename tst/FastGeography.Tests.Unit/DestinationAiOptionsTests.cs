namespace FastGeography.Tests.Unit;

using FastGeography.Server.Options;

public sealed class DestinationAiOptionsTests
{
    [Theory]
    [InlineData("None", false)]
    [InlineData("OpenAI", false)]
    [InlineData("Grok", false)]
    [InlineData("Claude", false)]
    [InlineData("Ollama", true)]
    public void IsConfigured_WithoutApiKey_ReturnsExpected(string provider, bool expectedWithoutKey)
    {
        var opts = new DestinationAiOptions
        {
            Provider = provider,
            ApiKey = string.Empty,
            BaseUrl = provider == "Ollama" ? "http://localhost:11434/v1" : string.Empty
        };

        Assert.Equal(expectedWithoutKey, opts.IsConfigured());
    }

    [Fact]
    public void IsConfigured_OpenAI_WithApiKey_ReturnsTrue()
    {
        var opts = new DestinationAiOptions
        {
            Provider = "OpenAI",
            ApiKey = "test-key"
        };

        Assert.True(opts.IsConfigured());
    }

    [Theory]
    [InlineData("OpenAI", "https://api.openai.com/v1")]
    [InlineData("Grok", "https://api.x.ai/v1")]
    [InlineData("Claude", "https://api.anthropic.com")]
    [InlineData("Ollama", "http://localhost:11434/v1")]
    public void GetEffectiveBaseUrl_UsesProviderDefaults(string provider, string expected)
    {
        var opts = new DestinationAiOptions { Provider = provider };
        Assert.Equal(expected, opts.GetEffectiveBaseUrl());
    }

    [Fact]
    public void GetEffectiveBaseUrl_CustomOverride_TakesPrecedence()
    {
        var opts = new DestinationAiOptions
        {
            Provider = "OpenAI",
            BaseUrl = "https://custom.example/v1"
        };

        Assert.Equal("https://custom.example/v1", opts.GetEffectiveBaseUrl());
    }

    [Theory]
    [InlineData("openai", DestinationAiProvider.OpenAI)]
    [InlineData("GROK", DestinationAiProvider.Grok)]
    [InlineData("claude", DestinationAiProvider.Claude)]
    [InlineData("ollama", DestinationAiProvider.Ollama)]
    [InlineData("invalid", DestinationAiProvider.None)]
    public void GetProvider_ParsesCaseInsensitive(string value, DestinationAiProvider expected)
    {
        var opts = new DestinationAiOptions { Provider = value };
        Assert.Equal(expected, opts.GetProvider());
    }
}

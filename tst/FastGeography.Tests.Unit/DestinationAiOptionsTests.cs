namespace FastGeography.Tests.Unit;

using FastGeography.Server.Options;

public sealed class DestinationAiOptionsTests
{
    [Fact]
    public void Auto_FallbackChain_IsOpenAiThenOllama()
    {
        var opts = new DestinationAiOptions { Provider = "Auto" };
        Assert.Equal(
            [DestinationAiProvider.OpenAI, DestinationAiProvider.Ollama],
            opts.GetFallbackChain());
    }

    [Fact]
    public void Auto_WithOpenAiKey_IsConfigured()
    {
        var opts = new DestinationAiOptions
        {
            Provider = "Auto",
            ApiKey = "sk-test"
        };

        Assert.True(opts.IsConfigured());
        Assert.True(opts.IsProviderConfigured(DestinationAiProvider.OpenAI));
    }

    [Fact]
    public void Auto_WithoutOpenAiKey_WithOllamaUrl_IsConfigured()
    {
        var opts = new DestinationAiOptions
        {
            Provider = "Auto",
            OllamaBaseUrl = "http://localhost:11434/v1"
        };

        Assert.True(opts.IsConfigured());
        Assert.False(opts.IsProviderConfigured(DestinationAiProvider.OpenAI));
        Assert.True(opts.IsProviderConfigured(DestinationAiProvider.Ollama));
    }

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
            OllamaBaseUrl = provider == "Ollama" ? "http://localhost:11434/v1" : string.Empty
        };

        Assert.Equal(expectedWithoutKey, opts.IsConfigured());
    }

    [Fact]
    public void OpenAI_UsesDefaultBaseUrlAndModel()
    {
        var opts = new DestinationAiOptions { Provider = "OpenAI", ApiKey = "k" };
        Assert.Equal("https://api.openai.com/v1", opts.GetBaseUrl(DestinationAiProvider.OpenAI));
        Assert.Equal("gpt-4o-mini", opts.GetModel(DestinationAiProvider.OpenAI));
    }

    [Fact]
    public void GetApiKey_OpenAIUsesApiKey_OllamaHasNone()
    {
        var opts = new DestinationAiOptions
        {
            Provider = "OpenAI",
            ApiKey = "openai-key"
        };

        Assert.Equal("openai-key", opts.GetApiKey(DestinationAiProvider.OpenAI));
        Assert.Equal(string.Empty, opts.GetApiKey(DestinationAiProvider.Ollama));
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

    [Fact]
    public void OllamaBaseUrl_DoesNotOverrideOpenAI()
    {
        var opts = new DestinationAiOptions
        {
            Provider = "Auto",
            ApiKey = "k",
            OllamaBaseUrl = "http://ollama:11434/v1"
        };

        Assert.Equal("https://api.openai.com/v1", opts.GetBaseUrl(DestinationAiProvider.OpenAI));
        Assert.Equal("http://ollama:11434/v1", opts.GetBaseUrl(DestinationAiProvider.Ollama));
    }

    [Theory]
    [InlineData("openai", DestinationAiProvider.OpenAI)]
    [InlineData("GROK", DestinationAiProvider.Grok)]
    [InlineData("claude", DestinationAiProvider.Claude)]
    [InlineData("ollama", DestinationAiProvider.Ollama)]
    [InlineData("auto", DestinationAiProvider.Auto)]
    [InlineData("invalid", DestinationAiProvider.Auto)]
    public void GetProvider_ParsesCaseInsensitive(string value, DestinationAiProvider expected)
    {
        var opts = new DestinationAiOptions { Provider = value };
        Assert.Equal(expected, opts.GetProvider());
    }
}

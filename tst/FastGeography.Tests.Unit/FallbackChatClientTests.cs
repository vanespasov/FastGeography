namespace FastGeography.Tests.Unit;

using FastGeography.Server.Services.Ai;

using Microsoft.Extensions.Logging.Abstractions;

public sealed class FallbackChatClientTests
{
    [Fact]
    public async Task CompleteAsync_UsesFirstSuccessfulProvider()
    {
        var first = new StubChatClient(null);
        var second = new StubChatClient("from-ollama");
        var sut = new FallbackChatClient(
            [("OpenAI", first), ("Ollama", second)],
            NullLogger<FallbackChatClient>.Instance);

        var result = await sut.CompleteAsync("sys", "user");

        Assert.Equal("from-ollama", result);
        Assert.Equal(1, first.Calls);
        Assert.Equal(1, second.Calls);
    }

    [Fact]
    public async Task CompleteAsync_DoesNotCallLaterProvidersWhenFirstSucceeds()
    {
        var first = new StubChatClient("from-openai");
        var second = new StubChatClient("from-ollama");
        var sut = new FallbackChatClient(
            [("OpenAI", first), ("Ollama", second)],
            NullLogger<FallbackChatClient>.Instance);

        var result = await sut.CompleteAsync("sys", "user");

        Assert.Equal("from-openai", result);
        Assert.Equal(1, first.Calls);
        Assert.Equal(0, second.Calls);
    }

    private sealed class StubChatClient : IChatCompletionClient
    {
        private readonly string? _result;
        public int Calls { get; private set; }

        public StubChatClient(string? result) => _result = result;

        public Task<string?> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken ct = default)
        {
            Calls++;
            return Task.FromResult(_result);
        }
    }
}

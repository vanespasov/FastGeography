namespace FastGeography.Server.Services.Ai;

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

/// <summary>
/// OpenAI-compatible chat completions (OpenAI, Grok/xAI, Ollama).
/// </summary>
public sealed class OpenAiCompatibleChatClient : IChatCompletionClient
{
    private readonly HttpClient _http;
    private readonly string _model;
    private readonly ILogger<OpenAiCompatibleChatClient> _logger;

    public OpenAiCompatibleChatClient(
        HttpClient http,
        string model,
        string? apiKey,
        ILogger<OpenAiCompatibleChatClient> logger)
    {
        _http = http;
        _model = model;
        _logger = logger;

        if (!string.IsNullOrWhiteSpace(apiKey))
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
    }

    public async Task<string?> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken ct = default)
    {
        var body = new ChatRequest(
            _model,
            [
                new ChatMessage("system", systemPrompt),
                new ChatMessage("user", userPrompt)
            ]);

        try
        {
            using var response = await _http.PostAsJsonAsync("chat/completions", body, ct);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("Chat completion failed ({Status}): {Error}", response.StatusCode, error);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<ChatResponse>(cancellationToken: ct);
            return result?.Choices?.FirstOrDefault()?.Message?.Content?.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Chat completion request failed");
            return null;
        }
    }

    private sealed record ChatRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("messages")] ChatMessage[] Messages);

    private sealed record ChatMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private sealed record ChatResponse(
        [property: JsonPropertyName("choices")] ChatChoice[]? Choices);

    private sealed record ChatChoice(
        [property: JsonPropertyName("message")] ChatMessage? Message);
}

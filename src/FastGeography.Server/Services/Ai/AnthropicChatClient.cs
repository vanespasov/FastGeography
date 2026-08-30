namespace FastGeography.Server.Services.Ai;

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Anthropic Messages API client for Claude models.
/// </summary>
public sealed class AnthropicChatClient : IChatCompletionClient
{
    private readonly HttpClient _http;
    private readonly string _model;
    private readonly ILogger<AnthropicChatClient> _logger;

    public AnthropicChatClient(
        HttpClient http,
        string model,
        string apiKey,
        ILogger<AnthropicChatClient> logger)
    {
        _http = http;
        _model = model;
        _logger = logger;
        _http.DefaultRequestHeaders.Add("x-api-key", apiKey);
        _http.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<string?> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken ct = default)
    {
        var body = new AnthropicRequest(
            _model,
            512,
            systemPrompt,
            [new AnthropicMessage("user", userPrompt)]);

        try
        {
            using var response = await _http.PostAsJsonAsync("v1/messages", body, ct);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("Anthropic request failed ({Status}): {Error}", response.StatusCode, error);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<AnthropicResponse>(cancellationToken: ct);
            var text = result?.Content?.FirstOrDefault(c => c.Type == "text")?.Text;
            return text?.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Anthropic request failed");
            return null;
        }
    }

    private sealed record AnthropicRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("max_tokens")] int MaxTokens,
        [property: JsonPropertyName("system")] string System,
        [property: JsonPropertyName("messages")] AnthropicMessage[] Messages);

    private sealed record AnthropicMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private sealed record AnthropicResponse(
        [property: JsonPropertyName("content")] AnthropicContent[]? Content);

    private sealed record AnthropicContent(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("text")] string? Text);
}

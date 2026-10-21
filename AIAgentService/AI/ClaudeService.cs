using System.Text;
using System.Text.Json;

namespace AIAgentService.AI;

public class ClaudeService : IClaudeService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ClaudeService> _logger;

    public ClaudeService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<ClaudeService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> GenerateResponseAsync(string prompt)
    {
        var apiKey = _configuration["Claude:ApiKey"]?.Trim();
        var model = _configuration["Claude:Model"] ?? "claude-3-5-haiku-20241022";

        if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Contains("YOUR_"))
        {
            _logger.LogInformation("[ClaudeService] Anthropic API key is unconfigured. Falling back to local ERP tools.");
            throw new InvalidOperationException(
                "Anthropic Claude API key is not configured in appsettings.json ('Claude:ApiKey'). Add your key starting with 'sk-ant-api03-...' from https://console.anthropic.com/ or leave empty to use local ERP tools.");
        }

        const string endpoint = "https://api.anthropic.com/v1/messages";

        _logger.LogInformation("[ClaudeService] Calling Anthropic Messages API (Model: {Model})...", model);

        var requestBody = new
        {
            model,
            max_tokens = 1024,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = prompt
                }
            }
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json")
        };

        httpRequest.Headers.Add("x-api-key", apiKey);
        httpRequest.Headers.Add("anthropic-version", "2023-06-01");

        var response = await _httpClient.SendAsync(httpRequest);
        var rawResponse = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("[ClaudeService] Anthropic API call returned status {StatusCode}: {RawResponse}",
                (int)response.StatusCode, rawResponse);

            throw new HttpRequestException(
                $"Anthropic API returned status {(int)response.StatusCode} ({response.StatusCode}): {rawResponse}");
        }

        _logger.LogInformation("[ClaudeService] Anthropic API call completed successfully.");

        using var document = JsonDocument.Parse(rawResponse);
        if (document.RootElement.TryGetProperty("content", out var contentArray) &&
            contentArray.ValueKind == JsonValueKind.Array &&
            contentArray.GetArrayLength() > 0)
        {
            var firstContentBlock = contentArray[0];
            if (firstContentBlock.TryGetProperty("text", out var textElement))
            {
                return textElement.GetString() ?? string.Empty;
            }
        }

        return rawResponse;
    }
}

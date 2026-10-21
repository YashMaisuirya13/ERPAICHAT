using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AIAgentService.AI;

public class GeminiService : IGeminiService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GeminiService> _logger;

    public GeminiService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GeminiService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> GenerateResponseAsync(string prompt)
    {
        var apiKey = _configuration["Gemini:ApiKey"]?.Trim();
        var primaryModel = _configuration["Gemini:Model"] ?? "gemini-1.5-flash";

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("[GeminiService] Gemini API key is missing in appsettings.json ('Gemini:ApiKey').");
            throw new InvalidOperationException(
                "Gemini API key is not configured. Please set your Gemini API key in appsettings.json under 'Gemini:ApiKey'.");
        }

        // Determine authentication mechanism: Bearer token (for OAuth/AQ. tokens) vs API Key header
        bool isBearerToken = apiKey.StartsWith("AQ.", StringComparison.OrdinalIgnoreCase) ||
                             apiKey.StartsWith("ya29.", StringComparison.OrdinalIgnoreCase);

        _logger.LogInformation("[GeminiService] Initiating Gemini request (Auth Mode: {AuthMode})...",
            isBearerToken ? "Bearer Token" : "Google API Key");

        var modelsToTry = new[] { primaryModel, "gemini-1.5-flash", "gemini-1.5-flash-latest", "gemini-2.0-flash", "gemini-3.8-flash" }
            .Distinct()
            .ToList();

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            }
        };

        string lastError = string.Empty;

        foreach (var model in modelsToTry)
        {
            var apiVersions = new[] { "v1beta", "v1" };

            foreach (var version in apiVersions)
            {
                string endpoint = isBearerToken
                    ? $"https://generativelanguage.googleapis.com/{version}/models/{model}:generateContent"
                    : $"https://generativelanguage.googleapis.com/{version}/models/{model}:generateContent?key={apiKey}";

                try
                {
                    _logger.LogInformation("[GeminiService] Trying endpoint '{Version}' with model '{Model}'...", version, model);

                    using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
                    {
                        Content = new StringContent(
                            JsonSerializer.Serialize(requestBody),
                            Encoding.UTF8,
                            "application/json")
                    };

                    if (isBearerToken)
                    {
                        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                    }
                    else
                    {
                        httpRequest.Headers.Add("x-goog-api-key", apiKey);
                    }

                    var response = await _httpClient.SendAsync(httpRequest);
                    var rawResponse = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("[GeminiService] Gemini API call succeeded using model '{Model}' ({Version}).", model, version);

                        using var document = JsonDocument.Parse(rawResponse);
                        if (document.RootElement.TryGetProperty("candidates", out var candidates) &&
                            candidates.ValueKind == JsonValueKind.Array &&
                            candidates.GetArrayLength() > 0)
                        {
                            var firstCandidate = candidates[0];
                            if (firstCandidate.TryGetProperty("content", out var content) &&
                                content.TryGetProperty("parts", out var parts) &&
                                parts.ValueKind == JsonValueKind.Array &&
                                parts.GetArrayLength() > 0)
                            {
                                var text = parts[0].GetProperty("text").GetString();
                                return text ?? string.Empty;
                            }
                        }

                        return rawResponse;
                    }

                    lastError = $"[{version}/{model}] Status {(int)response.StatusCode} ({response.StatusCode}): {rawResponse}";
                    _logger.LogWarning("[GeminiService] Model '{Model}' ({Version}) returned {StatusCode}. Trying next configuration...",
                        model, version, response.StatusCode);
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                }
            }
        }

        _logger.LogError("[GeminiService] All Gemini endpoint attempts failed. Last detail: {LastError}", lastError);

        throw new HttpRequestException(
            $"Google Gemini API call failed. Details: {lastError}");
    }
}

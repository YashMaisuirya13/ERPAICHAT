using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AIAgentService.AI;

public class UnifiedAIService : IAIService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly IERPToolRegistry _toolRegistry;
    private readonly ILogger<UnifiedAIService> _logger;

    public UnifiedAIService(
        HttpClient httpClient,
        IConfiguration configuration,
        IERPToolRegistry toolRegistry,
        ILogger<UnifiedAIService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _toolRegistry = toolRegistry;
        _logger = logger;
    }

    public string ProviderName
    {
        get
        {
            var provider = _configuration["AI:Provider"]
                ?? _configuration["AIProvider"]
                ?? "OpenRouter";

            return provider;
        }
    }

    public string ModelName
    {
        get
        {
            var model = _configuration["AI:Model"]
                ?? _configuration["Nemotron:Model"]
                ?? "nvidia/nemotron-3-ultra-550b-a55b:free";

            return model;
        }
    }

    public async Task<string> GenerateResponseAsync(string prompt)
    {
        var agentResult = await ExecuteChatAgentAsync(prompt);
        return agentResult.Response;
    }

    public async Task<AgentChatResult> ExecuteChatAgentAsync(string prompt, IEnumerable<ChatMessageHistoryItem>? history = null)
    {
        var provider = ProviderName.Trim();

        if (provider.Equals("Anthropic", StringComparison.OrdinalIgnoreCase) ||
            provider.Equals("Claude", StringComparison.OrdinalIgnoreCase))
        {
            return await ExecuteAnthropicDirectAsync(prompt, history);
        }

        if (provider.Equals("Gemini", StringComparison.OrdinalIgnoreCase) ||
            provider.Equals("Google", StringComparison.OrdinalIgnoreCase))
        {
            return await ExecuteGeminiDirectAsync(prompt, history);
        }

        // Standard Universal OpenAI-compatible Engine (OpenRouter, OpenAI, Groq, Ollama, DeepSeek)
        return await ExecuteOpenAiCompatibleAgentAsync(prompt, history);
    }

    #region OpenAI-Compatible Engine (OpenRouter, OpenAI, Groq, Ollama, DeepSeek)

    private async Task<AgentChatResult> ExecuteOpenAiCompatibleAgentAsync(string prompt, IEnumerable<ChatMessageHistoryItem>? history = null)
    {
        var apiKey = _configuration["AI:ApiKey"]?.Trim()
            ?? _configuration["Nemotron:ApiKey"]?.Trim()
            ?? _configuration["Claude:ApiKey"]?.Trim();

        var primaryModel = ModelName;
        var fallbackModel = _configuration["AI:FallbackModel"]
            ?? _configuration["Nemotron:FallbackModel"]
            ?? "openrouter/free";

        var endpoint = _configuration["AI:Endpoint"]
            ?? _configuration["Nemotron:Endpoint"]
            ?? "https://openrouter.ai/api/v1/chat/completions";

        if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Contains("YOUR_"))
        {
            _logger.LogInformation("[UnifiedAIService] AI ApiKey is unconfigured. Falling back to local tools.");
            throw new InvalidOperationException(
                "AI API key is not configured in appsettings.json ('AI:ApiKey'). Please configure an API key.");
        }

        try
        {
            return await ExecuteAgentTurnAsync(endpoint, apiKey, primaryModel, prompt, fallbackModel, history);
        }
        catch (Exception ex) when (!string.IsNullOrWhiteSpace(fallbackModel) && !fallbackModel.Equals(primaryModel, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("[UnifiedAIService] Primary model '{PrimaryModel}' failed ({Message}). Attempting fallback model '{FallbackModel}'...",
                primaryModel, ex.Message, fallbackModel);

            return await ExecuteAgentTurnAsync(endpoint, apiKey, fallbackModel, prompt, null, history);
        }
    }

    private async Task<AgentChatResult> ExecuteAgentTurnAsync(
        string endpoint, string apiKey, string model, string prompt, string? fallbackModel = null, IEnumerable<ChatMessageHistoryItem>? history = null)
    {
        _logger.LogInformation("[UnifiedAIService] Starting Autonomous Agent Loop for prompt: '{Prompt}' with model: '{Model}'", prompt, model);

        var systemMessage = new
        {
            role = "system",
            content = "You are an intelligent ERP AI Assistant for enterprise resource planning (inventory, suppliers, procurement). " +
                      "You have access to database tools. When the user asks for specific ERP data, inventory, suppliers, purchase orders, or creating new products, invoke the appropriate tools. " +
                      "You can create new products in the catalog using create_product when requested. " +
                      "If a request requires multiple steps (for example: finding low stock items, then finding suppliers, and then creating a purchase order), " +
                      "you can invoke tools sequentially: inspect the intermediate tool output from earlier turns, and invoke the next required tool. " +
                      "You also have access to the recent conversation history in this thread. Use prior context (e.g. products, suppliers, or PO numbers discussed earlier) to resolve references like 'it', 'them', or 'that supplier'. " +
                      "Once all required operations are complete, present a clear, professional summary to the user. " +
                      "For casual greetings or conversational questions, respond directly in plain text without invoking any tools."
        };

        var userMessage = new
        {
            role = "user",
            content = prompt
        };

        var messages = new List<object> { systemMessage };

        // Inject prior conversation turns for full context memory
        if (history != null)
        {
            foreach (var h in history)
            {
                if (string.IsNullOrWhiteSpace(h.Content)) continue;
                string role = h.Role.Equals("ai", StringComparison.OrdinalIgnoreCase) ||
                              h.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase)
                    ? "assistant"
                    : "user";

                messages.Add(new
                {
                    role,
                    content = h.Content
                });
            }
        }

        messages.Add(userMessage);
        var tools = _toolRegistry.GetToolDefinitions();
        double temperature = _configuration.GetValue<double>("AI:Temperature", 0.1);
        int maxTokens = _configuration.GetValue<int>("AI:MaxTokens", 512);
        int maxTurns = _configuration.GetValue<int>("AI:MaxAgentTurns", 4);

        var executedToolNames = new List<string>();
        var executedDataResults = new List<object>();
        var formattedMarkdownSnippets = new List<string>();
        string? finalAssistantContent = null;

        for (int turn = 1; turn <= maxTurns; turn++)
        {
            _logger.LogInformation("[UnifiedAIService] Running Agent Turn {Turn}/{MaxTurns} (Model: {Model})...", turn, maxTurns, model);

            var requestBody = new
            {
                model,
                messages = messages.ToArray(),
                tools,
                tool_choice = "auto",
                temperature,
                max_tokens = maxTokens
            };

            var rawResponse = await SendOpenAiCompatibleRequestAsync(endpoint, apiKey, requestBody);
            using var doc = JsonDocument.Parse(rawResponse);

            if (doc.RootElement.TryGetProperty("error", out var errorElement))
            {
                string errorMessage = errorElement.TryGetProperty("message", out var msgProp) ? msgProp.GetString() ?? "Upstream error" : "Upstream error";
                throw new HttpRequestException($"Upstream error from model '{model}': {errorMessage}");
            }

            if (!doc.RootElement.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
            {
                throw new InvalidOperationException($"Invalid completion from model '{model}'. Response: {rawResponse}");
            }

            var messageElement = choices[0].GetProperty("message");

            // Check if model requested Tool Calls in this turn
            if (messageElement.TryGetProperty("tool_calls", out var toolCallsElement) &&
                toolCallsElement.ValueKind == JsonValueKind.Array &&
                toolCallsElement.GetArrayLength() > 0)
            {
                var toolCallsForAssistantMessage = new List<object>();
                var toolExecTasks = new List<Task<(string ToolCallId, string ToolName, string ResultJson, string Markdown, object DataResult)>>();

                foreach (var toolCall in toolCallsElement.EnumerateArray())
                {
                    string toolCallId = toolCall.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? $"call_{turn}" : $"call_{turn}";
                    var func = toolCall.GetProperty("function");
                    string toolName = func.GetProperty("name").GetString() ?? "";
                    string arguments = func.TryGetProperty("arguments", out var argProp) ? argProp.GetString() ?? "{}" : "{}";

                    toolCallsForAssistantMessage.Add(new
                    {
                        id = toolCallId,
                        type = "function",
                        function = new
                        {
                            name = toolName,
                            arguments
                        }
                    });

                    toolExecTasks.Add(Task.Run(async () =>
                    {
                        _logger.LogInformation("[UnifiedAIService] Turn {Turn}: Executing Tool '{ToolName}' with args: {Args}", turn, toolName, arguments);
                        var (resultJson, dataResult) = await _toolRegistry.ExecuteToolAsync(toolName, arguments);
                        var markdown = ERPToolResponseFormatter.FormatToolResultToMarkdown(toolName, resultJson);
                        return (toolCallId, toolName, resultJson, markdown, dataResult);
                    }));
                }

                // Append assistant's tool-call request to conversational history
                messages.Add(new
                {
                    role = "assistant",
                    content = (string?)null,
                    tool_calls = toolCallsForAssistantMessage.ToArray()
                });

                // Await tool executions (concurrent if multiple tools in this turn)
                var batchResults = await Task.WhenAll(toolExecTasks);

                foreach (var res in batchResults)
                {
                    executedToolNames.Add(res.ToolName);
                    if (res.DataResult != null)
                    {
                        executedDataResults.Add(res.DataResult);
                    }
                    formattedMarkdownSnippets.Add(res.Markdown);

                    // Append tool result into conversational history for the next turn
                    messages.Add(new
                    {
                        role = "tool",
                        tool_call_id = res.ToolCallId,
                        content = res.ResultJson
                    });
                }

                // Continue loop: LLM will inspect these tool results in the next turn and can call the next tool or finalize!
                continue;
            }

            // No more tools requested - LLM provided final response
            finalAssistantContent = messageElement.TryGetProperty("content", out var cProp) ? cProp.GetString()?.Trim() : null;
            break;
        }

        // Case 1: Pure conversation (no tools invoked)
        if (executedToolNames.Count == 0)
        {
            return new AgentChatResult
            {
                Response = finalAssistantContent ?? "Hello! I am your ERP Assistant. How can I help you today?",
                ToolCalled = null,
                ToolResult = null
            };
        }

        // Case 2: One or more tools executed (sequential or parallel)
        string combinedToolNames = string.Join(" -> ", executedToolNames);
        object? combinedDataResult = executedDataResults.Count == 1 ? executedDataResults[0] : executedDataResults;

        string finalResponse;
        if (!string.IsNullOrWhiteSpace(finalAssistantContent) && !finalAssistantContent.StartsWith("[{") && !finalAssistantContent.StartsWith("```json"))
        {
            // If LLM produced a synthesized narrative, present the narrative along with the rich ERP tables
            finalResponse = $"{finalAssistantContent}\n\n---\n\n" + string.Join("\n\n---\n\n", formattedMarkdownSnippets);
        }
        else
        {
            finalResponse = string.Join("\n\n---\n\n", formattedMarkdownSnippets);
        }

        return new AgentChatResult
        {
            Response = finalResponse,
            ToolCalled = combinedToolNames,
            ToolResult = combinedDataResult
        };
    }



    private async Task<string> SendOpenAiCompatibleRequestAsync(string endpoint, string apiKey, object requestBody)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json")
        };

        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        httpRequest.Headers.TryAddWithoutValidation("HTTP-Referer", "http://localhost:5048");
        httpRequest.Headers.TryAddWithoutValidation("X-Title", "ERP AI Agent Service");

        var response = await _httpClient.SendAsync(httpRequest);
        var rawResponse = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            var extractedError = TryExtractErrorMessage(rawResponse);
            throw new HttpRequestException($"API returned status {(int)response.StatusCode}: {extractedError ?? rawResponse}");
        }

        return rawResponse;
    }

    private static string? TryExtractErrorMessage(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("error", out var err) &&
                err.TryGetProperty("message", out var msg))
            {
                return msg.GetString();
            }
        }
        catch
        {
            // Ignore parse failure and return null
        }
        return null;
    }

    #endregion

    #region Direct Anthropic / Claude Messages API

    private async Task<AgentChatResult> ExecuteAnthropicDirectAsync(string prompt, IEnumerable<ChatMessageHistoryItem>? history = null)
    {
        var apiKey = _configuration["Anthropic:ApiKey"]
            ?? _configuration["Claude:ApiKey"]
            ?? _configuration["AI:ApiKey"];

        apiKey = apiKey?.Trim();
        var model = _configuration["Anthropic:Model"]
            ?? _configuration["Claude:Model"]
            ?? "claude-3-5-haiku-20241022";

        if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Contains("YOUR_"))
        {
            throw new InvalidOperationException("Anthropic API key is not configured in appsettings.json ('Anthropic:ApiKey').");
        }

        const string endpoint = "https://api.anthropic.com/v1/messages";
        _logger.LogInformation("[UnifiedAIService] Calling Direct Anthropic Messages API (Model: {Model})...", model);

        var messagesList = new List<object>();
        if (history != null)
        {
            foreach (var h in history)
            {
                if (string.IsNullOrWhiteSpace(h.Content)) continue;
                string role = h.Role.Equals("ai", StringComparison.OrdinalIgnoreCase) ||
                              h.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase)
                    ? "assistant"
                    : "user";
                messagesList.Add(new { role, content = h.Content });
            }
        }
        messagesList.Add(new { role = "user", content = prompt });

        var requestBody = new
        {
            model,
            max_tokens = 1024,
            messages = messagesList.ToArray()
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
            throw new HttpRequestException($"Anthropic API returned status {(int)response.StatusCode}: {rawResponse}");
        }

        using var document = JsonDocument.Parse(rawResponse);
        string text = rawResponse;
        if (document.RootElement.TryGetProperty("content", out var contentArray) &&
            contentArray.ValueKind == JsonValueKind.Array &&
            contentArray.GetArrayLength() > 0)
        {
            var firstContentBlock = contentArray[0];
            if (firstContentBlock.TryGetProperty("text", out var textElement))
            {
                text = textElement.GetString() ?? string.Empty;
            }
        }

        return new AgentChatResult
        {
            Response = text,
            ToolCalled = null,
            ToolResult = null
        };
    }

    #endregion

    #region Direct Google Gemini REST API

    private async Task<AgentChatResult> ExecuteGeminiDirectAsync(string prompt, IEnumerable<ChatMessageHistoryItem>? history = null)
    {
        var apiKey = _configuration["Gemini:ApiKey"]
            ?? _configuration["AI:ApiKey"];

        apiKey = apiKey?.Trim();
        var primaryModel = _configuration["Gemini:Model"] ?? "gemini-1.5-flash";

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Gemini API key is not configured in appsettings.json ('Gemini:ApiKey').");
        }

        bool isBearerToken = apiKey.StartsWith("AQ.", StringComparison.OrdinalIgnoreCase) ||
                             apiKey.StartsWith("ya29.", StringComparison.OrdinalIgnoreCase);

        var modelsToTry = new[] { primaryModel, "gemini-1.5-flash", "gemini-2.0-flash" }.Distinct();

        var contentsList = new List<object>();
        if (history != null)
        {
            foreach (var h in history)
            {
                if (string.IsNullOrWhiteSpace(h.Content)) continue;
                string role = h.Role.Equals("ai", StringComparison.OrdinalIgnoreCase) ||
                              h.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase)
                    ? "model"
                    : "user";

                contentsList.Add(new
                {
                    role,
                    parts = new[] { new { text = h.Content } }
                });
            }
        }
        contentsList.Add(new
        {
            role = "user",
            parts = new[] { new { text = prompt } }
        });

        var requestBody = new
        {
            contents = contentsList.ToArray()
        };

        string lastError = string.Empty;

        foreach (var model in modelsToTry)
        {
            foreach (var version in new[] { "v1beta", "v1" })
            {
                string endpoint = isBearerToken
                    ? $"https://generativelanguage.googleapis.com/{version}/models/{model}:generateContent"
                    : $"https://generativelanguage.googleapis.com/{version}/models/{model}:generateContent?key={apiKey}";

                try
                {
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
                        using var document = JsonDocument.Parse(rawResponse);
                        string text = rawResponse;
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
                                text = parts[0].GetProperty("text").GetString() ?? string.Empty;
                            }
                        }

                        return new AgentChatResult
                        {
                            Response = text,
                            ToolCalled = null,
                            ToolResult = null
                        };
                    }

                    lastError = $"[{version}/{model}] Status {(int)response.StatusCode}: {rawResponse}";
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                }
            }
        }

        throw new HttpRequestException($"Google Gemini API call failed. Details: {lastError}");
    }

    #endregion
}

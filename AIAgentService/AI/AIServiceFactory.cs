namespace AIAgentService.AI;

public class AIServiceFactory : IAIService
{
    private readonly INemotronService _nemotronService;
    private readonly IClaudeService _claudeService;
    private readonly IGeminiService _geminiService;
    private readonly IConfiguration _configuration;

    public AIServiceFactory(
        INemotronService nemotronService,
        IClaudeService claudeService,
        IGeminiService geminiService,
        IConfiguration configuration)
    {
        _nemotronService = nemotronService;
        _claudeService = claudeService;
        _geminiService = geminiService;
        _configuration = configuration;
    }

    public string ProviderName
    {
        get
        {
            var provider = _configuration["AIProvider"] ?? "Nemotron";

            if (provider.Equals("Claude", StringComparison.OrdinalIgnoreCase))
                return "Anthropic-Claude";

            if (provider.Equals("Gemini", StringComparison.OrdinalIgnoreCase))
                return "Google-Gemini";

            return "NVIDIA-Nemotron-3-Ultra";
        }
    }

    public async Task<string> GenerateResponseAsync(string prompt)
    {
        var provider = _configuration["AIProvider"] ?? "Nemotron";

        if (provider.Equals("Claude", StringComparison.OrdinalIgnoreCase))
        {
            return await _claudeService.GenerateResponseAsync(prompt);
        }

        if (provider.Equals("Gemini", StringComparison.OrdinalIgnoreCase))
        {
            return await _geminiService.GenerateResponseAsync(prompt);
        }

        return await _nemotronService.GenerateResponseAsync(prompt);
    }

    public async Task<AgentChatResult> ExecuteChatAgentAsync(string prompt)
    {
        // Currently Nemotron / OpenRouter supports native tool calling
        return await _nemotronService.ExecuteChatAgentAsync(prompt);
    }
}

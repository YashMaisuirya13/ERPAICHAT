namespace AIAgentService.AI;

public interface IAIService
{
    string ProviderName { get; }
    Task<string> GenerateResponseAsync(string prompt);
    Task<AgentChatResult> ExecuteChatAgentAsync(string prompt);
}

public class AgentChatResult
{
    public string Response { get; set; } = string.Empty;
    public string? ToolCalled { get; set; }
    public object? ToolResult { get; set; }
}

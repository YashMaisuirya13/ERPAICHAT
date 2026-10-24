namespace AIAgentService.AI;

public class ChatMessageHistoryItem
{
    public string Role { get; set; } = "user"; // "user" or "ai" / "assistant"
    public string Content { get; set; } = string.Empty;
}

public interface IAIService
{
    string ProviderName { get; }
    string ModelName { get; }
    Task<string> GenerateResponseAsync(string prompt);
    Task<AgentChatResult> ExecuteChatAgentAsync(string prompt, IEnumerable<ChatMessageHistoryItem>? history = null);
}

public class AgentChatResult
{
    public string Response { get; set; } = string.Empty;
    public string? ToolCalled { get; set; }
    public object? ToolResult { get; set; }
}

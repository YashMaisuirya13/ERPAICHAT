namespace AIAgentService.AI;

public interface INemotronService
{
    Task<string> GenerateResponseAsync(string prompt);
    Task<AgentChatResult> ExecuteChatAgentAsync(string prompt);
}

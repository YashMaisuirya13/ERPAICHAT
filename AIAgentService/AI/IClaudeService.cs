namespace AIAgentService.AI;

public interface IClaudeService
{
    Task<string> GenerateResponseAsync(string prompt);
}

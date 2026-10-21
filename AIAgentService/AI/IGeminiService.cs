namespace AIAgentService.AI;

public interface IGeminiService
{
    Task<string> GenerateResponseAsync(string prompt);
}

using AIAgentService.Data.Entities;
using AIAgentService.DTOs;

namespace AIAgentService.Services;

public interface IChatHistoryService
{
    Task<List<ChatConversationDto>> GetConversationsAsync(string? userId);
    Task<ChatConversationDetailDto?> GetConversationAsync(Guid conversationGuid, string? userId);
    Task<ChatConversation> GetOrCreateConversationAsync(Guid? conversationGuid, string firstPrompt, string? userId);
    Task SaveMessageAsync(int conversationId, string sender, string content, string? toolCalled, object? toolResult);
    Task<bool> DeleteConversationAsync(Guid conversationGuid, string? userId);
}

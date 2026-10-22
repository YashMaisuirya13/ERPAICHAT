using System.Text.Json;
using AIAgentService.Data;
using AIAgentService.Data.Entities;
using AIAgentService.DTOs;
using Microsoft.EntityFrameworkCore;

namespace AIAgentService.Services;

public class ChatHistoryService : IChatHistoryService
{
    private readonly AIAgentDbContext _dbContext;
    private readonly ILogger<ChatHistoryService> _logger;

    public ChatHistoryService(AIAgentDbContext dbContext, ILogger<ChatHistoryService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<List<ChatConversationDto>> GetConversationsAsync(string? userId)
    {
        var query = _dbContext.Conversations
            .AsNoTracking()
            .Include(c => c.Messages)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(userId))
        {
            query = query.Where(c => c.UserId == userId || c.UserId == null);
        }

        var conversations = await query
            .OrderByDescending(c => c.UpdatedAt)
            .Take(50)
            .ToListAsync();

        return conversations.Select(c =>
        {
            var lastMsg = c.Messages.OrderByDescending(m => m.CreatedAt).FirstOrDefault();
            return new ChatConversationDto
            {
                ConversationGuid = c.ConversationGuid,
                Title = c.Title,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                MessageCount = c.Messages.Count,
                LastMessagePreview = lastMsg != null
                    ? (lastMsg.Content.Length > 80 ? lastMsg.Content.Substring(0, 77) + "..." : lastMsg.Content)
                    : null
            };
        }).ToList();
    }

    public async Task<ChatConversationDetailDto?> GetConversationAsync(Guid conversationGuid, string? userId)
    {
        var query = _dbContext.Conversations
            .AsNoTracking()
            .Include(c => c.Messages)
            .Where(c => c.ConversationGuid == conversationGuid);

        if (!string.IsNullOrWhiteSpace(userId))
        {
            query = query.Where(c => c.UserId == userId || c.UserId == null);
        }

        var conv = await query.FirstOrDefaultAsync();
        if (conv == null) return null;

        return new ChatConversationDetailDto
        {
            ConversationGuid = conv.ConversationGuid,
            Title = conv.Title,
            CreatedAt = conv.CreatedAt,
            UpdatedAt = conv.UpdatedAt,
            Messages = conv.Messages.OrderBy(m => m.CreatedAt).Select(m =>
            {
                object? data = null;
                if (!string.IsNullOrWhiteSpace(m.ToolResultJson))
                {
                    try
                    {
                        data = JsonSerializer.Deserialize<object>(m.ToolResultJson);
                    }
                    catch
                    {
                        data = m.ToolResultJson;
                    }
                }

                return new ChatMessageDto
                {
                    MessageGuid = m.MessageGuid,
                    Sender = m.Sender,
                    Text = m.Content,
                    ToolCalled = m.ToolCalled,
                    DataResult = data,
                    Timestamp = m.CreatedAt
                };
            }).ToList()
        };
    }

    public async Task<ChatConversation> GetOrCreateConversationAsync(Guid? conversationGuid, string firstPrompt, string? userId)
    {
        if (conversationGuid.HasValue)
        {
            var existing = await _dbContext.Conversations
                .FirstOrDefaultAsync(c => c.ConversationGuid == conversationGuid.Value);

            if (existing != null)
            {
                existing.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
                return existing;
            }
        }

        // Generate concise title from prompt
        string cleanPrompt = firstPrompt.Replace("\r", " ").Replace("\n", " ").Trim();
        string title = cleanPrompt.Length > 40 ? cleanPrompt.Substring(0, 37) + "..." : cleanPrompt;
        if (string.IsNullOrWhiteSpace(title)) title = "ERP Chat Session";

        var newConv = new ChatConversation
        {
            ConversationGuid = conversationGuid ?? Guid.NewGuid(),
            Title = title,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Conversations.Add(newConv);
        await _dbContext.SaveChangesAsync();

        return newConv;
    }

    public async Task SaveMessageAsync(int conversationId, string sender, string content, string? toolCalled, object? toolResult)
    {
        string? resultJson = null;
        if (toolResult != null)
        {
            try
            {
                resultJson = JsonSerializer.Serialize(toolResult);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to serialize tool result for message persistence.");
            }
        }

        var msg = new ChatMessage
        {
            ConversationId = conversationId,
            Sender = sender,
            Content = content,
            ToolCalled = toolCalled,
            ToolResultJson = resultJson,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Messages.Add(msg);

        // Update conversation timestamp
        var conv = await _dbContext.Conversations.FindAsync(conversationId);
        if (conv != null)
        {
            conv.UpdatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync();
    }

    public async Task<bool> DeleteConversationAsync(Guid conversationGuid, string? userId)
    {
        var query = _dbContext.Conversations.Where(c => c.ConversationGuid == conversationGuid);
        if (!string.IsNullOrWhiteSpace(userId))
        {
            query = query.Where(c => c.UserId == userId || c.UserId == null);
        }

        var conv = await query.FirstOrDefaultAsync();
        if (conv == null) return false;

        _dbContext.Conversations.Remove(conv);
        await _dbContext.SaveChangesAsync();
        return true;
    }
}

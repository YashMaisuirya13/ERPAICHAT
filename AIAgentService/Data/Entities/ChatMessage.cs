namespace AIAgentService.Data.Entities;

public class ChatMessage
{
    public int Id { get; set; }
    public Guid MessageGuid { get; set; } = Guid.NewGuid();
    public int ConversationId { get; set; }
    public ChatConversation? Conversation { get; set; }

    public string Sender { get; set; } = "user"; // "user" or "ai"
    public string Content { get; set; } = string.Empty;
    public string? ToolCalled { get; set; }
    public string? ToolResultJson { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

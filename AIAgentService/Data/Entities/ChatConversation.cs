namespace AIAgentService.Data.Entities;

public class ChatConversation
{
    public int Id { get; set; }
    public Guid ConversationGuid { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = "New Conversation";
    public string? UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<ChatMessage> Messages { get; set; } = new();
}

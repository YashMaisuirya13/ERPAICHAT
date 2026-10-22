namespace AIAgentService.DTOs;

public class ChatConversationDto
{
    public Guid ConversationGuid { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int MessageCount { get; set; }
    public string? LastMessagePreview { get; set; }
}

public class ChatMessageDto
{
    public Guid MessageGuid { get; set; }
    public string Sender { get; set; } = "user"; // "user" | "ai"
    public string Text { get; set; } = string.Empty;
    public string? ToolCalled { get; set; }
    public object? DataResult { get; set; }
    public DateTime Timestamp { get; set; }
}

public class ChatConversationDetailDto
{
    public Guid ConversationGuid { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<ChatMessageDto> Messages { get; set; } = new();
}

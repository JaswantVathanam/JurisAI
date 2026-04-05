namespace AILegalAsst.Models;

public class ChatMessage
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int SessionId { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsUserMessage { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? Context { get; set; }
    public List<string> RelatedCases { get; set; } = new();
    public List<string> RelatedLaws { get; set; } = new();
}

public class ChatSession
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public UserRole UserRole { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
    public List<ChatMessage> Messages { get; set; } = new();
    public string? CurrentContext { get; set; }
    public int? RelatedCaseId { get; set; }
}

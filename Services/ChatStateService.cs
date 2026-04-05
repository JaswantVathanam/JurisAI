namespace AILegalAsst.Services;

public class ChatStateService
{
    public event Action? OnChange;

    private List<ChatMessage> _messages = new();
    private List<ChatSession> _chatSessions = new();
    private int _currentSessionId = 1;
    private bool _isTyping = false;

    public List<ChatMessage> Messages => _messages;
    public List<ChatSession> ChatSessions => _chatSessions;
    public int CurrentSessionId => _currentSessionId;
    public bool IsTyping => _isTyping;

    public void SetTyping(bool isTyping)
    {
        _isTyping = isTyping;
        NotifyStateChanged();
    }

    public void AddMessage(ChatMessage message)
    {
        _messages.Add(message);
        NotifyStateChanged();
    }

    public void ClearMessages()
    {
        _messages.Clear();
        NotifyStateChanged();
    }

    public void SetMessages(List<ChatMessage> messages)
    {
        _messages = messages;
        NotifyStateChanged();
    }

    public void AddSession(ChatSession session)
    {
        _chatSessions.Insert(0, session);
        NotifyStateChanged();
    }

    public void RemoveSession(int sessionId)
    {
        var session = _chatSessions.FirstOrDefault(s => s.Id == sessionId);
        if (session != null)
        {
            _chatSessions.Remove(session);
            NotifyStateChanged();
        }
    }

    public void UpdateSessionTitle(int sessionId, string newTitle)
    {
        var session = _chatSessions.FirstOrDefault(s => s.Id == sessionId);
        if (session != null)
        {
            session.Title = newTitle;
            NotifyStateChanged();
        }
    }

    public void SetCurrentSession(int sessionId)
    {
        _currentSessionId = sessionId;
        NotifyStateChanged();
    }

    public void InitializeSessions()
    {
        if (_chatSessions.Count == 0)
        {
            _chatSessions = new List<ChatSession>
            {
                new ChatSession 
                { 
                    Id = 1, 
                    Title = "IT Act Section 66", 
                    CreatedAt = DateTime.Now.AddDays(-2) 
                },
                new ChatSession 
                { 
                    Id = 2, 
                    Title = "Cybercrime Complaint Help", 
                    CreatedAt = DateTime.Now.AddDays(-5) 
                }
            };
        }
    }

    public int GetNextSessionId()
    {
        return _chatSessions.Count + 1;
    }

    private void NotifyStateChanged() => OnChange?.Invoke();

    // Chat Models
    public class ChatMessage
    {
        public string Text { get; set; } = string.Empty;
        public bool IsUser { get; set; }
        public bool IsSystem { get; set; }
        public DateTime Timestamp { get; set; }
        public List<string> RelatedLaws { get; set; } = new();
        public List<string>? Options { get; set; }
        
        // Extended properties for case filing
        public MessageType Type { get; set; } = MessageType.Normal;
        public string? CaseId { get; set; }
        public int? CurrentStep { get; set; }
        public int? TotalSteps { get; set; }

        // Orchestration activity tracking
        public List<AgentActivity>? AgentActivities { get; set; }
        public bool IsOrchestrated { get; set; }

        // Permission/confirmation flow
        public bool AwaitingConfirmation { get; set; }
        public string? ConfirmationPrompt { get; set; }
        public string? PendingAction { get; set; }
    }

    public class AgentActivity
    {
        public string AgentName { get; set; } = string.Empty;
        public AgentStatus Status { get; set; } = AgentStatus.Pending;
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string? DataSummary { get; set; }
        public string? ErrorMessage { get; set; }
        public bool IsExpanded { get; set; }

        public string ElapsedDisplay => (StartTime, EndTime) switch
        {
            (not null, not null) => $"{(EndTime.Value - StartTime.Value).TotalSeconds:F1}s",
            (not null, null) => "...",
            _ => ""
        };
    }

    public enum AgentStatus
    {
        Pending,
        Running,
        Completed,
        Failed,
        Skipped
    }

    public class ChatSession
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public SessionType Type { get; set; } = SessionType.GeneralChat;
    }
    
    public enum MessageType
    {
        Normal,
        CaseFilingQuestion,
        CaseFilingAnswer,
        CaseFilingComplete,
        SystemNotification,
        WorkflowUpdate
    }
    
    public enum SessionType
    {
        GeneralChat,
        CaseFiling,
        CaseTracking
    }
}

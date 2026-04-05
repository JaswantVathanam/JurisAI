using AILegalAsst.Models;

namespace AILegalAsst.Services;

public class AILegalChatService
{
    private readonly List<ChatSession> _sessions = new();
    private readonly AuthenticationService _authService;
    private readonly AzureAgentService _agentService;
    private readonly ILogger<AILegalChatService> _logger;
    private int _nextSessionId = 1;
    private int _nextMessageId = 1;

    public AILegalChatService(AuthenticationService authService, AzureAgentService agentService, ILogger<AILegalChatService> logger)
    {
        _authService = authService;
        _agentService = agentService;
        _logger = logger;
    }

    public Task<ChatSession> CreateSessionAsync()
    {
        var user = _authService.GetCurrentUser();
        if (user == null) throw new InvalidOperationException("User must be authenticated");

        var session = new ChatSession
        {
            Id = _nextSessionId++,
            UserId = user.Id,
            UserRole = user.Role,
            StartedAt = DateTime.Now
        };

        _sessions.Add(session);
        return Task.FromResult(session);
    }

    public async Task<ChatMessage> SendMessageAsync(int sessionId, string message, string? context = null)
    {
        var session = _sessions.FirstOrDefault(s => s.Id == sessionId);
        if (session == null) throw new InvalidOperationException("Session not found");

        var userMessage = new ChatMessage
        {
            Id = _nextMessageId++,
            UserId = session.UserId,
            SessionId = sessionId,
            Message = message,
            IsUserMessage = true,
            Timestamp = DateTime.Now,
            Context = context
        };

        session.Messages.Add(userMessage);

        // Generate AI response based on user role and context
        var aiResponse = await GenerateAIResponseAsync(message, session.UserRole, context, session.Messages);
        session.Messages.Add(aiResponse);

        return aiResponse;
    }

    private static bool IsAgentRefusal(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return true;
        var lower = message.ToLowerInvariant();
        return lower.Contains("i'm sorry, but i cannot") ||
               lower.Contains("i cannot assist with that") ||
               lower.Contains("i'm not able to help with") ||
               lower.Contains("i can't assist with that") ||
               (lower.Contains("sorry") && lower.Contains("cannot") && lower.Contains("request"));
    }

    private async Task<ChatMessage> GenerateAIResponseAsync(string userMessage, UserRole userRole, string? context, List<ChatMessage> sessionMessages)
    {
        string response = "";
        var relatedLaws = new List<string>();
        var relatedCases = new List<string>();

        // Try AI Agent first
        if (_agentService.IsReady)
        {
            try
            {
                var roleContext = userRole switch
                {
                    UserRole.Citizen => "You are an AI legal assistant helping an Indian citizen understand cybercrime laws, reporting procedures, and their legal rights. Provide clear, actionable guidance in simple language. Always cite relevant Indian laws (IT Act 2000, IPC/BNS sections).",
                    UserRole.Lawyer => "You are an AI legal assistant for a practicing Indian lawyer. Provide detailed legal analysis, relevant case precedents, section-wise interpretation, and case strategy suggestions for cybercrime cases under Indian law.",
                    UserRole.Police => "You are an AI legal assistant for an Indian police officer. Provide investigation guidance, FIR registration procedures, digital evidence collection protocols, applicable sections, and procedural compliance requirements for cybercrime cases.",
                    _ => "You are an AI legal assistant specializing in Indian cybercrime law."
                };

                var fullContext = string.IsNullOrEmpty(context) ? roleContext : $"{roleContext}\n\nAdditional context: {context}";

                // Use conversation history for better context
                var history = sessionMessages
                    .TakeLast(10)
                    .Select(m => new ConversationMessage
                    {
                        Message = m.Message,
                        IsUser = m.IsUserMessage,
                        Timestamp = m.Timestamp
                    })
                    .ToList();

                var agentResponse = await _agentService.SendMessageWithHistoryAsync(userMessage, history, fullContext);

                if (agentResponse.Success && !IsAgentRefusal(agentResponse.Message))
                {
                    return new ChatMessage
                    {
                        Id = _nextMessageId++,
                        Message = agentResponse.Message,
                        IsUserMessage = false,
                        Timestamp = DateTime.Now,
                        RelatedLaws = agentResponse.RelatedLaws.Count > 0 ? agentResponse.RelatedLaws : relatedLaws,
                        RelatedCases = agentResponse.RelatedCases.Count > 0 ? agentResponse.RelatedCases : relatedCases
                    };
                }

                _logger.LogWarning("AI Agent returned unsuccessful response, falling back to hardcoded: {Error}", agentResponse.Error);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling AI Agent, falling back to hardcoded responses");
            }
        }

        // Fallback: Role-specific hardcoded responses
        switch (userRole)
        {
            case UserRole.Citizen:
                response = GenerateCitizenResponse(userMessage, ref relatedLaws, ref relatedCases);
                break;
            case UserRole.Lawyer:
                response = GenerateLawyerResponse(userMessage, ref relatedLaws, ref relatedCases);
                break;
            case UserRole.Police:
                response = GeneratePoliceResponse(userMessage, ref relatedLaws, ref relatedCases);
                break;
        }

        return new ChatMessage
        {
            Id = _nextMessageId++,
            Message = response,
            IsUserMessage = false,
            Timestamp = DateTime.Now,
            RelatedLaws = relatedLaws,
            RelatedCases = relatedCases
        };
    }

    private string GenerateCitizenResponse(string message, ref List<string> laws, ref List<string> cases)
    {
        message = message.ToLower();
        
        if (message.Contains("report") || message.Contains("complaint") || message.Contains("cybercrime"))
        {
            laws.Add("IT Act 2000");
            return "To report a cybercrime:\n\n1. Visit the nearest Cyber Cell or Police Station\n2. File an FIR with complete details\n3. Preserve all digital evidence (screenshots, emails, messages)\n4. You can also file online at cybercrime.gov.in\n\nWould you like guidance on what information to include in your complaint?";
        }
        
        if (message.Contains("fraud") || message.Contains("scam"))
        {
            laws.Add("Section 66D IT Act");
            laws.Add("Section 420 IPC");
            return "For online fraud cases, you're protected under:\n• IT Act Section 66D (Cheating by personation)\n• IPC Section 420 (Cheating)\n\nImmediate steps:\n1. Stop all communication with the fraudster\n2. Report to cybercrime.gov.in\n3. Inform your bank if financial details are compromised\n4. Collect all evidence\n\nWould you like help with the FIR process?";
        }

        return "I'm here to help you understand cybercrime procedures and your rights. You can ask me about:\n• How to report a cybercrime\n• Types of cybercrimes\n• Your legal rights as a victim\n• Evidence preservation\n• The complaint process\n\nWhat would you like to know?";
    }

    private string GenerateLawyerResponse(string message, ref List<string> laws, ref List<string> cases)
    {
        message = message.ToLower();
        
        if (message.Contains("precedent") || message.Contains("similar case"))
        {
            cases.Add("Shreya Singhal v. Union of India");
            cases.Add("State of Tamil Nadu v. Suhas Katti");
            laws.Add("IT Act 2000");
            return "Key precedents for cybercrime cases:\n\n1. **Shreya Singhal v. UOI (2015)** - Section 66A struck down, freedom of speech\n2. **State of TN v. Suhas Katti (2004)** - First cyberstalking conviction\n3. **Avnish Bajaj v. State (2005)** - Intermediary liability\n\nFor your specific case, I'd recommend reviewing the applicable sections and their judicial interpretations. Would you like detailed analysis of any particular precedent?";
        }
        
        if (message.Contains("section") || message.Contains("law"))
        {
            laws.Add("Section 66C IT Act");
            laws.Add("Section 66D IT Act");
            return "Common IT Act sections for cybercrime:\n\n• **Section 43** - Damage to computer systems (Civil)\n• **Section 66** - Computer related offences (Criminal)\n• **Section 66C** - Identity theft\n• **Section 66D** - Cheating by personation\n• **Section 66E** - Privacy violation\n• **Section 67** - Obscene content\n\nWhich section would you like detailed analysis on?";
        }

        return "I can assist with:\n• Relevant case precedents\n• Section-wise analysis\n• Case strategy suggestions\n• Evidence requirements\n• Procedural guidance\n• Success probability assessment\n\nWhat aspect of your case would you like to discuss?";
    }

    private string GeneratePoliceResponse(string message, ref List<string> laws, ref List<string> cases)
    {
        message = message.ToLower();
        
        if (message.Contains("fir") || message.Contains("register"))
        {
            laws.Add("IT Act 2000");
            return "For registering cybercrime FIR:\n\n**Essential Information:**\n1. Complainant details with contact info\n2. Accused details (if known)\n3. Incident date, time, and description\n4. Nature of cybercrime (category)\n5. Applicable sections\n6. Digital evidence details\n\n**Common Sections:**\n• Section 66C/66D for fraud\n• Section 67 for obscene content\n• Section 354D IPC for stalking\n\nWould you like help with section determination?";
        }
        
        if (message.Contains("investigation") || message.Contains("evidence"))
        {
            laws.Add("IT Act 2000 Section 43");
            return "Digital Evidence Collection:\n\n1. **Preserve Original Evidence**\n   - Take forensic images\n   - Maintain chain of custody\n\n2. **Required Documentation**\n   - Screenshots with timestamps\n   - Server logs\n   - IP address records\n   - Email headers\n\n3. **Legal Requirements**\n   - Section 65B certificate for admissibility\n   - Proper seizure procedures\n   - Forensic expert involvement\n\nNeed specific guidance on any aspect?";
        }

        return "Investigation Support Available:\n• FIR registration guidance\n• Section determination\n• Evidence collection procedures\n• Forensic analysis requirements\n• Case documentation\n• Procedural compliance\n\nHow can I assist with your investigation?";
    }

    public Task<List<ChatMessage>> GetSessionMessagesAsync(int sessionId)
    {
        var session = _sessions.FirstOrDefault(s => s.Id == sessionId);
        return Task.FromResult(session?.Messages ?? new List<ChatMessage>());
    }

    public Task<List<ChatSession>> GetUserSessionsAsync()
    {
        var user = _authService.GetCurrentUser();
        if (user == null) return Task.FromResult(new List<ChatSession>());

        var userSessions = _sessions.Where(s => s.UserId == user.Id).ToList();
        return Task.FromResult(userSessions);
    }
}

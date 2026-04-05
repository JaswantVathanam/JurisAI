using AILegalAsst.Models;
using System.Text.Json;

namespace AILegalAsst.Services;

/// <summary>
/// Service for AI Agent-driven case filing - Uses Azure AI Agent (Case-Project) to handle conversations
/// </summary>
public class AgentCaseFilingService
{
    private readonly CaseService _caseService;
    private readonly AzureAgentService _agentService;
    private readonly IdentityVerificationService _identityService;
    private readonly SessionSecurityService _sessionSecurity;
    private readonly ILogger<AgentCaseFilingService> _logger;

    // Filing session state
    private Dictionary<string, FilingSession> _activeSessions = new();

    // System prompt for the Azure Agent to act as a case filing assistant
    private const string CaseFilingSystemPrompt = @"
You are an AI Legal Assistant helping citizens file complaints in India. Your role is to:
1. Gather all necessary information to file a legal complaint
2. Be empathetic and professional
3. Ask one question at a time
4. Guide users through the process step by step

Information you need to collect:
- Type of incident (Cybercrime, Criminal, Civil, Other)
- For cybercrime: specific category (fraud, hacking, stalking, etc.)
- Title/brief summary of the complaint
- Detailed description of what happened (when, where, how)
- Information about the accused (if known)
- Evidence available (screenshots, documents, witnesses)
- Financial loss (if any)

After collecting all information, summarize the complaint and ask for confirmation before filing.
When ready to file, respond with: [CASE_READY_TO_FILE] followed by a JSON summary.

Current conversation context:
";

    public AgentCaseFilingService(
        CaseService caseService, 
        AzureAgentService agentService,
        IdentityVerificationService identityService,
        SessionSecurityService sessionSecurity,
        ILogger<AgentCaseFilingService> logger)
    {
        _caseService = caseService;
        _agentService = agentService;
        _identityService = identityService;
        _sessionSecurity = sessionSecurity;
        _logger = logger;
    }

    /// <summary>
    /// Start a new case filing session with Azure AI Agent.
    /// Binds the session to the authenticated user's identity and device fingerprint.
    /// </summary>
    public FilingSession StartFilingSession(string userId, string userName, string userEmail, 
        int authenticatedUserId = 0, UserRole userRole = UserRole.Citizen, string? deviceFingerprint = null)
    {
        var session = new FilingSession
        {
            SessionId = Guid.NewGuid().ToString(),
            UserId = userId,
            UserName = userName,
            UserEmail = userEmail,
            CurrentStep = FilingStep.Welcome,
            StartedAt = DateTime.UtcNow,
            CaseData = new CaseDto(),
            ConversationHistory = new List<string>(),
            // Identity binding
            AuthenticatedUserId = authenticatedUserId,
            AuthenticatedUserRole = userRole,
            DeviceFingerprint = deviceFingerprint ?? "unknown",
            IdentityVerifiedForSubmission = false
        };

        // Bind session security token
        var securitySession = _sessionSecurity.GetSession(authenticatedUserId);
        if (securitySession != null)
        {
            session.SecuritySessionToken = securitySession.SessionToken;
        }

        _activeSessions[session.SessionId] = session;
        _logger.LogInformation("Filing session started: {SessionId} for user {UserName} ({Email}), UserId: {UserId}",
            session.SessionId, userName, userEmail, authenticatedUserId);
        
        return session;
    }

    /// <summary>
    /// Get the current session
    /// </summary>
    public FilingSession? GetSession(string sessionId)
    {
        return _activeSessions.TryGetValue(sessionId, out var session) ? session : null;
    }

    /// <summary>
    /// Process user response through Azure AI Agent
    /// </summary>
    public async Task<AgentFilingResponse> ProcessUserResponseAsync(string sessionId, string userInput)
    {
        if (!_activeSessions.TryGetValue(sessionId, out var session))
        {
            return new AgentFilingResponse
            {
                Success = false,
                Message = "Session not found. Please start a new complaint filing.",
                IsComplete = false
            };
        }

        try
        {
            // Handle initial welcome
            if (session.CurrentStep == FilingStep.Welcome && string.IsNullOrEmpty(userInput))
            {
                session.CurrentStep = FilingStep.IncidentType;
                
                // Send initial prompt to Azure Agent
                var initialPrompt = $@"A citizen named {session.UserName} wants to file a complaint. 
Start by greeting them warmly and asking what type of incident they want to report.
Offer these options: 1) Cybercrime 2) Criminal 3) Civil 4) Other";

                var agentResponse = await _agentService.SendMessageAsync(initialPrompt, 
                    $"User: {session.UserName}, Email: {session.UserEmail}");

                if (agentResponse.Success)
                {
                    session.ConversationHistory.Add($"Assistant: {agentResponse.Message}");
                    return new AgentFilingResponse
                    {
                        Success = true,
                        Message = agentResponse.Message,
                        CurrentStep = FilingStep.IncidentType,
                        Options = new[] { "Cybercrime", "Criminal", "Civil", "Other" }
                    };
                }
                else
                {
                    // Fallback to local response
                    return GetFallbackWelcome(session);
                }
            }

            // Add user input to history
            session.ConversationHistory.Add($"User: {userInput}");

            // Build context for the agent
            var context = BuildConversationContext(session);
            
            // Check for confirmation/submission keywords
            if (IsConfirmationResponse(userInput) && session.CurrentStep == FilingStep.Confirmation)
            {
                return await SubmitComplaintAsync(session);
            }

            // Send to Azure Agent for processing
            var prompt = $@"{CaseFilingSystemPrompt}

Complainant: {session.UserName} ({session.UserEmail})
Conversation so far:
{string.Join("\n", session.ConversationHistory.TakeLast(10))}

User's latest message: {userInput}

Respond naturally, ask the next relevant question, or if you have all the information needed, 
provide a summary and ask for confirmation. If confirming, include [CASE_READY_TO_FILE] marker.";

            var response = await _agentService.SendMessageAsync(prompt);

            if (response.Success)
            {
                session.ConversationHistory.Add($"Assistant: {response.Message}");

                // Check if agent signals case is ready to file
                if (response.Message.Contains("[CASE_READY_TO_FILE]"))
                {
                    session.CurrentStep = FilingStep.Confirmation;
                    var cleanMessage = response.Message.Replace("[CASE_READY_TO_FILE]", "").Trim();
                    
                    // Try to parse case data from agent response
                    ExtractCaseDataFromResponse(session, response.Message);

                    return new AgentFilingResponse
                    {
                        Success = true,
                        Message = cleanMessage + "\n\n**Please type 'Yes' to confirm and submit, or 'Edit' to make changes.**",
                        CurrentStep = FilingStep.Confirmation,
                        Options = new[] { "Yes, Submit", "Edit", "Cancel" }
                    };
                }

                // Update step based on conversation progress
                UpdateFilingStep(session, userInput, response.Message);

                return new AgentFilingResponse
                {
                    Success = true,
                    Message = response.Message,
                    CurrentStep = session.CurrentStep
                };
            }
            else
            {
                // Fallback to rule-based processing
                return await ProcessStepLocallyAsync(session, userInput);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing filing step with Azure Agent");
            return await ProcessStepLocallyAsync(session, userInput);
        }
    }

    private string BuildConversationContext(FilingSession session)
    {
        var context = new System.Text.StringBuilder();
        context.AppendLine($"Complainant: {session.UserName}");
        context.AppendLine($"Session started: {session.StartedAt}");
        
        if (!string.IsNullOrEmpty(session.CaseData.Type))
            context.AppendLine($"Case Type: {session.CaseData.Type}");
        if (!string.IsNullOrEmpty(session.CaseData.Title))
            context.AppendLine($"Title: {session.CaseData.Title}");
        if (!string.IsNullOrEmpty(session.CaseData.Description))
            context.AppendLine($"Description: {session.CaseData.Description}");
            
        return context.ToString();
    }

    private void UpdateFilingStep(FilingSession session, string userInput, string agentResponse)
    {
        var lowerInput = userInput.ToLower();
        var lowerResponse = agentResponse.ToLower();

        // Detect case type from user input
        if (session.CurrentStep == FilingStep.IncidentType)
        {
            if (lowerInput.Contains("cyber") || lowerInput == "1")
            {
                session.CaseData.Type = "Cybercrime";
                session.CaseData.IsCybercrime = true;
                session.CurrentStep = FilingStep.CybercrimeCategory;
            }
            else if (lowerInput.Contains("criminal") || lowerInput == "2")
            {
                session.CaseData.Type = "Criminal";
                session.CurrentStep = FilingStep.IncidentTitle;
            }
            else if (lowerInput.Contains("civil") || lowerInput == "3")
            {
                session.CaseData.Type = "Civil";
                session.CurrentStep = FilingStep.IncidentTitle;
            }
            else if (lowerInput.Contains("other") || lowerInput == "4")
            {
                session.CaseData.Type = "Other";
                session.CurrentStep = FilingStep.IncidentTitle;
            }
        }
        else if (session.CurrentStep == FilingStep.CybercrimeCategory)
        {
            session.CaseData.CybercrimeCategory = ExtractCybercrimeCategory(userInput);
            session.CurrentStep = FilingStep.IncidentTitle;
        }
        else if (session.CurrentStep == FilingStep.IncidentTitle && string.IsNullOrEmpty(session.CaseData.Title))
        {
            session.CaseData.Title = userInput.Length > 100 ? userInput.Substring(0, 100) : userInput;
            session.CurrentStep = FilingStep.IncidentDescription;
        }
        else if (session.CurrentStep == FilingStep.IncidentDescription && string.IsNullOrEmpty(session.CaseData.Description))
        {
            session.CaseData.Description = userInput;
            session.CurrentStep = FilingStep.AccusedInfo;
        }
        else if (session.CurrentStep == FilingStep.AccusedInfo && string.IsNullOrEmpty(session.CaseData.Accused))
        {
            session.CaseData.Accused = userInput;
            session.CurrentStep = FilingStep.EvidenceInfo;
        }
        else if (session.CurrentStep == FilingStep.EvidenceInfo)
        {
            session.CaseData.DigitalEvidence = userInput;
            session.CaseData.DigitalEvidenceCollected = !lowerInput.Contains("no") && !lowerInput.Contains("none");
            session.CurrentStep = FilingStep.FinancialLoss;
        }
        else if (session.CurrentStep == FilingStep.FinancialLoss)
        {
            if (!lowerInput.Contains("no") && !lowerInput.Contains("none"))
            {
                session.CaseData.Description += $"\n\nFinancial Loss: {userInput}";
            }
            session.CurrentStep = FilingStep.Confirmation;
        }
    }

    private string ExtractCybercrimeCategory(string input)
    {
        input = input.ToLower();
        if (input.Contains("fraud") || input.Contains("scam")) return "Online Fraud";
        if (input.Contains("hack")) return "Hacking";
        if (input.Contains("identity")) return "Identity Theft";
        if (input.Contains("stalk") || input.Contains("harass")) return "Cyberstalking";
        if (input.Contains("breach") || input.Contains("data")) return "Data Breach";
        if (input.Contains("phish")) return "Phishing";
        if (input.Contains("social")) return "Social Media Crime";
        return "Other Cybercrime";
    }

    private void ExtractCaseDataFromResponse(FilingSession session, string response)
    {
        // Try to find JSON in the response
        try
        {
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}');
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var json = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
                var extractedData = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (extractedData != null)
                {
                    if (extractedData.TryGetValue("title", out var title))
                        session.CaseData.Title = title;
                    if (extractedData.TryGetValue("description", out var desc))
                        session.CaseData.Description = desc;
                    if (extractedData.TryGetValue("type", out var type))
                        session.CaseData.Type = type;
                    if (extractedData.TryGetValue("accused", out var accused))
                        session.CaseData.Accused = accused;
                }
            }
        }
        catch
        {
            // Ignore JSON parsing errors, use data collected from conversation
        }
    }

    private bool IsConfirmationResponse(string input)
    {
        var lower = input.ToLower().Trim();
        return lower == "yes" || lower == "confirm" || lower == "submit" || 
               lower == "yes, submit" || lower == "1" || lower.Contains("yes");
    }

    private AgentFilingResponse GetFallbackWelcome(FilingSession session)
    {
        return new AgentFilingResponse
        {
            Success = true,
            Message = $"Hello {session.UserName}! I'm your AI Legal Assistant powered by Azure AI. I'll help you file a complaint step by step.\n\n" +
                      "**What type of incident are you reporting?**\n\n" +
                      "1️⃣ **Cybercrime** - Online fraud, hacking, cyberstalking, etc.\n" +
                      "2️⃣ **Criminal** - Theft, assault, threat, etc.\n" +
                      "3️⃣ **Civil** - Property dispute, contract issues, etc.\n" +
                      "4️⃣ **Other** - Any other legal matter\n\n" +
                      "Please type the number or name of the incident type.",
            CurrentStep = FilingStep.IncidentType,
            Options = new[] { "Cybercrime", "Criminal", "Civil", "Other" }
        };
    }

    /// <summary>
    /// Fallback: Process step locally when Azure Agent is unavailable
    /// </summary>
    private async Task<AgentFilingResponse> ProcessStepLocallyAsync(FilingSession session, string userInput)
    {
        _logger.LogInformation("Using local fallback for case filing step: {Step}", session.CurrentStep);
        
        switch (session.CurrentStep)
        {
            case FilingStep.IncidentType:
                var caseType = ParseCaseType(userInput);
                session.CaseData.Type = caseType.ToString();
                session.CaseData.IsCybercrime = caseType == CaseType.Cybercrime;
                session.CurrentStep = session.CaseData.IsCybercrime ? FilingStep.CybercrimeCategory : FilingStep.IncidentTitle;
                
                if (session.CaseData.IsCybercrime)
                {
                    return new AgentFilingResponse
                    {
                        Success = true,
                        Message = "I understand you're reporting a **cybercrime**.\n\n" +
                                  "**What category of cybercrime is this?**\n\n" +
                                  "1️⃣ Online Fraud / Financial Scam\n" +
                                  "2️⃣ Hacking / Unauthorized Access\n" +
                                  "3️⃣ Identity Theft\n" +
                                  "4️⃣ Cyberstalking / Online Harassment\n" +
                                  "5️⃣ Data Breach\n" +
                                  "6️⃣ Other Cybercrime",
                        CurrentStep = FilingStep.CybercrimeCategory,
                        Options = new[] { "Online Fraud", "Hacking", "Identity Theft", "Cyberstalking", "Data Breach", "Other" }
                    };
                }
                return new AgentFilingResponse
                {
                    Success = true,
                    Message = $"You're filing a **{caseType}** complaint.\n\n**Please provide a brief title for your complaint.**",
                    CurrentStep = FilingStep.IncidentTitle
                };

            case FilingStep.CybercrimeCategory:
                session.CaseData.CybercrimeCategory = ExtractCybercrimeCategory(userInput);
                session.CurrentStep = FilingStep.IncidentTitle;
                return new AgentFilingResponse
                {
                    Success = true,
                    Message = $"Category: **{session.CaseData.CybercrimeCategory}**\n\n**Please provide a brief title for your complaint.**",
                    CurrentStep = FilingStep.IncidentTitle
                };

            case FilingStep.IncidentTitle:
                session.CaseData.Title = userInput.Trim();
                session.CurrentStep = FilingStep.IncidentDescription;
                return new AgentFilingResponse
                {
                    Success = true,
                    Message = "**Now, please describe the incident in detail.**\n\nInclude when, where, what happened, and how it affected you.",
                    CurrentStep = FilingStep.IncidentDescription
                };

            case FilingStep.IncidentDescription:
                session.CaseData.Description = userInput.Trim();
                session.CurrentStep = FilingStep.AccusedInfo;
                return new AgentFilingResponse
                {
                    Success = true,
                    Message = "**Do you know who is responsible?**\n\nProvide name, identifying info, or type 'Unknown'.",
                    CurrentStep = FilingStep.AccusedInfo
                };

            case FilingStep.AccusedInfo:
                session.CaseData.Accused = userInput.ToLower().Contains("unknown") ? "Unknown" : userInput.Trim();
                session.CurrentStep = FilingStep.EvidenceInfo;
                return new AgentFilingResponse
                {
                    Success = true,
                    Message = "**Do you have any evidence?**\n\nDescribe screenshots, documents, witnesses, or type 'None'.",
                    CurrentStep = FilingStep.EvidenceInfo
                };

            case FilingStep.EvidenceInfo:
                session.CaseData.DigitalEvidence = userInput.Trim();
                session.CaseData.DigitalEvidenceCollected = !userInput.ToLower().Contains("none");
                session.CurrentStep = FilingStep.FinancialLoss;
                return new AgentFilingResponse
                {
                    Success = true,
                    Message = "**Did you suffer any financial loss?**\n\nMention the amount or type 'No'.",
                    CurrentStep = FilingStep.FinancialLoss
                };

            case FilingStep.FinancialLoss:
                if (!userInput.ToLower().Contains("no") && !userInput.ToLower().Contains("none"))
                {
                    session.CaseData.Description += $"\n\nFinancial Loss: {userInput}";
                }
                session.CurrentStep = FilingStep.Confirmation;
                return await GenerateConfirmationAsync(session);

            case FilingStep.Confirmation:
                if (userInput.ToLower().Contains("yes") || userInput.ToLower().Contains("confirm") || userInput == "1")
                {
                    return await SubmitComplaintAsync(session);
                }
                return new AgentFilingResponse
                {
                    Success = true,
                    Message = "Please type **'Yes'** to submit or **'Edit'** to make changes.",
                    CurrentStep = FilingStep.Confirmation
                };

            default:
                return GetFallbackWelcome(session);
        }
    }

    private async Task<AgentFilingResponse> GenerateConfirmationAsync(FilingSession session)
    {
        // Try to get AI-suggested laws
        var suggestedLaws = await SuggestApplicableLawsAsync(session.CaseData);
        session.CaseData.ApplicableLaws = suggestedLaws.Laws;
        session.CaseData.Sections = suggestedLaws.Sections;

        var summary = $"## 📋 Complaint Summary\n\n" +
                      $"**Complainant:** {session.UserName}\n" +
                      $"**Type:** {session.CaseData.Type}" + (session.CaseData.IsCybercrime ? $" ({session.CaseData.CybercrimeCategory})" : "") + "\n" +
                      $"**Title:** {session.CaseData.Title}\n\n" +
                      $"**Description:**\n{session.CaseData.Description}\n\n" +
                      $"**Accused:** {session.CaseData.Accused}\n" +
                      $"**Evidence:** {(session.CaseData.DigitalEvidenceCollected ? session.CaseData.DigitalEvidence : "None provided")}\n\n" +
                      $"**Suggested Applicable Laws:**\n";
        
        foreach (var law in suggestedLaws.Laws)
        {
            summary += $"• {law}\n";
        }

        summary += "\n---\n\n" +
                   "**Please confirm to submit:**\n" +
                   "1️⃣ **Yes** - Submit this complaint\n" +
                   "2️⃣ **Edit** - Make changes\n" +
                   "3️⃣ **Cancel** - Discard";

        return new AgentFilingResponse
        {
            Success = true,
            Message = summary,
            CurrentStep = FilingStep.Confirmation,
            Options = new[] { "Yes, Submit", "Edit", "Cancel" },
            CaseSummary = session.CaseData
        };
    }

    private async Task<AgentFilingResponse> SubmitComplaintAsync(FilingSession session)
    {
        try
        {
            // SECURITY CHECK: Require identity verification before filing
            if (!session.IdentityVerifiedForSubmission)
            {
                _logger.LogWarning("Case filing blocked — identity not verified for session {SessionId}", session.SessionId);
                session.CurrentStep = FilingStep.Confirmation;
                return new AgentFilingResponse
                {
                    Success = false,
                    Message = "## 🔐 Identity Verification Required\n\n" +
                              "Before the AI can file this case on your behalf, you must verify your identity.\n\n" +
                              "**This ensures that:**\n" +
                              "- ✅ The case is filed by the authenticated account holder\n" +
                              "- ✅ No unauthorized person can misuse your session\n" +
                              "- ✅ A tamper-proof audit trail is created\n\n" +
                              "Please click **'Verify Identity'** to re-enter your password and authorize this filing.",
                    CurrentStep = FilingStep.Confirmation,
                    RequiresIdentityVerification = true
                };
            }

            // Ensure we have minimum required data
            if (string.IsNullOrEmpty(session.CaseData.Title))
                session.CaseData.Title = "Complaint filed via AI Assistant";
            if (string.IsNullOrEmpty(session.CaseData.Description))
                session.CaseData.Description = string.Join("\n", session.ConversationHistory.Where(h => h.StartsWith("User:")));
            if (string.IsNullOrEmpty(session.CaseData.Type))
                session.CaseData.Type = "Other";

            // Create the case
            var createdCase = await _caseService.CreateCaseFromAgentAsync(
                session.CaseData, 
                session.UserName, 
                session.UserEmail);

            // Log the AI action with full audit trail
            var user = new User 
            { 
                Id = session.AuthenticatedUserId, 
                Email = session.UserEmail, 
                Name = session.UserName, 
                Role = session.AuthenticatedUserRole 
            };
            
            _identityService.LogAction(
                user: user,
                actionType: AIActionType.CaseFiled,
                description: $"AI filed case '{session.CaseData.Title}' on behalf of {session.UserName}",
                sessionId: session.SessionId,
                deviceFingerprint: session.DeviceFingerprint,
                verificationMethod: session.VerificationMethod,
                succeeded: true,
                caseNumber: createdCase.CaseNumber,
                caseId: createdCase.Id
            );

            // Remove session
            _activeSessions.Remove(session.SessionId);

            return new AgentFilingResponse
            {
                Success = true,
                IsComplete = true,
                Message = $"## ✅ Complaint Filed Successfully!\n\n" +
                          $"**Case Number:** {createdCase.CaseNumber}\n" +
                          $"**Filed On:** {createdCase.FiledDate:MMMM dd, yyyy 'at' hh:mm tt}\n" +
                          $"**Filed By:** {session.UserName} (Identity Verified ✔️)\n" +
                          $"**Verification Method:** {session.VerificationMethod}\n" +
                          $"**Audit Hash:** `{_identityService.GetUserActionLogs(session.AuthenticatedUserId).FirstOrDefault()?.ActionHash?[..16]}...`\n\n" +
                          $"### What happens next?\n\n" +
                          $"1. 📋 Your complaint has been registered in the system\n" +
                          $"2. 👮 It will be assigned to a police station for FIR registration\n" +
                          $"3. 🔍 An investigating officer will review your case\n" +
                          $"4. 📱 You can track the status in the **Cases** section\n\n" +
                          $"**Need help with anything else?** Just ask!",
                CreatedCaseNumber = createdCase.CaseNumber,
                CreatedCaseId = createdCase.Id,
                CurrentStep = FilingStep.Complete
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting complaint");

            // Log failed action
            var user = new User 
            { 
                Id = session.AuthenticatedUserId, 
                Email = session.UserEmail, 
                Name = session.UserName, 
                Role = session.AuthenticatedUserRole 
            };
            _identityService.LogAction(
                user: user,
                actionType: AIActionType.CaseFiled,
                description: $"FAILED: AI case filing for '{session.CaseData.Title}'",
                sessionId: session.SessionId,
                deviceFingerprint: session.DeviceFingerprint,
                verificationMethod: session.VerificationMethod,
                succeeded: false,
                failureReason: ex.Message
            );

            return new AgentFilingResponse
            {
                Success = false,
                Message = "There was an error submitting your complaint. Please try again.",
                CurrentStep = FilingStep.Confirmation
            };
        }
    }

    /// <summary>
    /// Request identity verification before AI files the case.
    /// Called when user reaches the submission step.
    /// </summary>
    public VerificationChallenge? RequestIdentityVerification(string sessionId)
    {
        if (!_activeSessions.TryGetValue(sessionId, out var session))
            return null;

        var user = new User
        {
            Id = session.AuthenticatedUserId,
            Email = session.UserEmail,
            Name = session.UserName,
            Role = session.AuthenticatedUserRole
        };

        var challenge = _identityService.RequestVerification(
            user, 
            AIActionType.CaseFiled, 
            $"File case: {session.CaseData.Title ?? "New complaint"}");

        session.VerificationToken = challenge.VerificationToken;
        return challenge;
    }

    /// <summary>
    /// Confirm identity with the selected verification method. If verified, marks the session as authorized for filing.
    /// </summary>
    public async Task<VerificationResult> ConfirmIdentityAsync(string sessionId, string credential, IdentityVerificationMethod method = IdentityVerificationMethod.PasswordReEntry)
    {
        if (!_activeSessions.TryGetValue(sessionId, out var session))
        {
            return new VerificationResult
            {
                IsVerified = false,
                Reason = "Filing session not found."
            };
        }

        if (string.IsNullOrEmpty(session.VerificationToken))
        {
            return new VerificationResult
            {
                IsVerified = false,
                Reason = "No verification was requested. Please try submitting again."
            };
        }

        var result = await _identityService.VerifyIdentityAsync(
            session.AuthenticatedUserId,
            session.VerificationToken,
            credential,
            method,
            session.DeviceFingerprint);

        if (result.IsVerified)
        {
            session.IdentityVerifiedForSubmission = true;
            session.IdentityVerifiedAt = DateTime.UtcNow;
            session.VerificationMethod = method;
            _logger.LogInformation("Identity confirmed for filing session {SessionId}, user {UserId}, method {Method}", 
                sessionId, session.AuthenticatedUserId, method);
        }

        return result;
    }

    private async Task<(List<string> Laws, List<string> Sections)> SuggestApplicableLawsAsync(CaseDto caseData)
    {
        // Try Azure Agent for law suggestions
        try
        {
            if (_agentService.IsReady)
            {
                var prompt = $@"For a {caseData.Type} case in India:
Title: {caseData.Title}
Description: {caseData.Description}
Category: {caseData.CybercrimeCategory ?? "General"}

List the top 3-5 applicable Indian laws and specific sections. Be concise.";
                
                var response = await _agentService.SendMessageAsync(prompt);
                if (response.Success)
                {
                    // Parse laws from response (simple extraction)
                    var laws = new List<string>();
                    var sections = new List<string>();
                    
                    if (response.Message.Contains("IT Act") || response.Message.Contains("Information Technology"))
                        laws.Add("Information Technology Act, 2000");
                    if (response.Message.Contains("IPC") || response.Message.Contains("Indian Penal Code"))
                        laws.Add("Indian Penal Code (IPC)");
                    if (response.Message.Contains("CrPC"))
                        laws.Add("Code of Criminal Procedure (CrPC)");

                    // Extract section numbers
                    var sectionMatches = System.Text.RegularExpressions.Regex.Matches(
                        response.Message, @"Section\s+\d+[A-Z]?(?:\s+\w+)?");
                    foreach (System.Text.RegularExpressions.Match match in sectionMatches)
                    {
                        sections.Add(match.Value);
                    }

                    if (laws.Count > 0)
                        return (laws, sections.Any() ? sections : new List<string> { "As per agent analysis" });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not get AI suggestions for laws");
        }

        // Fallback to rule-based
        return GetRuleBasedLawSuggestions(caseData);
    }

    private (List<string> Laws, List<string> Sections) GetRuleBasedLawSuggestions(CaseDto caseData)
    {
        var laws = new List<string>();
        var sections = new List<string>();

        if (caseData.IsCybercrime)
        {
            laws.Add("Information Technology Act, 2000");
            laws.Add("Indian Penal Code (IPC)");

            switch (caseData.CybercrimeCategory?.ToLower())
            {
                case "online fraud":
                    sections.AddRange(new[] { "Section 66C IT Act", "Section 66D IT Act", "Section 420 IPC" });
                    break;
                case "hacking":
                    sections.AddRange(new[] { "Section 43 IT Act", "Section 66 IT Act" });
                    break;
                case "cyberstalking":
                    sections.AddRange(new[] { "Section 67 IT Act", "Section 354D IPC" });
                    break;
                default:
                    sections.AddRange(new[] { "Section 66 IT Act", "Section 43 IT Act" });
                    break;
            }
        }
        else
        {
            laws.Add("Indian Penal Code (IPC)");
            sections.Add("To be determined based on investigation");
        }

        return (laws, sections);
    }

    private CaseType ParseCaseType(string input)
    {
        input = input.ToLower().Trim();
        if (input.Contains("cyber") || input == "1") return CaseType.Cybercrime;
        if (input.Contains("criminal") || input == "2") return CaseType.Criminal;
        if (input.Contains("civil") || input == "3") return CaseType.Civil;
        return CaseType.Other;
    }
}

/// <summary>
/// Filing session state - includes conversation history for Azure Agent
/// Enhanced with identity binding for AI action attribution
/// </summary>
public class FilingSession
{
    public string SessionId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public FilingStep CurrentStep { get; set; }
    public DateTime StartedAt { get; set; }
    public CaseDto CaseData { get; set; } = new();
    public List<string> ConversationHistory { get; set; } = new();

    // Identity & Security binding
    public string DeviceFingerprint { get; set; } = string.Empty;
    public string SecuritySessionToken { get; set; } = string.Empty;
    public int AuthenticatedUserId { get; set; }
    public UserRole AuthenticatedUserRole { get; set; }
    public bool IdentityVerifiedForSubmission { get; set; }
    public string? VerificationToken { get; set; }
    public DateTime? IdentityVerifiedAt { get; set; }
    public IdentityVerificationMethod VerificationMethod { get; set; }
}

/// <summary>
/// Filing workflow steps
/// </summary>
public enum FilingStep
{
    Welcome,
    IncidentType,
    CybercrimeCategory,
    IncidentTitle,
    IncidentDescription,
    AccusedInfo,
    EvidenceInfo,
    FinancialLoss,
    Confirmation,
    Complete
}

/// <summary>
/// Response from agent filing service
/// </summary>
public class AgentFilingResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public FilingStep CurrentStep { get; set; }
    public string[]? Options { get; set; }
    public bool IsComplete { get; set; }
    public string? CreatedCaseNumber { get; set; }
    public int? CreatedCaseId { get; set; }
    public CaseDto? CaseSummary { get; set; }
    public bool RequiresIdentityVerification { get; set; }
}

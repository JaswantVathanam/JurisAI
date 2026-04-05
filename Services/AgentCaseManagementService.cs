using AILegalAsst.Models;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AILegalAsst.Services;


public class AgentCaseManagementService
{
    private readonly CaseService _caseService;
    private readonly AzureAgentService _agentService;
    private readonly ILogger<AgentCaseManagementService> _logger;

    // Active command sessions
    private Dictionary<string, CommandSession> _activeSessions = new();

    /// <summary>
    /// Detects if the agent response is a content-safety refusal rather than a useful answer.
    /// </summary>
    private static bool IsAgentRefusal(string? message)
    {
        if (string.IsNullOrWhiteSpace(message)) return true;
        var lower = message.ToLowerInvariant();
        return lower.Contains("i'm sorry, but i cannot") ||
               lower.Contains("i cannot assist with that") ||
               lower.Contains("i can't assist with that") ||
               lower.Contains("i'm not able to help with") ||
               lower.Contains("i'm unable to") ||
               lower.Contains("as an ai, i cannot");
    }

    public AgentCaseManagementService(
        CaseService caseService,
        AzureAgentService agentService,
        ILogger<AgentCaseManagementService> logger)
    {
        _caseService = caseService;
        _agentService = agentService;
        _logger = logger;
    }


    public async Task<AgentCommandResponse> ProcessQueryAsync(string query, User user)
    {
        var intent = DetectIntent(query, user.Role);
        
        _logger.LogInformation("Processing query for {Role}: Intent={Intent}", user.Role, intent.Type);

        return intent.Type switch
        {
            IntentType.ListCases => await HandleListCasesAsync(query, user, intent),
            IntentType.CaseDetails => await HandleCaseDetailsAsync(query, user, intent),
            IntentType.CaseSummary => await HandleCaseSummaryAsync(query, user, intent),
            IntentType.TakeAction => await HandleTakeActionAsync(query, user, intent),
            IntentType.AnalyzeCase => await HandleAnalyzeCaseAsync(query, user, intent),
            IntentType.UpdateStatus => await HandleUpdateStatusAsync(query, user, intent),
            IntentType.AssignCase => await HandleAssignCaseAsync(query, user, intent),
            IntentType.GenerateDocument => await HandleGenerateDocumentAsync(query, user, intent),
            IntentType.LegalAdvice => await HandleLegalAdviceAsync(query, user, intent),
            IntentType.CaseProgress => await HandleCaseProgressAsync(query, user, intent),
            _ => await HandleGeneralQueryAsync(query, user)
        };
    }

    #region Intent Detection

    private DetectedIntent DetectIntent(string query, UserRole role)
    {
        var lowerQuery = query.ToLower();
        var intent = new DetectedIntent();

        // Extract case number if present
        var caseMatch = Regex.Match(query, @"(CYB|CRM|CVL|CON|GEN)/\d{4}/\d+", RegexOptions.IgnoreCase);
        if (caseMatch.Success)
        {
            intent.CaseNumber = caseMatch.Value.ToUpper();
        }

        // Extract case ID if mentioned
        var idMatch = Regex.Match(query, @"case\s*(?:id|#)?\s*(\d+)", RegexOptions.IgnoreCase);
        if (idMatch.Success && int.TryParse(idMatch.Groups[1].Value, out var caseId))
        {
            intent.CaseId = caseId;
        }

        // Detect intent based on keywords and role
        if (ContainsAny(lowerQuery, "list", "show", "display", "my cases", "all cases", "pending cases", "assigned", "filed cases"))
        {
            intent.Type = IntentType.ListCases;
            
            // Detect filter criteria
            if (lowerQuery.Contains("pending")) intent.StatusFilter = "Filed";
            if (lowerQuery.Contains("investigation") || lowerQuery.Contains("investigating")) intent.StatusFilter = "UnderInvestigation";
            if (lowerQuery.Contains("filed")) intent.StatusFilter = "Filed";
            if (lowerQuery.Contains("closed")) intent.StatusFilter = "Closed";
            if (lowerQuery.Contains("dismissed")) intent.StatusFilter = "Dismissed";
            if (lowerQuery.Contains("trial")) intent.StatusFilter = "TrialInProgress";
            if (lowerQuery.Contains("hearing")) intent.StatusFilter = "InProgress";
            if (lowerQuery.Contains("cyber")) intent.TypeFilter = "Cybercrime";
        }
        else if (ContainsAny(lowerQuery, "categorize", "category", "group", "by type", "summary", "overview", "dashboard"))
        {
            intent.Type = IntentType.CaseSummary;
        }
        else if (ContainsAny(lowerQuery, "timeline", "history", "what happened", "updates", "workflow"))
        {
            intent.Type = IntentType.CaseProgress;
        }
        else if (ContainsAny(lowerQuery, "details", "show case", "view case", "about case", "tell me about", "case details"))
        {
            intent.Type = IntentType.CaseDetails;
        }
        else if (ContainsAny(lowerQuery, "analyze", "analyse", "review", "assess", "evaluate", "strength"))
        {
            intent.Type = IntentType.AnalyzeCase;
        }
        else if (role == UserRole.Police && ContainsAny(lowerQuery, "register fir", "file fir", "create fir", "start investigation", "assign officer", "collect evidence", "file chargesheet"))
        {
            intent.Type = IntentType.TakeAction;
            
            if (ContainsAny(lowerQuery, "fir")) intent.Action = "RegisterFIR";
            else if (ContainsAny(lowerQuery, "investigation")) intent.Action = "StartInvestigation";
            else if (ContainsAny(lowerQuery, "evidence")) intent.Action = "CollectEvidence";
            else if (ContainsAny(lowerQuery, "chargesheet")) intent.Action = "FileChargesheet";
            else if (ContainsAny(lowerQuery, "assign")) intent.Action = "AssignOfficer";
        }
        else if (role == UserRole.Lawyer && ContainsAny(lowerQuery, "take case", "accept case", "prepare", "file motion", "court submission", "defense", "argument"))
        {
            intent.Type = IntentType.TakeAction;
            
            if (ContainsAny(lowerQuery, "take", "accept")) intent.Action = "AcceptCase";
            else if (ContainsAny(lowerQuery, "prepare", "preparation")) intent.Action = "PrepareCase";
            else if (ContainsAny(lowerQuery, "defense", "argument")) intent.Action = "PrepareDefense";
            else if (ContainsAny(lowerQuery, "motion", "file")) intent.Action = "FileMotion";
        }
        else if (ContainsAny(lowerQuery, "update status", "change status", "mark as", "set status"))
        {
            intent.Type = IntentType.UpdateStatus;
        }
        else if (ContainsAny(lowerQuery, "assign", "allocate", "transfer"))
        {
            intent.Type = IntentType.AssignCase;
        }
        else if (ContainsAny(lowerQuery, "generate", "create document", "draft", "prepare report"))
        {
            intent.Type = IntentType.GenerateDocument;
        }
        else if (ContainsAny(lowerQuery, "progress", "status", "track", "what's happening", "update on", "where is"))
        {
            intent.Type = IntentType.CaseProgress;
        }
        else if (ContainsAny(lowerQuery, "legal", "law", "rights", "section", "act", "advice"))
        {
            intent.Type = IntentType.LegalAdvice;
        }
        else
        {
            intent.Type = IntentType.General;
        }

        return intent;
    }

    private bool ContainsAny(string text, params string[] keywords)
    {
        return keywords.Any(k => text.Contains(k));
    }

    #endregion

    #region Police Actions

    private async Task<AgentCommandResponse> HandleListCasesAsync(string query, User user, DetectedIntent intent)
    {
        List<Case> cases;

        if (user.Role == UserRole.Citizen)
        {
            cases = await _caseService.GetCasesByUserAsync(user.Email);
        }
        else if (user.Role == UserRole.Police)
        {
            cases = await _caseService.GetAllCasesAsync();
            // Filter for cases needing police action
            if (string.IsNullOrEmpty(intent.StatusFilter))
            {
                cases = cases.Where(c => 
                    c.Status == CaseStatus.Filed || 
                    c.Status == CaseStatus.UnderInvestigation ||
                    c.InvestigatingOfficer == user.Name
                ).ToList();
            }
        }
        else if (user.Role == UserRole.Lawyer)
        {
            cases = await _caseService.GetAllCasesAsync();
            // Filter for cases available for lawyers or assigned to this lawyer
            cases = cases.Where(c => 
                c.Status == CaseStatus.ChargesheetFiled ||
                c.Status == CaseStatus.InProgress ||
                c.Status == CaseStatus.TrialInProgress ||
                c.AssignedLawyer == user.Name
            ).ToList();
        }
        else
        {
            cases = await _caseService.GetAllCasesAsync();
        }

        // Apply additional filters
        if (!string.IsNullOrEmpty(intent.StatusFilter))
        {
            if (Enum.TryParse<CaseStatus>(intent.StatusFilter, out var status))
            {
                cases = cases.Where(c => c.Status == status).ToList();
            }
        }

        if (!string.IsNullOrEmpty(intent.TypeFilter))
        {
            if (intent.TypeFilter.Equals("Cybercrime", StringComparison.OrdinalIgnoreCase))
            {
                cases = cases.Where(c => c.IsCybercrime).ToList();
            }
        }

        // Build case data context for Azure Agent
        var caseDataJson = BuildCaseDataContext(cases, user.Role);
        
        // Generate response using Azure Agent
        var agentPrompt = BuildListCasesPrompt(query, user, cases, intent);
        var agentResponse = await _agentService.SendMessageAsync(agentPrompt, caseDataJson);

        string responseMessage;
        if (agentResponse.Success && !agentResponse.IsFallback && !IsAgentRefusal(agentResponse.Message))
        {
            responseMessage = agentResponse.Message;
        }
        else
        {
            // Fallback to structured format if agent unavailable or refused
            responseMessage = FormatCaseListForRole(cases, user.Role);
        }
        
        return new AgentCommandResponse
        {
            Success = true,
            Message = responseMessage,
            Cases = cases,
            Actions = GetAvailableActionsForRole(user.Role, cases)
        };
    }

    /// <summary>
    /// Provides a categorized summary/dashboard view of user's cases
    /// </summary>
    private async Task<AgentCommandResponse> HandleCaseSummaryAsync(string query, User user, DetectedIntent intent)
    {
        List<Case> cases;

        if (user.Role == UserRole.Citizen)
        {
            cases = await _caseService.GetCasesByUserAsync(user.Email);
        }
        else
        {
            cases = await _caseService.GetAllCasesAsync();
        }

        if (!cases.Any())
        {
            // Even for empty cases, let the agent provide a helpful response
            var emptyPrompt = $@"You are an AI assistant helping {user.Name} ({user.Role}).
They asked to see their case dashboard/summary, but they have no cases filed yet.
Provide a friendly, helpful response explaining:
1. They have no cases yet
2. How they can file a complaint
3. What the system can help them with

User Query: ""{query}""";

            var emptyResponse = await _agentService.SendMessageAsync(emptyPrompt);
            
            return new AgentCommandResponse
            {
                Success = true,
                Message = (emptyResponse.Success && !IsAgentRefusal(emptyResponse.Message)) ? emptyResponse.Message : 
                    "📋 **No Cases Found**\n\nYou haven't filed any complaints yet.\n\nSay **'file a complaint'** to get started!",
                Actions = new List<string> { "File a complaint", "Learn about my rights" }
            };
        }

        // Build context and prompt for Azure Agent
        var caseDataJson = BuildCaseDataContext(cases, user.Role);
        var summaryPrompt = BuildCaseSummaryPrompt(query, user, cases);
        
        var agentResponse = await _agentService.SendMessageAsync(summaryPrompt, caseDataJson);

        string responseMessage;
        if (agentResponse.Success && !agentResponse.IsFallback && !IsAgentRefusal(agentResponse.Message))
        {
            responseMessage = agentResponse.Message;
        }
        else
        {
            // Fallback to locally generated summary
            responseMessage = GenerateFallbackCaseSummary(cases, user);
        }

        return new AgentCommandResponse
        {
            Success = true,
            Message = responseMessage,
            Cases = cases,
            Actions = new List<string>
            {
                "Show my cases",
                "Show cybercrime cases",
                "Track case progress",
                "File new complaint"
            }
        };
    }

    /// <summary>
    /// Fallback case summary when Azure Agent is unavailable
    /// </summary>
    private string GenerateFallbackCaseSummary(List<Case> cases, User user)
    {
        var summary = new System.Text.StringBuilder();
        summary.AppendLine("## 📊 Your Case Dashboard\n");
        summary.AppendLine($"**Total Cases:** {cases.Count}\n");

        // By Status
        summary.AppendLine("### 📈 By Status\n");
        var statusGroups = cases.GroupBy(c => c.Status).OrderBy(g => g.Key);
        foreach (var group in statusGroups)
        {
            var statusIcon = GetStatusIcon(group.Key);
            summary.AppendLine($"{statusIcon} **{group.Key}:** {group.Count()} case(s)");
        }

        // By Type
        summary.AppendLine("\n### 📁 By Type\n");
        var cybercrimeCount = cases.Count(c => c.IsCybercrime);
        var otherCount = cases.Count - cybercrimeCount;
        if (cybercrimeCount > 0) summary.AppendLine($"🔒 **Cybercrime:** {cybercrimeCount} case(s)");
        if (otherCount > 0) summary.AppendLine($"📄 **Other:** {otherCount} case(s)");

        // Recent Activity
        summary.AppendLine("\n### 🕐 Recent Cases\n");
        var recentCases = cases.OrderByDescending(c => c.FiledDate).Take(3);
        foreach (var c in recentCases)
        {
            var daysAgo = (DateTime.Now - c.FiledDate).Days;
            var timeText = daysAgo == 0 ? "Today" : daysAgo == 1 ? "Yesterday" : $"{daysAgo} days ago";
            summary.AppendLine($"• **{c.CaseNumber}** - {c.Title} ({timeText})");
        }

        summary.AppendLine("\n---");
        summary.AppendLine("*AI analysis unavailable. Showing basic summary.*");

        return summary.ToString();
    }

    private async Task<AgentCommandResponse> HandleTakeActionAsync(string query, User user, DetectedIntent intent)
    {
        if (string.IsNullOrEmpty(intent.CaseNumber) && !intent.CaseId.HasValue)
        {
            return new AgentCommandResponse
            {
                Success = false,
                Message = GetActionPromptForRole(user.Role, intent.Action),
                RequiresInput = true,
                InputPrompt = "Please specify the case number (e.g., CYB/2025/001)"
            };
        }

        var caseData = intent.CaseId.HasValue 
            ? await _caseService.GetCaseByIdAsync(intent.CaseId.Value)
            : (await _caseService.SearchCasesAsync(intent.CaseNumber ?? "")).FirstOrDefault();

        if (caseData == null)
        {
            return new AgentCommandResponse
            {
                Success = false,
                Message = "❌ Case not found. Please verify the case number and try again."
            };
        }

        // Handle action based on role
        if (user.Role == UserRole.Police)
        {
            return await HandlePoliceActionAsync(intent.Action ?? "", caseData, user, query);
        }
        else if (user.Role == UserRole.Lawyer)
        {
            return await HandleLawyerActionAsync(intent.Action ?? "", caseData, user, query);
        }

        return new AgentCommandResponse
        {
            Success = false,
            Message = "⚠️ You don't have permission to perform this action."
        };
    }

    private async Task<AgentCommandResponse> HandlePoliceActionAsync(string action, Case caseData, User user, string query)
    {
        var workflowStep = new CaseWorkflowStep
        {
            Date = DateTime.UtcNow,
            Actor = user.Name,
            ActorRole = "Police"
        };

        switch (action)
        {
            case "RegisterFIR":
                if (caseData.Status != CaseStatus.Filed)
                {
                    return new AgentCommandResponse
                    {
                        Success = false,
                        Message = $"⚠️ Cannot register FIR. Case is already in '{caseData.Status}' status."
                    };
                }

                // Generate FIR number
                var firNumber = $"FIR/{DateTime.Now.Year}/{new Random().Next(1000, 9999)}";
                caseData.FIRNumber = firNumber;
                caseData.InvestigatingOfficer = user.Name;
                caseData.PoliceStation = user.PoliceStation ?? "Central Police Station";

                workflowStep.Stage = WorkflowStages.FIRRegistered;
                workflowStep.Status = "Completed";
                workflowStep.Notes = $"FIR registered: {firNumber}. Assigned to {user.Name}";

                await _caseService.UpdateCaseWorkflowAsync(caseData.Id, workflowStep);

                // Ask Azure Agent to generate the response
                var firPrompt = $@"You are an AI assistant helping a Police Officer with case management in India.

FIR has just been successfully registered. Generate a comprehensive response for the officer.

ACTION COMPLETED:
- FIR Number: {firNumber}
- Case Number: {caseData.CaseNumber}
- Case Title: {caseData.Title}
- Case Type: {caseData.Type}{(caseData.IsCybercrime ? " (Cybercrime)" : "")}
- Investigating Officer: {user.Name}
- Police Station: {caseData.PoliceStation}
- Description: {caseData.Description}
- Accused: {caseData.Accused}

Provide:
1. Confirmation of FIR registration
2. Investigation suggestions specific to this case type
3. Evidence to collect
4. Key next steps
5. Relevant IPC sections to consider

Format with proper markdown. Be professional and helpful.";

                var firResponse = await _agentService.SendMessageAsync(firPrompt, $"Role: Police, User: {user.Name}");
                
                var firMessage = firResponse.Success && !string.IsNullOrEmpty(firResponse.Message) && !IsAgentRefusal(firResponse.Message)
                    ? firResponse.Message
                    : $"## ✅ FIR Registered Successfully\n\n**FIR Number:** {firNumber}\n**Case:** {caseData.CaseNumber}\n**Investigating Officer:** {user.Name}\n\nSay **'start investigation for {caseData.CaseNumber}'** to proceed.";

                return new AgentCommandResponse
                {
                    Success = true,
                    Message = firMessage,
                    CaseUpdated = true,
                    UpdatedCase = caseData
                };

            case "StartInvestigation":
                if (caseData.Status != CaseStatus.Filed && !caseData.FIRNumber?.StartsWith("FIR") == true)
                {
                    return new AgentCommandResponse
                    {
                        Success = false,
                        Message = "⚠️ Please register FIR first before starting investigation."
                    };
                }

                workflowStep.Stage = WorkflowStages.Investigation;
                workflowStep.Status = "InProgress";
                workflowStep.Notes = $"Investigation started by {user.Name}";

                await _caseService.UpdateCaseWorkflowAsync(caseData.Id, workflowStep);

                // Ask Azure Agent to generate investigation plan
                var investigationPrompt = $@"You are an AI assistant helping a Police Officer start investigation in India.

Investigation has been initiated for this case. Generate a comprehensive investigation plan.

CASE DETAILS:
- Case Number: {caseData.CaseNumber}
- FIR Number: {caseData.FIRNumber ?? "Pending"}
- Type: {caseData.Type}{(caseData.IsCybercrime ? " (Cybercrime)" : "")}
- Title: {caseData.Title}
- Description: {caseData.Description}
- Accused: {caseData.Accused}
- Filed By: {caseData.Complainant}
- Investigating Officer: {user.Name}

Provide:
1. Investigation timeline
2. Key areas to investigate
3. Witnesses to interview
4. Evidence collection priorities
5. Legal considerations
6. Available next actions

Format with proper markdown. Be thorough but concise.";

                var investigationResponse = await _agentService.SendMessageAsync(investigationPrompt, $"Role: Police, User: {user.Name}");
                
                var investigationMessage = investigationResponse.Success && !string.IsNullOrEmpty(investigationResponse.Message) && !IsAgentRefusal(investigationResponse.Message)
                    ? investigationResponse.Message
                    : $"## 🔍 Investigation Started\n\n**Case:** {caseData.CaseNumber}\n**Status:** Under Investigation\n\nSay **'collect evidence for {caseData.CaseNumber}'** or **'file chargesheet for {caseData.CaseNumber}'** to proceed.";

                return new AgentCommandResponse
                {
                    Success = true,
                    Message = investigationMessage,
                    CaseUpdated = true
                };

            case "CollectEvidence":
                workflowStep.Stage = WorkflowStages.EvidenceCollection;
                workflowStep.Status = "InProgress";
                workflowStep.Notes = $"Evidence collection in progress by {user.Name}";

                await _caseService.UpdateCaseWorkflowAsync(caseData.Id, workflowStep);

                // Ask Azure Agent to generate evidence checklist
                var evidencePrompt = $@"You are an AI assistant helping a Police Officer with evidence collection in India.

Generate a comprehensive evidence collection checklist and guide for this case.

CASE DETAILS:
- Case Number: {caseData.CaseNumber}
- Type: {caseData.Type}{(caseData.IsCybercrime ? " (Cybercrime)" : "")}
- Title: {caseData.Title}
- Description: {caseData.Description}
- Accused: {caseData.Accused}

Provide:
1. Evidence checklist specific to this case type
2. Collection procedures
3. Chain of custody guidelines
4. Digital evidence handling (if cybercrime)
5. Documentation requirements
6. Next steps after evidence collection

Format with proper markdown and checkboxes where appropriate.";

                var evidenceResponse = await _agentService.SendMessageAsync(evidencePrompt, $"Role: Police, User: {user.Name}");
                
                var evidenceMessage = evidenceResponse.Success && !string.IsNullOrEmpty(evidenceResponse.Message) && !IsAgentRefusal(evidenceResponse.Message)
                    ? evidenceResponse.Message
                    : $"## 📋 Evidence Collection Started\n\n**Case:** {caseData.CaseNumber}\n\nOnce evidence is collected, say **'file chargesheet for {caseData.CaseNumber}'**";

                return new AgentCommandResponse
                {
                    Success = true,
                    Message = evidenceMessage,
                    CaseUpdated = true
                };

            case "FileChargesheet":
                workflowStep.Stage = WorkflowStages.ChargesheetFiled;
                workflowStep.Status = "Completed";
                workflowStep.Notes = $"Chargesheet filed by {user.Name}";

                await _caseService.UpdateCaseWorkflowAsync(caseData.Id, workflowStep);

                // Ask Azure Agent to generate chargesheet confirmation
                var chargesheetPrompt = $@"You are an AI assistant helping a Police Officer with case management in India.

A chargesheet has been successfully filed. Generate a comprehensive confirmation and next steps guide.

CASE DETAILS:
- Case Number: {caseData.CaseNumber}
- FIR Number: {caseData.FIRNumber ?? "N/A"}
- Type: {caseData.Type}{(caseData.IsCybercrime ? " (Cybercrime)" : "")}
- Title: {caseData.Title}
- Accused: {caseData.Accused}
- Filed By Officer: {user.Name}
- Chargesheet Date: {DateTime.Now:MMMM dd, yyyy}
- IPC Sections: {(caseData.Sections?.Any() == true ? string.Join(", ", caseData.Sections) : "To be determined by court")}

Provide:
1. Chargesheet filing confirmation
2. What happens next in court proceedings
3. Role of police after chargesheet
4. Expected timeline to trial
5. Any follow-up actions needed

Format with proper markdown. Be informative about the legal process.";

                var chargesheetResponse = await _agentService.SendMessageAsync(chargesheetPrompt, $"Role: Police, User: {user.Name}");
                
                var chargesheetMessage = chargesheetResponse.Success && !string.IsNullOrEmpty(chargesheetResponse.Message) && !IsAgentRefusal(chargesheetResponse.Message)
                    ? chargesheetResponse.Message
                    : $"## ✅ Chargesheet Filed\n\n**Case:** {caseData.CaseNumber}\n**Filed By:** {user.Name}\n**Date:** {DateTime.Now:MMMM dd, yyyy}\n\nThe case is now ready for court proceedings.";

                return new AgentCommandResponse
                {
                    Success = true,
                    Message = chargesheetMessage,
                    CaseUpdated = true
                };

            default:
                return new AgentCommandResponse
                {
                    Success = false,
                    Message = "⚠️ Unknown action. Available actions:\n" +
                              "- Register FIR\n" +
                              "- Start Investigation\n" +
                              "- Collect Evidence\n" +
                              "- File Chargesheet"
                };
        }
    }

    private async Task<AgentCommandResponse> HandleLawyerActionAsync(string action, Case caseData, User user, string query)
    {
        var workflowStep = new CaseWorkflowStep
        {
            Date = DateTime.UtcNow,
            Actor = user.Name,
            ActorRole = "Lawyer"
        };

        switch (action)
        {
            case "AcceptCase":
                if (caseData.Status != CaseStatus.ChargesheetFiled && caseData.Status != CaseStatus.InProgress)
                {
                    return new AgentCommandResponse
                    {
                        Success = false,
                        Message = $"⚠️ This case is not available for lawyer assignment. Current status: {caseData.Status}"
                    };
                }

                caseData.AssignedLawyer = user.Name;
                caseData.LawyerName = $"Adv. {user.Name}";

                workflowStep.Stage = WorkflowStages.LawyerAssigned;
                workflowStep.Status = "Completed";
                workflowStep.Notes = $"Case accepted by Adv. {user.Name}";

                await _caseService.UpdateCaseWorkflowAsync(caseData.Id, workflowStep);

                // Ask Azure Agent to generate case acceptance response
                var acceptPrompt = $@"You are an AI assistant helping a Lawyer with case management in India.

A lawyer has just accepted this case. Generate a comprehensive case briefing and initial analysis.

CASE DETAILS:
- Case Number: {caseData.CaseNumber}
- Title: {caseData.Title}
- Type: {caseData.Type}{(caseData.IsCybercrime ? " (Cybercrime)" : "")}
- Assigned Lawyer: Adv. {user.Name}
- Description: {caseData.Description}
- Accused: {caseData.Accused}
- IPC Sections: {(caseData.Sections?.Any() == true ? string.Join(", ", caseData.Sections) : "Not specified")}
- FIR Number: {caseData.FIRNumber ?? "N/A"}
- Filed By: {caseData.Complainant}

Provide:
1. Case acceptance confirmation
2. Initial legal analysis
3. Key strengths and weaknesses
4. Relevant laws and sections to consider
5. Recommended defense/prosecution strategy points
6. Available actions for the lawyer
7. Similar precedents to research

Format with proper markdown. Be thorough and legally focused.";

                var acceptResponse = await _agentService.SendMessageAsync(acceptPrompt, $"Role: Lawyer, User: {user.Name}");
                
                var acceptMessage = acceptResponse.Success && !string.IsNullOrEmpty(acceptResponse.Message) && !IsAgentRefusal(acceptResponse.Message)
                    ? acceptResponse.Message
                    : $"## ✅ Case Accepted\n\n**Case:** {caseData.CaseNumber} - {caseData.Title}\n**Assigned Lawyer:** Adv. {user.Name}\n\nSay **'prepare defense for {caseData.CaseNumber}'** to get defense strategy.";

                return new AgentCommandResponse
                {
                    Success = true,
                    Message = acceptMessage,
                    CaseUpdated = true,
                    UpdatedCase = caseData
                };

            case "PrepareDefense":
            case "PrepareCase":
                // Ask Azure Agent to generate defense strategy
                var defensePrompt = $@"You are an AI assistant helping a Lawyer prepare defense strategy in India.

Generate a comprehensive defense preparation guide for this case.

CASE DETAILS:
- Case Number: {caseData.CaseNumber}
- Title: {caseData.Title}
- Type: {caseData.Type}{(caseData.IsCybercrime ? " (Cybercrime)" : "")}
- Description: {caseData.Description}
- Accused: {caseData.Accused}
- IPC Sections: {(caseData.Sections?.Any() == true ? string.Join(", ", caseData.Sections) : "Not specified")}
- Filed By: {caseData.Complainant}

Provide comprehensive defense preparation including:
1. Defense Strategy Overview
2. Key Legal Arguments to Make
3. Evidence to Gather/Challenge
4. Witnesses to Consider
5. Relevant Legal Precedents
6. Potential Prosecution Arguments & Counter-strategies
7. Court Preparation Checklist
8. Timeline for Preparation

Format with proper markdown. Be detailed and actionable.";

                var defenseResponse = await _agentService.SendMessageAsync(defensePrompt, $"Role: Lawyer, User: {user.Name}");
                
                var defenseMessage = defenseResponse.Success && !string.IsNullOrEmpty(defenseResponse.Message) && !IsAgentRefusal(defenseResponse.Message)
                    ? defenseResponse.Message
                    : $"## ⚖️ Defense Preparation - {caseData.CaseNumber}\n\nUnable to generate defense strategy. Please try again.";

                return new AgentCommandResponse
                {
                    Success = true,
                    Message = defenseMessage,
                    CaseUpdated = false
                };

            case "FileMotion":
                workflowStep.Stage = WorkflowStages.CourtHearing;
                workflowStep.Status = "InProgress";
                workflowStep.Notes = $"Motion filed by Adv. {user.Name}";

                await _caseService.UpdateCaseWorkflowAsync(caseData.Id, workflowStep);

                // Ask Azure Agent to generate motion filing response
                var motionPrompt = $@"You are an AI assistant helping a Lawyer with court filings in India.

A motion has been filed for this case. Generate a confirmation and next steps guide.

CASE DETAILS:
- Case Number: {caseData.CaseNumber}
- Title: {caseData.Title}
- Filed By: Adv. {user.Name}
- Filing Date: {DateTime.Now:MMMM dd, yyyy}
- Case Type: {caseData.Type}

Provide:
1. Motion filing confirmation
2. What to expect after filing
3. Typical timeline for hearing
4. Documents to prepare
5. Court etiquette reminders
6. Next actions available

Format with proper markdown.";

                var motionResponse = await _agentService.SendMessageAsync(motionPrompt, $"Role: Lawyer, User: {user.Name}");
                
                var motionMessage = motionResponse.Success && !string.IsNullOrEmpty(motionResponse.Message) && !IsAgentRefusal(motionResponse.Message)
                    ? motionResponse.Message
                    : $"## 📄 Motion Filed\n\n**Case:** {caseData.CaseNumber}\n**Filed By:** Adv. {user.Name}\n**Date:** {DateTime.Now:MMMM dd, yyyy}\n\nThe motion has been submitted to the court. Awaiting hearing date.";

                return new AgentCommandResponse
                {
                    Success = true,
                    Message = motionMessage,
                    CaseUpdated = true
                };

            default:
                return new AgentCommandResponse
                {
                    Success = false,
                    Message = "⚠️ Unknown action. Available actions:\n" +
                              "- Accept/Take Case\n" +
                              "- Prepare Defense\n" +
                              "- File Motion\n" +
                              "- Analyze Case"
                };
        }
    }

    #endregion

    #region Analysis Handlers

    private async Task<AgentCommandResponse> HandleAnalyzeCaseAsync(string query, User user, DetectedIntent intent)
    {
        if (!intent.CaseId.HasValue && string.IsNullOrEmpty(intent.CaseNumber))
        {
            return new AgentCommandResponse
            {
                Success = false,
                Message = "📋 Please specify which case to analyze.\n\n" +
                          "Example: **'analyze case CYB/2025/001'** or **'analyze case #1'**",
                RequiresInput = true
            };
        }

        var caseData = intent.CaseId.HasValue
            ? await _caseService.GetCaseByIdAsync(intent.CaseId.Value)
            : (await _caseService.SearchCasesAsync(intent.CaseNumber ?? "")).FirstOrDefault();

        if (caseData == null)
        {
            return new AgentCommandResponse
            {
                Success = false,
                Message = "❌ Case not found."
            };
        }

        // Build role-specific analysis prompt for Azure Agent
        var analysisPrompt = BuildCaseAnalysisPromptForAgent(caseData, user);
        var context = $"Role: {user.Role}, User: {user.Name}, Analysis Request";
        var response = await _agentService.SendMessageAsync(analysisPrompt, context);
        
        string analysis;
        if (response.Success && !string.IsNullOrEmpty(response.Message) && !IsAgentRefusal(response.Message))
        {
            analysis = response.Message;
        }
        else
        {
            // Fallback to role-specific analysis
            analysis = user.Role switch
            {
                UserRole.Police => await GetAIPoliceAnalysisAsync(caseData),
                UserRole.Lawyer => await GetAICaseAnalysisForLawyerAsync(caseData),
                _ => await GetAICitizenCaseStatusAsync(caseData)
            };
        }

        return new AgentCommandResponse
        {
            Success = true,
            Message = analysis,
            RelatedCase = caseData
        };
    }

    private string BuildCaseAnalysisPromptForAgent(Case caseData, User user)
    {
        var roleContext = user.Role switch
        {
            UserRole.Police => @"You are analyzing this case for a Police Officer.
Focus on: Investigation angle, evidence assessment, legal sections applicable, prosecution strength, investigation gaps.
Provide actionable insights for law enforcement.",

            UserRole.Lawyer => @"You are analyzing this case for a Lawyer.
Focus on: Legal merits, defense/prosecution strategy, precedents, evidence strength, procedural aspects, winning strategy.
Provide detailed legal analysis.",

            UserRole.Citizen => @"You are analyzing this case for a Citizen (complainant/accused).
Focus on: Current status explained simply, what they can expect, timeline estimates, their rights, what they can do.
Use clear, non-legal language they can understand.",

            _ => "Provide a comprehensive case analysis."
        };

        return $@"{roleContext}

Analyze the following case comprehensively:

CASE INFORMATION:
- Case Number: {caseData.CaseNumber}
- Title: {caseData.Title}
- Type: {caseData.Type}{(caseData.IsCybercrime ? " (Cybercrime)" : "")}
- Status: {caseData.Status}
- Filed Date: {caseData.FiledDate:MMM dd, yyyy}
- Filed By: {caseData.Complainant}

DESCRIPTION:
{caseData.Description}

PARTIES:
- Accused: {caseData.Accused}
{(string.IsNullOrEmpty(caseData.InvestigatingOfficer) ? "" : $"- Investigating Officer: {caseData.InvestigatingOfficer}")}
{(string.IsNullOrEmpty(caseData.AssignedLawyer) ? "" : $"- Assigned Lawyer: {caseData.AssignedLawyer}")}

LEGAL DETAILS:
- IPC Sections: {(caseData.Sections?.Any() == true ? string.Join(", ", caseData.Sections) : "Not specified")}
{(caseData.ApplicableLaws?.Any() == true ? $"- Related Laws: {string.Join(", ", caseData.ApplicableLaws)}" : "")}

CASE HISTORY:
- FIR Number: {caseData.FIRNumber ?? "Not registered"}
{(caseData.NextHearingDate.HasValue ? $"- FIR Date: {caseData.NextHearingDate:MMM dd, yyyy}" : "")}
{(caseData.LastUpdated.HasValue ? $"- Chargesheet Date: {caseData.LastUpdated:MMM dd, yyyy}" : "")}
{(caseData.NextHearingDate.HasValue ? $"- Court Date: {caseData.NextHearingDate:MMM dd, yyyy}" : "")}

Provide a detailed analysis with proper markdown formatting including:
1. Case Overview
2. Key Facts Analysis  
3. Legal Assessment
4. Strengths and Weaknesses
5. Recommended Actions
6. Important Considerations";
    }

    private async Task<AgentCommandResponse> HandleCaseDetailsAsync(string query, User user, DetectedIntent intent)
    {
        if (!intent.CaseId.HasValue && string.IsNullOrEmpty(intent.CaseNumber))
        {
            var userCases = user.Role == UserRole.Citizen 
                ? await _caseService.GetCasesByUserAsync(user.Email)
                : await _caseService.GetAllCasesAsync();

            if (userCases.Any())
            {
                // Ask Azure Agent to generate the prompt for case selection
                var caseListData = string.Join("\n", userCases.Take(5).Select(c => 
                    $"- Case: {c.CaseNumber}, Title: {c.Title}, Status: {c.Status}, Type: {c.Type}"));
                
                var selectionPrompt = $@"The user ({user.Role}) wants to see case details but didn't specify which case.
Available cases:
{caseListData}

Generate a friendly response asking them to specify which case they want details for.
Include the case numbers in your response so they can easily reference them.
Keep it concise and helpful.";

                var agentResponse = await _agentService.SendMessageAsync(selectionPrompt, $"Role: {user.Role}, User: {user.Name}");
                
                var message = agentResponse.Success && !string.IsNullOrEmpty(agentResponse.Message) && !IsAgentRefusal(agentResponse.Message)
                    ? agentResponse.Message
                    : $"📋 Please specify which case:\n\n{string.Join("\n", userCases.Take(5).Select(c => $"• **{c.CaseNumber}** - {c.Title} ({c.Status})"))}\n\nSay **'show details for [case number]'**";
                
                return new AgentCommandResponse
                {
                    Success = false,
                    Message = message,
                    RequiresInput = true
                };
            }
        }

        var caseData = intent.CaseId.HasValue
            ? await _caseService.GetCaseByIdAsync(intent.CaseId.Value)
            : (await _caseService.SearchCasesAsync(intent.CaseNumber ?? "")).FirstOrDefault();

        if (caseData == null)
        {
            return new AgentCommandResponse
            {
                Success = false,
                Message = "❌ Case not found."
            };
        }

        // Build comprehensive case details prompt for Azure Agent
        var caseDetailsPrompt = BuildCaseDetailsPromptForAgent(caseData, user, query);
        var context = $"Role: {user.Role}, User: {user.Name}, Query: {query}";
        var response = await _agentService.SendMessageAsync(caseDetailsPrompt, context);
        
        string details;
        if (response.Success && !string.IsNullOrEmpty(response.Message) && !IsAgentRefusal(response.Message))
        {
            details = response.Message;
        }
        else
        {
            // Fallback to local formatting only if agent unavailable
            details = FormatCaseDetailsForRole(caseData, user.Role);
        }
        
        return new AgentCommandResponse
        {
            Success = true,
            Message = details,
            RelatedCase = caseData,
            Actions = GetActionsForCase(caseData, user.Role)
        };
    }

    private string BuildCaseDetailsPromptForAgent(Case caseData, User user, string query)
    {
        var roleInstructions = user.Role switch
        {
            UserRole.Police => @"You are helping a Police Officer view case details.
Focus on: Investigation status, evidence collection, accused information, legal sections applicable, next investigative steps.
Highlight any urgent actions needed.",
            
            UserRole.Lawyer => @"You are helping a Lawyer review case details.
Focus on: Legal aspects, applicable laws/sections, case strength analysis, defense/prosecution strategy points, court preparation needs.
Provide legal analysis perspective.",
            
            UserRole.Citizen => @"You are helping a Citizen understand their case.
Focus on: Current status explained in simple terms, what happens next, estimated timeline, what they can do.
Use friendly, non-technical language.",
            
            _ => "Present the case details clearly and comprehensively."
        };

        return $@"{roleInstructions}

Present the following case details to the user:

CASE INFORMATION:
- Case Number: {caseData.CaseNumber}
- Title: {caseData.Title}
- Type: {caseData.Type}{(caseData.IsCybercrime ? " (Cybercrime)" : "")}
- Status: {caseData.Status}
- Filed Date: {caseData.FiledDate:MMM dd, yyyy}
- Filed By: {caseData.Complainant}

PARTIES:
- Accused: {caseData.Accused}
{(string.IsNullOrEmpty(caseData.InvestigatingOfficer) ? "" : $"- Assigned Officer: {caseData.InvestigatingOfficer}")}
{(string.IsNullOrEmpty(caseData.AssignedLawyer) ? "" : $"- Assigned Lawyer: {caseData.AssignedLawyer}")}

DESCRIPTION:
{caseData.Description}

LEGAL:
{(caseData.Sections?.Any() != true ? "- IPC Sections: Not specified" : $"- IPC Sections: {string.Join(", ", caseData.Sections)}")}
{(caseData.ApplicableLaws?.Any() == true ? $"- Related Laws: {string.Join(", ", caseData.ApplicableLaws)}" : "")}

HISTORY:
{(caseData.NextHearingDate.HasValue ? $"- FIR Registered: {caseData.NextHearingDate:MMM dd, yyyy}" : "- FIR: Not yet registered")}
{(caseData.LastUpdated.HasValue ? $"- Chargesheet Filed: {caseData.LastUpdated:MMM dd, yyyy}" : "")}
{(caseData.NextHearingDate.HasValue ? $"- Court Date: {caseData.NextHearingDate:MMM dd, yyyy}" : "")}

User's query: {query}

Format your response with proper markdown headings and sections. Be informative but concise.";
    }

    private async Task<AgentCommandResponse> HandleCaseProgressAsync(string query, User user, DetectedIntent intent)
    {
        Case? caseData = null;

        if (intent.CaseId.HasValue || !string.IsNullOrEmpty(intent.CaseNumber))
        {
            caseData = intent.CaseId.HasValue
                ? await _caseService.GetCaseByIdAsync(intent.CaseId.Value)
                : (await _caseService.SearchCasesAsync(intent.CaseNumber ?? "")).FirstOrDefault();
        }
        else if (user.Role == UserRole.Citizen)
        {
            var userCases = await _caseService.GetCasesByUserAsync(user.Email);
            caseData = userCases.OrderByDescending(c => c.FiledDate).FirstOrDefault();
        }

        if (caseData == null)
        {
            return new AgentCommandResponse
            {
                Success = false,
                Message = "📋 No case found. Please specify the case number."
            };
        }

        var workflow = await _caseService.GetCaseWorkflowAsync(caseData.Id);
        
        // Build progress prompt for Azure Agent
        var progressPrompt = BuildCaseProgressPromptForAgent(caseData, workflow, user);
        var context = $"Role: {user.Role}, User: {user.Name}";
        var response = await _agentService.SendMessageAsync(progressPrompt, context);
        
        string progressReport;
        if (response.Success && !string.IsNullOrEmpty(response.Message) && !IsAgentRefusal(response.Message))
        {
            progressReport = response.Message;
        }
        else
        {
            // Fallback to local formatting only if agent unavailable
            progressReport = FormatWorkflowProgress(caseData, workflow, user.Role);
        }

        return new AgentCommandResponse
        {
            Success = true,
            Message = progressReport,
            RelatedCase = caseData
        };
    }

    private string BuildCaseProgressPromptForAgent(Case caseData, IEnumerable<CaseWorkflowStep> workflow, User user)
    {
        var roleContext = user.Role switch
        {
            UserRole.Police => "You are explaining case progress to a Police Officer. Focus on investigation stages, what's completed, what needs action.",
            UserRole.Lawyer => "You are explaining case progress to a Lawyer. Focus on legal milestones, court dates, preparation status.",
            UserRole.Citizen => "You are explaining case progress to a Citizen. Use simple language, explain what each stage means, give realistic expectations.",
            _ => "Explain the case progress clearly."
        };

        var workflowSteps = workflow.ToList();
        var completedSteps = workflowSteps.Where(w => w.Status == "Completed").Select(w => $"✅ {w.Stage} - {w.Date:MMM dd, yyyy}");
        var pendingSteps = workflowSteps.Where(w => w.Status != "Completed").Select(w => $"⏳ {w.Stage}");
        
        return $@"{roleContext}

Present the progress/timeline for this case:

CASE: {caseData.CaseNumber} - {caseData.Title}
Current Status: {caseData.Status}
Filed: {caseData.FiledDate:MMM dd, yyyy}

WORKFLOW PROGRESS:
Completed Steps:
{(completedSteps.Any() ? string.Join("\n", completedSteps) : "- No steps completed yet")}

Pending Steps:
{(pendingSteps.Any() ? string.Join("\n", pendingSteps) : "- All steps completed")}

KEY DATES:
{(caseData.NextHearingDate.HasValue ? $"- FIR: {caseData.NextHearingDate:MMM dd, yyyy}" : "- FIR: Pending")}
{(caseData.LastUpdated.HasValue ? $"- Chargesheet: {caseData.LastUpdated:MMM dd, yyyy}" : "")}
{(caseData.NextHearingDate.HasValue ? $"- Next Court Date: {caseData.NextHearingDate:MMM dd, yyyy}" : "")}

Generate a clear, visual progress report. Use a timeline or progress bar representation if helpful.
Explain what the current status means and what the user can expect next.
Keep the response informative but concise.";
    }

    private async Task<AgentCommandResponse> HandleUpdateStatusAsync(string query, User user, DetectedIntent intent)
    {
        return new AgentCommandResponse
        {
            Success = false,
            Message = "⚠️ Please use specific actions to update case status:\n\n" +
                      (user.Role == UserRole.Police 
                          ? "- **'register FIR for [case]'**\n- **'start investigation for [case]'**\n- **'file chargesheet for [case]'**"
                          : "- **'accept case [case number]'**\n- **'prepare defense for [case]'**")
        };
    }

    private async Task<AgentCommandResponse> HandleAssignCaseAsync(string query, User user, DetectedIntent intent)
    {
        if (user.Role != UserRole.Police && user.Role != UserRole.Admin)
        {
            return new AgentCommandResponse
            {
                Success = false,
                Message = "⚠️ Only Police officers can assign cases."
            };
        }

        return new AgentCommandResponse
        {
            Success = true,
            Message = "To assign a case, use:\n- **'assign [officer name] to case [case number]'**"
        };
    }

    private async Task<AgentCommandResponse> HandleGenerateDocumentAsync(string query, User user, DetectedIntent intent)
    {
        return new AgentCommandResponse
        {
            Success = true,
            Message = "📄 Document generation available:\n\n" +
                      (user.Role == UserRole.Police 
                          ? "- FIR Document\n- Investigation Report\n- Chargesheet Draft"
                          : user.Role == UserRole.Lawyer
                              ? "- Case Brief\n- Legal Notice\n- Court Filing\n- Defense Arguments"
                              : "- Complaint Summary\n- Case Progress Report")
        };
    }

    private async Task<AgentCommandResponse> HandleLegalAdviceAsync(string query, User user, DetectedIntent intent)
    {
        var response = await _agentService.SendMessageAsync(
            $"As a legal expert in Indian law, answer this query: {query}\n\n" +
            $"User Role: {user.Role}. Provide practical, relevant advice.",
            $"User: {user.Name}, Role: {user.Role}"
        );

        if (response.Success && !IsAgentRefusal(response.Message))
        {
            return new AgentCommandResponse
            {
                Success = true,
                Message = $"## ⚖️ Legal Information\n\n{response.Message}",
                RelatedLaws = response.RelatedLaws
            };
        }

        return await GetFallbackLegalAdviceAsync(query);
    }

    private async Task<AgentCommandResponse> HandleGeneralQueryAsync(string query, User user)
    {
        // Get role-specific context
        var roleContext = user.Role switch
        {
            UserRole.Police => "You are helping a police officer with case management, investigation, and FIR processing.",
            UserRole.Lawyer => "You are helping a lawyer with case analysis, legal strategy, and court preparation.",
            UserRole.Citizen => "You are helping a citizen file complaints, track cases, and understand their legal rights.",
            _ => "You are an AI legal assistant."
        };

        var response = await _agentService.SendMessageAsync(
            $"{roleContext}\n\nUser query: {query}",
            $"User: {user.Name}, Role: {user.Role}"
        );

        if (response.Success && !IsAgentRefusal(response.Message))
        {
            return new AgentCommandResponse
            {
                Success = true,
                Message = response.Message
            };
        }

        // Provide helpful fallback
        return new AgentCommandResponse
        {
            Success = true,
            Message = GetRoleBasedHelp(user.Role)
        };
    }

    #endregion

    #region AI Analysis Methods

    private async Task<string> GetAIInvestigationSuggestionsAsync(Case caseData)
    {
        var prompt = $@"You are an AI assistant for police investigation in India.

Case Details:
- Type: {caseData.Type} {(caseData.IsCybercrime ? "(Cybercrime)" : "")}
- Title: {caseData.Title}
- Description: {caseData.Description}
- Accused: {caseData.Accused}

Provide brief investigation suggestions:
1. Key areas to investigate
2. Evidence to collect
3. Relevant legal sections
4. Priority actions

Be concise (max 200 words).";

        var response = await _agentService.SendMessageAsync(prompt);
        
        if (response.Success && !IsAgentRefusal(response.Message))
            return response.Message;

        // Fallback
        return GetFallbackInvestigationSuggestions(caseData);
    }

    private async Task<string> GetAIInvestigationPlanAsync(Case caseData)
    {
        var prompt = $@"Create a brief investigation plan for:
Case: {caseData.CaseNumber}
Type: {caseData.Type}
Description: {caseData.Description}

Include: Timeline, Key witnesses, Evidence needed, Legal considerations.
Keep it under 250 words.";

        var response = await _agentService.SendMessageAsync(prompt);
        return (response.Success && !IsAgentRefusal(response.Message)) ? response.Message : GetFallbackInvestigationPlan(caseData);
    }

    private async Task<string> GetAIEvidenceChecklistAsync(Case caseData)
    {
        if (caseData.IsCybercrime)
        {
            return "### Digital Evidence Checklist\n\n" +
                   "- [ ] Screenshots of fraudulent communications\n" +
                   "- [ ] Transaction records/Bank statements\n" +
                   "- [ ] IP address logs\n" +
                   "- [ ] Email headers\n" +
                   "- [ ] Device forensics (if applicable)\n" +
                   "- [ ] Social media evidence\n" +
                   "- [ ] Witness statements\n" +
                   "- [ ] CCTV footage (if physical location involved)";
        }

        return "### Evidence Checklist\n\n" +
               "- [ ] Witness statements\n" +
               "- [ ] Physical evidence\n" +
               "- [ ] Documentary evidence\n" +
               "- [ ] Forensic reports\n" +
               "- [ ] CCTV footage\n" +
               "- [ ] Expert opinions";
    }

    private async Task<string> GetAICaseAnalysisForLawyerAsync(Case caseData)
    {
        var prompt = $@"Provide a legal analysis for a lawyer:

Case: {caseData.CaseNumber}
Type: {caseData.Type}
Description: {caseData.Description}
Applicable Laws: {string.Join(", ", caseData.ApplicableLaws)}
Sections: {string.Join(", ", caseData.Sections)}

Analyze:
1. Case strength (Strong/Medium/Weak)
2. Key legal issues
3. Potential challenges
4. Recommended strategy
5. Similar precedents to reference

Be professional and concise (300 words max).";

        var response = await _agentService.SendMessageAsync(prompt);
        return (response.Success && !IsAgentRefusal(response.Message)) ? response.Message : GetFallbackLawyerAnalysis(caseData);
    }

    private async Task<string> GetAIDefenseStrategyAsync(Case caseData)
    {
        var prompt = $@"Prepare defense strategy for:
Case: {caseData.CaseNumber}
Type: {caseData.Type}
Charges/Sections: {string.Join(", ", caseData.Sections)}

Provide:
1. Defense approach
2. Key arguments
3. Evidence to challenge
4. Procedural points
5. Recommended witnesses

Professional tone, 300 words max.";

        var response = await _agentService.SendMessageAsync(prompt);
        
        if (response.Success && !IsAgentRefusal(response.Message))
            return response.Message;

        return $"### Defense Strategy for {caseData.CaseNumber}\n\n" +
               $"**Approach:** Review all evidence for procedural compliance\n\n" +
               $"**Key Points:**\n" +
               $"1. Verify chain of custody for all evidence\n" +
               $"2. Challenge any procedural irregularities\n" +
               $"3. Request full disclosure of prosecution evidence\n" +
               $"4. Prepare witness examination strategy\n\n" +
               $"**Next Steps:**\n" +
               $"- File for document discovery\n" +
               $"- Prepare opening statement\n" +
               $"- Identify expert witnesses if needed";
    }

    private async Task<string> GetAIPoliceAnalysisAsync(Case caseData)
    {
        return $"## 🔍 Investigation Analysis - {caseData.CaseNumber}\n\n" +
               $"**Case Status:** {caseData.Status}\n" +
               $"**Type:** {caseData.Type}\n" +
               $"**FIR:** {caseData.FIRNumber ?? "Not registered"}\n\n" +
               $"### Investigation Priority\n" +
               $"{(caseData.IsCybercrime ? "🔴 HIGH - Cybercrime case requires immediate digital evidence preservation" : "🟡 NORMAL")}\n\n" +
               $"### Recommended Actions\n" +
               await GetAIInvestigationSuggestionsAsync(caseData);
    }

    private async Task<string> GetAICitizenCaseStatusAsync(Case caseData)
    {
        return $"## 📋 Your Case Status\n\n" +
               $"**Case Number:** {caseData.CaseNumber}\n" +
               $"**Title:** {caseData.Title}\n" +
               $"**Current Status:** {caseData.Status}\n" +
               $"**Filed On:** {caseData.FiledDate:MMMM dd, yyyy}\n\n" +
               (caseData.FIRNumber != null ? $"**FIR Number:** {caseData.FIRNumber}\n" : "") +
               (caseData.InvestigatingOfficer != null ? $"**Investigating Officer:** {caseData.InvestigatingOfficer}\n" : "") +
               (caseData.AssignedLawyer != null ? $"**Assigned Lawyer:** {caseData.AssignedLawyer}\n" : "") +
               $"\n### What This Means\n" +
               GetStatusExplanation(caseData.Status);
    }

    #endregion

    #region Formatting Helpers

    private string FormatCaseListForRole(List<Case> cases, UserRole role)
    {
        if (!cases.Any())
        {
            return role switch
            {
                UserRole.Police => "📋 No pending cases require your attention at this time.",
                UserRole.Lawyer => "📋 No cases are currently available for assignment.",
                _ => "📋 You haven't filed any cases yet. Say **'file a complaint'** to get started."
            };
        }

        var header = role switch
        {
            UserRole.Police => $"## 🛡️ Cases Dashboard ({cases.Count} cases)\n\n",
            UserRole.Lawyer => $"## ⚖️ Available Cases ({cases.Count} cases)\n\n",
            _ => $"## 📋 Your Filed Cases ({cases.Count})\n\n"
        };

        string caseList;
        
        if (role == UserRole.Citizen)
        {
            // Enhanced citizen view with progress indicators
            caseList = string.Join("\n\n", cases.Take(10).Select(c =>
            {
                var statusIcon = GetStatusIcon(c.Status);
                var typeTag = c.IsCybercrime ? "🔒 Cybercrime" : "📄 General";
                var daysAgo = (DateTime.Now - c.FiledDate).Days;
                var timeText = daysAgo == 0 ? "Filed today" : daysAgo == 1 ? "Filed yesterday" : $"Filed {daysAgo} days ago";
                
                var caseInfo = $"### {c.CaseNumber}\n" +
                       $"**{c.Title}**\n" +
                       $"📌 Type: {typeTag}\n" +
                       $"{statusIcon} Status: **{c.Status}**\n" +
                       $"🕐 {timeText}";

                // Add progress context for citizen
                if (c.FIRNumber != null)
                    caseInfo += $"\n✅ FIR: {c.FIRNumber}";
                if (c.InvestigatingOfficer != null)
                    caseInfo += $"\n👮 Officer: {c.InvestigatingOfficer}";
                if (c.AssignedLawyer != null)
                    caseInfo += $"\n⚖️ Lawyer: {c.AssignedLawyer}";
                if (c.NextHearingDate.HasValue)
                    caseInfo += $"\n📅 Next Hearing: {c.NextHearingDate:MMM dd, yyyy}";

                return caseInfo;
            }));
        }
        else
        {
            caseList = string.Join("\n\n", cases.Take(10).Select(c =>
            {
                var statusIcon = GetStatusIcon(c.Status);
                var priority = c.IsCybercrime ? "🔴" : "🟢";
                
                return $"### {priority} {c.CaseNumber}\n" +
                       $"**{c.Title}**\n" +
                       $"Status: {statusIcon} {c.Status}\n" +
                       $"Filed: {c.FiledDate:MMM dd, yyyy}" +
                       (role == UserRole.Police && c.InvestigatingOfficer != null ? $" | Officer: {c.InvestigatingOfficer}" : "") +
                       (role == UserRole.Lawyer && c.AssignedLawyer != null ? $" | Lawyer: {c.AssignedLawyer}" : "");
            }));
        }

        var actions = role switch
        {
            UserRole.Police => "\n\n---\n**Actions:** Say **'register FIR for [case number]'** or **'analyze case [number]'**",
            UserRole.Lawyer => "\n\n---\n**Actions:** Say **'accept case [number]'** or **'analyze case [number]'**",
            _ => "\n\n---\n**Quick Actions:**\n" +
                 "• **'track progress for [case number]'** - see full timeline\n" +
                 "• **'case details [case number]'** - get complete info\n" +
                 "• **'show my cybercrime cases'** - filter by type\n" +
                 "• **'categorize my cases'** - see summary dashboard"
        };

        if (cases.Count > 10)
        {
            header += $"*Showing 10 of {cases.Count} cases*\n\n";
        }

        return header + caseList + actions;
    }

    private string FormatCaseDetailsForRole(Case caseData, UserRole role)
    {
        var header = $"## 📋 Case Details: {caseData.CaseNumber}\n\n";
        
        var basicInfo = $"**Title:** {caseData.Title}\n" +
                        $"**Type:** {caseData.Type} {(caseData.IsCybercrime ? "🔒 Cybercrime" : "")}\n" +
                        $"**Status:** {GetStatusIcon(caseData.Status)} {caseData.Status}\n" +
                        $"**Filed:** {caseData.FiledDate:MMMM dd, yyyy}\n\n" +
                        $"**Description:**\n{caseData.Description}\n\n";

        var roleSpecific = role switch
        {
            UserRole.Police => $"### 🛡️ Investigation Details\n" +
                              $"**FIR:** {caseData.FIRNumber ?? "Not registered"}\n" +
                              $"**Officer:** {caseData.InvestigatingOfficer ?? "Not assigned"}\n" +
                              $"**Station:** {caseData.PoliceStation ?? "N/A"}\n" +
                              $"**Accused:** {caseData.Accused}\n" +
                              $"**Evidence:** {(caseData.DigitalEvidenceCollected ? "✅ Collected" : "⏳ Pending")}\n",

            UserRole.Lawyer => $"### ⚖️ Legal Details\n" +
                              $"**Assigned Lawyer:** {caseData.AssignedLawyer ?? "Not assigned"}\n" +
                              $"**Court:** {caseData.Court ?? "Not assigned"}\n" +
                              $"**Next Hearing:** {caseData.NextHearingDate?.ToString("MMMM dd, yyyy") ?? "Not scheduled"}\n" +
                              $"**Applicable Laws:** {string.Join(", ", caseData.ApplicableLaws)}\n" +
                              $"**Sections:** {string.Join(", ", caseData.Sections)}\n",

            _ => $"### 📊 Status Information\n" +
                 $"**Complainant:** {caseData.Complainant}\n" +
                 $"**Accused:** {caseData.Accused}\n" +
                 (caseData.FIRNumber != null ? $"**FIR Number:** {caseData.FIRNumber}\n" : "") +
                 (caseData.AssignedLawyer != null ? $"**Lawyer:** {caseData.AssignedLawyer}\n" : "")
        };

        return header + basicInfo + roleSpecific;
    }

    private string FormatWorkflowProgress(Case caseData, List<CaseWorkflowStep> workflow, UserRole role)
    {
        var stages = WorkflowStages.GetStandardFlow();
        var currentStageIndex = workflow.Any() 
            ? stages.IndexOf(workflow.Last().Stage) 
            : 0;

        var progressBar = "## 📊 Case Progress: " + caseData.CaseNumber + "\n\n";
        
        for (int i = 0; i < stages.Count; i++)
        {
            var stage = stages[i];
            var completedStep = workflow.FirstOrDefault(w => w.Stage == stage);
            
            if (completedStep != null && completedStep.Status == "Completed")
            {
                progressBar += $"✅ **{stage}** - {completedStep.Date:MMM dd, yyyy} by {completedStep.Actor}\n";
            }
            else if (i == currentStageIndex + 1)
            {
                progressBar += $"🔄 **{stage}** ← Current Stage\n";
            }
            else if (i > currentStageIndex + 1)
            {
                progressBar += $"⬜ {stage}\n";
            }
        }

        progressBar += $"\n### Current Status: {caseData.Status}\n";
        progressBar += GetStatusExplanation(caseData.Status);

        return progressBar;
    }

    private string GetStatusIcon(CaseStatus status)
    {
        return status switch
        {
            CaseStatus.Filed => "📝",
            CaseStatus.UnderInvestigation => "🔍",
            CaseStatus.ChargesheetFiled => "📋",
            CaseStatus.InProgress => "⚡",
            CaseStatus.TrialInProgress => "⚖️",
            CaseStatus.Judgement => "🔨",
            CaseStatus.Closed => "✅",
            CaseStatus.Dismissed => "❌",
            _ => "📌"
        };
    }

    private string GetStatusExplanation(CaseStatus status)
    {
        return status switch
        {
            CaseStatus.Filed => "Your complaint has been registered and is awaiting police action.",
            CaseStatus.UnderInvestigation => "Police are actively investigating your case.",
            CaseStatus.ChargesheetFiled => "Investigation complete. Chargesheet submitted to court.",
            CaseStatus.InProgress => "Case is being processed through the legal system.",
            CaseStatus.TrialInProgress => "Court proceedings are underway.",
            CaseStatus.Judgement => "The court has delivered its verdict.",
            CaseStatus.Closed => "The case has been closed.",
            _ => "Case is being processed."
        };
    }

    private List<string> GetAvailableActionsForRole(UserRole role, List<Case> cases)
    {
        if (role == UserRole.Police)
        {
            return new List<string>
            {
                "Register FIR",
                "Start Investigation",
                "Collect Evidence",
                "File Chargesheet",
                "Analyze Case"
            };
        }
        else if (role == UserRole.Lawyer)
        {
            return new List<string>
            {
                "Accept Case",
                "Analyze Case",
                "Prepare Defense",
                "Find Precedents",
                "File Motion"
            };
        }
        else
        {
            // Citizen actions based on their cases
            var actions = new List<string>();
            
            if (!cases.Any())
            {
                actions.Add("File a complaint");
                actions.Add("Learn about my rights");
            }
            else
            {
                actions.Add("Track case progress");
                actions.Add("Show cybercrime cases");
                actions.Add("Categorize my cases");
                actions.Add("File another complaint");
                
                // Add case-specific actions
                var pendingCases = cases.Where(c => c.Status == CaseStatus.Filed).Take(2);
                foreach (var c in pendingCases)
                {
                    actions.Add($"Track {c.CaseNumber}");
                }
            }
            
            return actions;
        }
    }

    private List<string> GetActionsForCase(Case caseData, UserRole role)
    {
        var actions = new List<string>();

        if (role == UserRole.Police)
        {
            if (caseData.Status == CaseStatus.Filed && caseData.FIRNumber == null)
                actions.Add($"Register FIR for {caseData.CaseNumber}");
            if (caseData.FIRNumber != null && caseData.Status == CaseStatus.Filed)
                actions.Add($"Start investigation for {caseData.CaseNumber}");
            if (caseData.Status == CaseStatus.UnderInvestigation)
            {
                actions.Add($"Collect evidence for {caseData.CaseNumber}");
                actions.Add($"File chargesheet for {caseData.CaseNumber}");
            }
        }
        else if (role == UserRole.Lawyer)
        {
            if (caseData.AssignedLawyer == null)
                actions.Add($"Accept case {caseData.CaseNumber}");
            actions.Add($"Analyze case {caseData.CaseNumber}");
            actions.Add($"Prepare defense for {caseData.CaseNumber}");
        }

        actions.Add($"Show details for {caseData.CaseNumber}");
        return actions;
    }

    private string GetActionPromptForRole(UserRole role, string? action)
    {
        return role switch
        {
            UserRole.Police => $"🛡️ To {action ?? "take action"}, please specify the case number.\n\nExample: **'{action?.ToLower() ?? "register fir"} for CYB/2025/001'**",
            UserRole.Lawyer => $"⚖️ To {action ?? "take action"}, please specify the case number.\n\nExample: **'{action?.ToLower() ?? "accept"} case CYB/2025/001'**",
            _ => "Please specify the case number."
        };
    }

    private string GetRoleBasedHelp(UserRole role)
    {
        return role switch
        {
            UserRole.Police => "## 🛡️ Police Officer Commands\n\n" +
                              "**Case Management:**\n" +
                              "- **'show my cases'** - List cases assigned/pending\n" +
                              "- **'show pending cases'** - Cases needing action\n" +
                              "- **'show cybercrime cases'** - Cybercrime cases only\n\n" +
                              "**Take Action:**\n" +
                              "- **'register FIR for [case]'** - Register FIR\n" +
                              "- **'start investigation for [case]'** - Begin investigation\n" +
                              "- **'collect evidence for [case]'** - Log evidence\n" +
                              "- **'file chargesheet for [case]'** - Submit chargesheet\n\n" +
                              "**Analysis:**\n" +
                              "- **'analyze case [number]'** - Get AI investigation suggestions",

            UserRole.Lawyer => "## ⚖️ Lawyer Commands\n\n" +
                              "**Case Management:**\n" +
                              "- **'show available cases'** - Cases needing lawyer\n" +
                              "- **'show my cases'** - Your assigned cases\n\n" +
                              "**Take Action:**\n" +
                              "- **'accept case [number]'** - Take on a case\n" +
                              "- **'prepare defense for [case]'** - Get strategy\n" +
                              "- **'analyze case [number]'** - Legal analysis\n\n" +
                              "**Research:**\n" +
                              "- **'find precedents for [case]'** - Similar cases\n" +
                              "- **'explain [legal section]'** - Law explanation",

            _ => "## 👤 How I Can Help\n\n" +
                 "**File Complaints:**\n" +
                 "- **'file a complaint'** - Start guided filing\n" +
                 "- **'report cybercrime'** - Report online crime\n\n" +
                 "**Track Cases:**\n" +
                 "- **'show my cases'** - View filed cases\n" +
                 "- **'track progress'** - Case status\n\n" +
                 "**Get Help:**\n" +
                 "- **'what are my rights'** - Legal rights info\n" +
                 "- **'explain [topic]'** - Legal information"
        };
    }

    #endregion

    #region Azure Agent Prompt Builders

    /// <summary>
    /// Builds case data context as JSON for the Azure Agent
    /// </summary>
    private string BuildCaseDataContext(List<Case> cases, UserRole role)
    {
        var context = new
        {
            TotalCases = cases.Count,
            CybercrimeCount = cases.Count(c => c.IsCybercrime),
            StatusBreakdown = cases.GroupBy(c => c.Status)
                .Select(g => new { Status = g.Key.ToString(), Count = g.Count() }),
            Cases = cases.Take(15).Select(c => new
            {
                c.CaseNumber,
                c.Title,
                c.Type,
                Status = c.Status.ToString(),
                c.IsCybercrime,
                FiledDate = c.FiledDate.ToString("yyyy-MM-dd"),
                DaysSinceFiled = (DateTime.Now - c.FiledDate).Days,
                c.FIRNumber,
                c.InvestigatingOfficer,
                c.AssignedLawyer,
                c.PoliceStation,
                NextHearing = c.NextHearingDate?.ToString("yyyy-MM-dd"),
                c.SuccessProbability,
                Sections = c.Sections,
                Laws = c.ApplicableLaws
            })
        };

        return System.Text.Json.JsonSerializer.Serialize(context);
    }

    /// <summary>
    /// Builds the prompt for listing cases based on user role
    /// </summary>
    private string BuildListCasesPrompt(string userQuery, User user, List<Case> cases, DetectedIntent intent)
    {
        var roleContext = user.Role switch
        {
            UserRole.Police => $"You are an AI assistant helping Police Officer {user.Name} ({user.Rank ?? "Officer"}) at {user.PoliceStation ?? "Police Station"}.",
            UserRole.Lawyer => $"You are an AI assistant helping Advocate {user.Name} (Bar Council: {user.BarCouncilNumber ?? "N/A"}).",
            UserRole.Citizen => $"You are an AI assistant helping citizen {user.Name} view and track their filed complaints.",
            _ => $"You are an AI assistant helping {user.Name}."
        };

        var instruction = user.Role switch
        {
            UserRole.Police => @"Present the cases in a clear, actionable format for police work. Highlight:
- Cases awaiting FIR registration (urgent)
- Ongoing investigations
- Cases with evidence pending
- Priority based on crime type and financial loss
Suggest specific next actions for each case.",

            UserRole.Lawyer => @"Present cases in a legal professional format. Highlight:
- Cases needing lawyer assignment
- Upcoming hearing dates
- Legal sections involved
- Case strength indicators
Suggest legal actions for each case.",

            UserRole.Citizen => @"Present the citizen's cases in an easy-to-understand format. For each case show:
- Current status and what it means
- What's happening next
- Who is handling it (officer, lawyer)
- Timeline and progress
Use friendly, non-legal language. Be reassuring and helpful.",

            _ => "Present the cases clearly with relevant details."
        };

        var filterInfo = "";
        if (!string.IsNullOrEmpty(intent.StatusFilter))
            filterInfo += $"User filtered by status: {intent.StatusFilter}. ";
        if (!string.IsNullOrEmpty(intent.TypeFilter))
            filterInfo += $"User filtered by type: {intent.TypeFilter}. ";

        return $@"{roleContext}

User Query: ""{userQuery}""
{filterInfo}

{instruction}

The case data is provided in the context. Generate a well-formatted response with:
1. A brief summary header
2. Each case with key details
3. Clear status indicators (use emojis)
4. Suggested next actions at the end

If there are no cases, provide a helpful message appropriate for the user's role.";
    }

    /// <summary>
    /// Builds prompt for case summary/dashboard view
    /// </summary>
    private string BuildCaseSummaryPrompt(string userQuery, User user, List<Case> cases)
    {
        return $@"You are an AI assistant helping {user.Name} ({user.Role}) view a dashboard summary of their cases.

User Query: ""{userQuery}""

The case data JSON is provided in context. Generate a comprehensive dashboard view showing:
1. Overall statistics (total cases, by status, by type)
2. Cases requiring attention or action
3. Recent activity
4. Upcoming events (hearings, deadlines)
5. Helpful tips or suggestions

Use emojis and clear formatting. Make it easy to scan and understand at a glance.";
    }

    /// <summary>
    /// Builds prompt for case details
    /// </summary>
    private string BuildCaseDetailsPrompt(string userQuery, User user, Case caseData)
    {
        var caseJson = System.Text.Json.JsonSerializer.Serialize(new
        {
            caseData.CaseNumber,
            caseData.Title,
            caseData.Type,
            caseData.Description,
            Status = caseData.Status.ToString(),
            caseData.IsCybercrime,
            FiledDate = caseData.FiledDate.ToString("yyyy-MM-dd"),
            caseData.FIRNumber,
            caseData.InvestigatingOfficer,
            caseData.AssignedLawyer,
            caseData.PoliceStation,
            caseData.Court,
            NextHearing = caseData.NextHearingDate?.ToString("yyyy-MM-dd"),
            caseData.Complainant,
            caseData.Accused,
            caseData.SuccessProbability,
            caseData.DigitalEvidenceCollected,
            Sections = caseData.Sections,
            Laws = caseData.ApplicableLaws
        });

        var roleSpecificInstructions = user.Role switch
        {
            UserRole.Police => "Focus on investigation details, evidence status, accused info, and next investigative steps.",
            UserRole.Lawyer => "Focus on legal aspects, applicable sections, potential arguments, and case strength.",
            UserRole.Citizen => "Explain in simple terms: what's the current status, who's handling it, what happens next, and expected timeline.",
            _ => "Provide comprehensive case details."
        };

        return $@"You are an AI assistant helping {user.Name} ({user.Role}) view details of case {caseData.CaseNumber}.

User Query: ""{userQuery}""

Case Data: {caseJson}

{roleSpecificInstructions}

Generate a detailed, well-formatted response with all relevant information. Use sections and emojis for clarity.
Include:
1. Case overview
2. Current status and meaning
3. Key parties involved
4. Timeline of events
5. Next steps/what to expect";
    }

    /// <summary>
    /// Builds prompt for case progress/tracking
    /// </summary>
    private string BuildCaseProgressPrompt(string userQuery, User user, Case caseData, List<CaseWorkflowStep> workflow)
    {
        var workflowJson = System.Text.Json.JsonSerializer.Serialize(workflow.Select(w => new
        {
            w.Stage,
            w.Status,
            Date = w.Date?.ToString("yyyy-MM-dd") ?? "Pending",
            w.Actor,
            w.Notes
        }));

        return $@"You are an AI assistant helping {user.Name} ({user.Role}) track the progress of case {caseData.CaseNumber}.

User Query: ""{userQuery}""

Case: {caseData.Title}
Status: {caseData.Status}
Filed: {caseData.FiledDate:yyyy-MM-dd}

Workflow History: {workflowJson}

Generate a visual timeline/progress view showing:
1. Each completed stage with date and who did it
2. Current stage highlighted
3. Upcoming stages
4. Estimated timeline if possible
5. What the user should expect next

Use emojis (✅ for completed, 🔄 for current, ⬜ for pending) and clear formatting.
For citizens, explain each stage in simple terms.";
    }

    /// <summary>
    /// Builds prompt for case analysis (Police/Lawyer)
    /// </summary>
    private string BuildCaseAnalysisPrompt(string userQuery, User user, Case caseData)
    {
        var roleSpecificPrompt = user.Role switch
        {
            UserRole.Police => $@"As an AI investigative assistant, analyze this case for Police Officer {user.Name}.

Provide:
1. **Case Assessment**: Severity, priority, complexity
2. **Investigation Strategy**: Step-by-step investigation plan
3. **Evidence Collection**: What evidence to gather, how to preserve it
4. **Legal Framework**: Applicable IPC/IT Act sections with explanations
5. **Red Flags**: Potential challenges or issues
6. **Timeline**: Suggested investigation timeline
7. **Coordination**: Other agencies/departments to involve",

            UserRole.Lawyer => $@"As an AI legal assistant, analyze this case for Advocate {user.Name}.

Provide:
1. **Case Strength Assessment**: Strong points, weak points
2. **Legal Analysis**: Applicable sections, precedents
3. **Prosecution/Defense Strategy**: Key arguments
4. **Evidence Review**: Admissibility, gaps
5. **Procedural Checklist**: Important deadlines, filings
6. **Potential Outcomes**: Best/worst case scenarios
7. **Recommendations**: Strategic advice",

            _ => "Provide a general analysis of the case."
        };

        return $@"{roleSpecificPrompt}

Case Details:
- Case Number: {caseData.CaseNumber}
- Title: {caseData.Title}
- Type: {caseData.Type}
- Cybercrime: {(caseData.IsCybercrime ? "Yes" : "No")}
- Description: {caseData.Description}
- Accused: {caseData.Accused}
- Financial Loss: ₹{caseData.SuccessProbability:N0}
- Sections: {string.Join(", ", caseData.Sections)}
- Applicable Laws: {string.Join(", ", caseData.ApplicableLaws)}

User Query: ""{userQuery}""

Provide detailed, actionable analysis with clear sections and formatting.";
    }

    /// <summary>
    /// Builds prompt for legal advice
    /// </summary>
    private string BuildLegalAdvicePrompt(string userQuery, User user)
    {
        return $@"You are an AI Legal Assistant specializing in Indian law, particularly cybercrime and criminal law.

User: {user.Name} ({user.Role})
Query: ""{userQuery}""

Provide accurate, helpful legal information about:
- Relevant Indian laws (IPC, IT Act, CrPC, Constitution)
- Legal procedures
- Rights and remedies
- Practical guidance

IMPORTANT:
- Be accurate about legal sections and their provisions
- Clarify that this is general information, not legal advice
- Suggest consulting a lawyer for specific situations
- Use simple language for non-lawyers

Format response with clear sections and include relevant section numbers.";
    }

    #endregion

    #region Fallback Methods

    private string GetFallbackInvestigationSuggestions(Case caseData)
    {
        if (caseData.IsCybercrime)
        {
            return "**Investigation Priorities:**\n" +
                   "1. Preserve digital evidence immediately\n" +
                   "2. Request transaction records from banks\n" +
                   "3. Obtain IP logs from service providers\n" +
                   "4. Document all victim communications\n\n" +
                   $"**Applicable Sections:** IT Act 66C, 66D, IPC 420";
        }

        return "**Investigation Priorities:**\n" +
               "1. Record detailed victim statement\n" +
               "2. Identify and interview witnesses\n" +
               "3. Collect physical evidence\n" +
               "4. Document crime scene (if applicable)\n\n" +
               "**Note:** AI analysis unavailable. Using standard protocol.";
    }

    private string GetFallbackInvestigationPlan(Case caseData)
    {
        return $"### Investigation Plan - {caseData.CaseNumber}\n\n" +
               "**Phase 1 (Week 1):** Initial documentation and evidence preservation\n" +
               "**Phase 2 (Week 2-3):** Witness interviews and statement recording\n" +
               "**Phase 3 (Week 3-4):** Evidence analysis and correlation\n" +
               "**Phase 4 (Week 4+):** Report preparation and chargesheet drafting";
    }

    private string GetFallbackLawyerAnalysis(Case caseData)
    {
        return $"### Legal Analysis - {caseData.CaseNumber}\n\n" +
               $"**Case Type:** {caseData.Type}\n" +
               $"**Strength Assessment:** Requires detailed review\n\n" +
               $"**Key Considerations:**\n" +
               "1. Review all evidence for admissibility\n" +
               "2. Check for procedural compliance\n" +
               "3. Identify potential defenses\n" +
               "4. Research relevant precedents\n\n" +
               "*Note: Connect to AI for detailed analysis*";
    }

    private async Task<AgentCommandResponse> GetFallbackLegalAdviceAsync(string query)
    {
        return new AgentCommandResponse
        {
            Success = true,
            Message = "## ⚖️ Legal Information\n\n" +
                      "I can help you with:\n" +
                      "- Indian Penal Code (IPC) sections\n" +
                      "- IT Act provisions for cybercrime\n" +
                      "- Constitutional rights\n" +
                      "- Criminal procedure\n\n" +
                      "Please ask a specific question about Indian law, or say **'explain [section/topic]'**"
        };
    }

    #endregion
}

#region Models

public class CommandSession
{
    public string SessionId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public UserRole UserRole { get; set; }
    public string CurrentCommand { get; set; } = string.Empty;
    public Dictionary<string, object> Context { get; set; } = new();
    public DateTime StartedAt { get; set; }
}

public class DetectedIntent
{
    public IntentType Type { get; set; }
    public string? CaseNumber { get; set; }
    public int? CaseId { get; set; }
    public string? Action { get; set; }
    public string? StatusFilter { get; set; }
    public string? TypeFilter { get; set; }
}

public enum IntentType
{
    General,
    ListCases,
    CaseDetails,
    CaseSummary,
    TakeAction,
    AnalyzeCase,
    UpdateStatus,
    AssignCase,
    GenerateDocument,
    LegalAdvice,
    CaseProgress
}

public class AgentCommandResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool RequiresInput { get; set; }
    public string? InputPrompt { get; set; }
    public List<Case>? Cases { get; set; }
    public Case? RelatedCase { get; set; }
    public Case? UpdatedCase { get; set; }
    public bool CaseUpdated { get; set; }
    public List<string> Actions { get; set; } = new();
    public List<string> RelatedLaws { get; set; } = new();
}

#endregion

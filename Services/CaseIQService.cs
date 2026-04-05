using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AILegalAsst.Models;
using Microsoft.Extensions.Logging;

namespace AILegalAsst.Services
{
    /// <summary>
    /// CaseIQ - AI-powered investigation assistant service
    /// Based on MahaCrimeOS AI model - designed specifically for Police Investigation Officers
    /// Reference: Maharashtra Police AI for Cybercrime Investigation
    /// </summary>
    public class CaseIQService
    {
        private static List<InvestigationSession> _sessions = new();
        private static List<CopilotSuggestion> _suggestions = new();
        private static List<InvestigationAction> _actions = new();
        private static int _sessionIdCounter = 1;
        private static int _suggestionIdCounter = 1;
        private static int _actionIdCounter = 1;
        
        private readonly AzureAgentService _aiService;
        private readonly CaseService _caseService;
        private readonly ILogger<CaseIQService> _logger;
        
        // Allowed roles for CaseIQ (Police officers only - like MahaCrimeOS AI)
        private static readonly UserRole[] AllowedRoles = { UserRole.Police, UserRole.Admin };
        
        public CaseIQService(
            AzureAgentService aiService,
            CaseService caseService,
            ILogger<CaseIQService> logger)
        {
            _aiService = aiService;
            _caseService = caseService;
            _logger = logger;
        }
        
        /// <summary>
        /// Check if user has access to CaseIQ
        /// Only Police officers can use this feature (based on Indian law enforcement structure)
        /// </summary>
        public bool HasAccess(User user)
        {
            if (user == null) return false;
            return AllowedRoles.Contains(user.Role) && user.IsVerified;
        }
        
        /// <summary>
        /// Get access denial reason for unauthorized users
        /// </summary>
        public string GetAccessDenialReason(User user)
        {
            if (user == null)
                return "Please login to access CaseIQ.";
            
            if (!AllowedRoles.Contains(user.Role))
                return "CaseIQ is available only for verified Police Officers. This tool is designed for law enforcement investigation purposes as per Indian Cyber Crime Coordination Centre (I4C) guidelines.";
            
            if (!user.IsVerified)
                return "Your Police credentials are pending verification. Please wait for admin approval to access CaseIQ.";
            
            return string.Empty;
        }
        
        /// <summary>
        /// Start a new investigation session for a case
        /// </summary>
        public async Task<InvestigationSession> StartInvestigationSessionAsync(int caseId, int investigatorId)
        {
            try
            {
                // Check if active session already exists
                var existingSession = _sessions.FirstOrDefault(s => s.CaseId == caseId && s.Status == "Active");
                
                if (existingSession != null)
                {
                    _logger.LogInformation($"Active session already exists for case {caseId}");
                    return existingSession;
                }
                
                // Create new session
                var session = new InvestigationSession
                {
                    Id = _sessionIdCounter++,
                    CaseId = caseId,
                    InvestigatorId = investigatorId,
                    CurrentStage = "Filing",
                    Status = "Active",
                    CompletedActionsCount = 0,
                    TotalActionsCount = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                
                // Load case details
                session.Case = await _caseService.GetCaseByIdAsync(caseId);
                
                _sessions.Add(session);
                _logger.LogInformation($"Investigation session created for case {caseId} with session ID {session.Id}");
                return await Task.FromResult(session);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error starting investigation session: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Get active session for a case
        /// </summary>
        public async Task<InvestigationSession> GetActiveSessionAsync(int caseId)
        {
            try
            {
                var session = _sessions.FirstOrDefault(s => s.CaseId == caseId && s.Status == "Active");
                if (session != null)
                {
                    // Load case details
                    session.Case = await _caseService.GetCaseByIdAsync(caseId);
                    session.Suggestions = _suggestions.Where(s => s.InvestigationSessionId == session.Id).ToList();
                    session.Actions = _actions.Where(a => a.InvestigationSessionId == session.Id).ToList();
                }
                return await Task.FromResult(session);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting active session: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Get session by ID with full details
        /// </summary>
        public async Task<InvestigationSession> GetSessionAsync(int sessionId)
        {
            try
            {
                var session = _sessions.FirstOrDefault(s => s.Id == sessionId);
                if (session != null)
                {
                    session.Suggestions = _suggestions.Where(s => s.InvestigationSessionId == session.Id).ToList();
                    session.Actions = _actions.Where(a => a.InvestigationSessionId == session.Id).ToList();
                }
                return await Task.FromResult(session);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting session: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Generate AI suggestions for a session - Always uses AI Agent Service
        /// </summary>
        public async Task<List<CopilotSuggestion>> GenerateSuggestionsAsync(InvestigationSession session)
        {
            if (session == null)
            {
                _logger.LogWarning("Session is null for suggestion generation");
                return new List<CopilotSuggestion>();
            }
            
            try
            {
                // Always try AI first - this is the primary mode of operation
                var suggestions = await GetAISuggestionsFromAgentAsync(session);
                
                // Add to in-memory storage
                foreach (var suggestion in suggestions)
                {
                    suggestion.Id = _suggestionIdCounter++;
                    suggestion.InvestigationSessionId = session.Id;
                    _suggestions.Add(suggestion);
                }
                
                _logger.LogInformation($"Generated {suggestions.Count} AI suggestions for session {session.Id}");
                
                return suggestions;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error generating suggestions: {ex.Message}");
                // Fallback to default suggestions only if AI completely fails
                return await GetDefaultSuggestionsAsync(session);
            }
        }
        
        /// <summary>
        /// Get AI suggestions directly from Azure Agent Service
        /// This is the primary method - Investigation Copilot always works with AI
        /// </summary>
        private async Task<List<CopilotSuggestion>> GetAISuggestionsFromAgentAsync(InvestigationSession session)
        {
            try
            {
                // Get case details for context
                var caseEntity = await _caseService.GetCaseByIdAsync(session.CaseId);
                
                // Build comprehensive prompt for investigation suggestions
                var prompt = BuildInvestigationPrompt(caseEntity, session);
                
                _logger.LogInformation($"Sending investigation prompt to AI Agent for session {session.Id}");
                
                // Call Azure Agent Service directly
                var response = await _aiService.SendMessageAsync(prompt, GetInvestigationContext(session));
                
                if (response.Success && !response.IsFallback)
                {
                    _logger.LogInformation("AI Agent responded successfully for investigation suggestions");
                    var suggestions = ParseAISuggestionsFromResponse(response.Message, session);
                    
                    // If parsing returned valid suggestions, use them
                    if (suggestions.Count > 0)
                    {
                        return suggestions;
                    }
                }
                else
                {
                    _logger.LogWarning($"AI Agent response was fallback or failed: {response.Message}");
                }
                
                // If AI response couldn't be parsed, get stage-specific defaults
                return await GetStageSpecificSuggestionsAsync(session);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting AI suggestions from agent: {ex.Message}");
                return await GetStageSpecificSuggestionsAsync(session);
            }
        }
        
        /// <summary>
        /// Build investigation context for AI
        /// </summary>
        private string GetInvestigationContext(InvestigationSession session)
        {
            return $@"You are an Investigation Copilot assisting a Police Investigation Officer in India.
Role: AI-powered cybercrime investigation assistant (similar to MahaCrimeOS AI)
Current Investigation Stage: {session.CurrentStage}
Jurisdiction: Indian Criminal Law (BNS, BNSS, BSA, IT Act 2000)
User: Verified Police Officer";
        }
        
        /// <summary>
        /// Build comprehensive investigation prompt for AI Agent
        /// </summary>
        private string BuildInvestigationPrompt(Case caseEntity, InvestigationSession session)
        {
            var existingActions = _actions.Where(a => a.InvestigationSessionId == session.Id).ToList();
            var completedActions = existingActions.Where(a => a.Status == "Completed").Select(a => a.Description).ToList();
            
            return $@"You are an AI Investigation Copilot for Indian Police Officers investigating cybercrime cases.

## Case Details:
- Case Number: {caseEntity?.CaseNumber ?? "N/A"}
- Case Title: {caseEntity?.Title ?? "N/A"}
- Description: {caseEntity?.Description ?? "N/A"}
- Crime Type: {caseEntity?.CybercrimeCategory ?? (caseEntity?.Type.ToString() ?? "Cybercrime")}
- Current Stage: {session.CurrentStage}
- Status: {session.Status}

## Completed Actions:
{(completedActions.Any() ? string.Join("\n- ", completedActions) : "No actions completed yet")}

## Instructions:
Generate 5-7 specific, actionable investigation suggestions for this case. Each suggestion should follow Indian law enforcement procedures.

Format each suggestion as:
TYPE|PRIORITY|CONTENT|EXPLANATION|ESTIMATED_HOURS

Where:
- TYPE: One of [Evidence, QuestionForSuspect, DocumentRequired, LegalAdvice, NextStep, Warning, Precedent, DigitalForensics, BankFreeze, TelecomRequest]
- PRIORITY: 1-5 (5 being highest)
- CONTENT: Specific action description
- EXPLANATION: Why this is important
- ESTIMATED_HOURS: Time estimate in hours

Focus on:
1. Evidence collection (digital footprints, bank records, telecom data)
2. Legal notices to banks (account freeze under IT Act)
3. CDR (Call Detail Records) analysis
4. IP address tracing
5. Social media account takedown requests
6. Witness/victim statements
7. Compliance with BNSS procedures

Be specific and practical for Indian cybercrime investigation.";
        }
        
        /// <summary>
        /// Parse AI response into structured suggestions
        /// </summary>
        private List<CopilotSuggestion> ParseAISuggestionsFromResponse(string aiResponse, InvestigationSession session)
        {
            var suggestions = new List<CopilotSuggestion>();
            
            try
            {
                var lines = aiResponse.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                
                foreach (var line in lines)
                {
                    // Try to parse structured format: TYPE|PRIORITY|CONTENT|EXPLANATION|HOURS
                    var parts = line.Split('|');
                    if (parts.Length >= 4)
                    {
                        var suggestion = new CopilotSuggestion
                        {
                            Type = parts[0].Trim(),
                            Priority = int.TryParse(parts[1].Trim(), out var priority) ? priority : 3,
                            Content = parts[2].Trim(),
                            Explanation = parts[3].Trim(),
                            EstimatedEffort = parts.Length > 4 && double.TryParse(parts[4].Trim(), out var hours) ? hours : 2,
                            ConfidenceScore = 85,
                            CreatedAt = DateTime.UtcNow,
                            Source = "AzureAgent"
                        };
                        suggestions.Add(suggestion);
                    }
                    else if (line.Contains(":") && !line.StartsWith("#"))
                    {
                        // Try to parse semi-structured response
                        var colonIndex = line.IndexOf(':');
                        if (colonIndex > 0 && colonIndex < line.Length - 1)
                        {
                            var type = DetermineSuggestionType(line.Substring(0, colonIndex));
                            var content = line.Substring(colonIndex + 1).Trim();
                            
                            if (!string.IsNullOrWhiteSpace(content) && content.Length > 10)
                            {
                                suggestions.Add(new CopilotSuggestion
                                {
                                    Type = type,
                                    Content = content,
                                    Priority = suggestions.Count < 3 ? 5 : 3,
                                    ConfidenceScore = 80,
                                    Explanation = "AI-generated investigation recommendation",
                                    CreatedAt = DateTime.UtcNow,
                                    Source = "AzureAgent"
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error parsing AI suggestions: {ex.Message}");
            }
            
            return suggestions;
        }
        
        /// <summary>
        /// Get stage-specific suggestions when AI parsing fails
        /// </summary>
        private async Task<List<CopilotSuggestion>> GetStageSpecificSuggestionsAsync(InvestigationSession session)
        {
            var suggestions = session.CurrentStage switch
            {
                "Filing" => GetFilingStageSuggestions(),
                "EvidenceCollection" => GetEvidenceCollectionSuggestions(),
                "Investigation" => GetInvestigationStageSuggestions(),
                "Analysis" => GetAnalysisStageSuggestions(),
                "Chargesheet" => GetChargesheetStageSuggestions(),
                _ => GetDefaultSuggestionsAsync(session).Result
            };
            
            return await Task.FromResult(suggestions);
        }
        
        private List<CopilotSuggestion> GetFilingStageSuggestions()
        {
            return new List<CopilotSuggestion>
            {
                new CopilotSuggestion
                {
                    Type = "DocumentRequired",
                    Content = "Complete FIR registration with all victim details, transaction records, and communication screenshots as per BNSS Section 173.",
                    Priority = 5,
                    ConfidenceScore = 95,
                    Explanation = "FIR is mandatory first step for cognizable offenses under BNSS",
                    Source = "SystemDefault"
                },
                new CopilotSuggestion
                {
                    Type = "Evidence",
                    Content = "Collect victim's bank statements, UPI transaction IDs, and payment app screenshots for the fraud period.",
                    Priority = 5,
                    ConfidenceScore = 90,
                    Explanation = "Financial evidence is crucial for money trail in cybercrime",
                    Source = "SystemDefault"
                },
                new CopilotSuggestion
                {
                    Type = "TelecomRequest",
                    Content = "Request CDR (Call Detail Records) for suspect phone numbers through proper legal channels under IT Act Section 69.",
                    Priority = 4,
                    ConfidenceScore = 88,
                    Explanation = "CDR analysis helps establish communication pattern and location",
                    Source = "SystemDefault"
                }
            };
        }
        
        private List<CopilotSuggestion> GetEvidenceCollectionSuggestions()
        {
            return new List<CopilotSuggestion>
            {
                new CopilotSuggestion
                {
                    Type = "BankFreeze",
                    Content = "Issue account freeze notice to beneficiary banks under IT Act provisions. Use I4C 1930 portal for faster processing.",
                    Priority = 5,
                    ConfidenceScore = 92,
                    Explanation = "Quick account freeze is critical within golden hour of cybercrime",
                    Source = "SystemDefault"
                },
                new CopilotSuggestion
                {
                    Type = "DigitalForensics",
                    Content = "Preserve digital evidence using proper forensic tools. Maintain chain of custody as per Section 65B of Indian Evidence Act (now BSA).",
                    Priority = 5,
                    ConfidenceScore = 90,
                    Explanation = "Proper evidence preservation is mandatory for court admissibility",
                    Source = "SystemDefault"
                },
                new CopilotSuggestion
                {
                    Type = "NextStep",
                    Content = "Request IP logs from social media platforms and websites involved through legal notices.",
                    Priority = 4,
                    ConfidenceScore = 85,
                    Explanation = "IP tracing helps identify actual perpetrator location",
                    Source = "SystemDefault"
                }
            };
        }
        
        private List<CopilotSuggestion> GetInvestigationStageSuggestions()
        {
            return new List<CopilotSuggestion>
            {
                new CopilotSuggestion
                {
                    Type = "QuestionForSuspect",
                    Content = "Prepare interrogation questions focusing on: account access method, money withdrawal pattern, accomplices, and communication channels used.",
                    Priority = 5,
                    ConfidenceScore = 88,
                    Explanation = "Structured interrogation helps establish modus operandi",
                    Source = "SystemDefault"
                },
                new CopilotSuggestion
                {
                    Type = "Evidence",
                    Content = "Analyze CDR data to identify communication clusters, common contacts, and tower location patterns.",
                    Priority = 4,
                    ConfidenceScore = 85,
                    Explanation = "CDR analysis reveals suspect network and movement",
                    Source = "SystemDefault"
                },
                new CopilotSuggestion
                {
                    Type = "LegalAdvice",
                    Content = "Check if offense falls under BNS Section 318 (Cheating), Section 319 (Fraud), or IT Act Section 66C/66D for cybercrime.",
                    Priority = 4,
                    ConfidenceScore = 90,
                    Explanation = "Correct section identification is crucial for chargesheet",
                    Source = "SystemDefault"
                }
            };
        }
        
        private List<CopilotSuggestion> GetAnalysisStageSuggestions()
        {
            return new List<CopilotSuggestion>
            {
                new CopilotSuggestion
                {
                    Type = "NextStep",
                    Content = "Correlate all evidence: bank records, CDR data, IP logs, and witness statements to build timeline.",
                    Priority = 5,
                    ConfidenceScore = 90,
                    Explanation = "Evidence correlation strengthens prosecution case",
                    Source = "SystemDefault"
                },
                new CopilotSuggestion
                {
                    Type = "Precedent",
                    Content = "Review similar cybercrime case precedents from Supreme Court and High Courts for applicable judgments.",
                    Priority = 3,
                    ConfidenceScore = 75,
                    Explanation = "Precedents guide investigation approach and legal strategy",
                    Source = "SystemDefault"
                }
            };
        }
        
        private List<CopilotSuggestion> GetChargesheetStageSuggestions()
        {
            return new List<CopilotSuggestion>
            {
                new CopilotSuggestion
                {
                    Type = "DocumentRequired",
                    Content = "Prepare chargesheet with all documentary evidence, witness list, and Section 65B certificate for electronic evidence.",
                    Priority = 5,
                    ConfidenceScore = 95,
                    Explanation = "Complete chargesheet is mandatory for prosecution",
                    Source = "SystemDefault"
                },
                new CopilotSuggestion
                {
                    Type = "LegalAdvice",
                    Content = "Ensure chargesheet is filed within 60/90 days as per BNSS provisions to avoid default bail.",
                    Priority = 5,
                    ConfidenceScore = 95,
                    Explanation = "Timely filing prevents accused from getting statutory bail",
                    Source = "SystemDefault"
                }
            };
        }
        
        /// <summary>
        /// Determine suggestion type from content
        /// </summary>
        private string DetermineSuggestionType(string content)
        {
            var lower = content.ToLower();
            
            if (lower.Contains("freeze") || lower.Contains("bank") || lower.Contains("account"))
                return "BankFreeze";
            if (lower.Contains("cdr") || lower.Contains("telecom") || lower.Contains("call record"))
                return "TelecomRequest";
            if (lower.Contains("forensic") || lower.Contains("digital") || lower.Contains("ip"))
                return "DigitalForensics";
            if (lower.Contains("evidence") || lower.Contains("collect"))
                return "Evidence";
            if (lower.Contains("interview") || lower.Contains("question") || lower.Contains("suspect") || lower.Contains("interrogat"))
                return "QuestionForSuspect";
            if (lower.Contains("precedent") || lower.Contains("similar case") || lower.Contains("judgment"))
                return "Precedent";
            if (lower.Contains("legal") || lower.Contains("law") || lower.Contains("section") || lower.Contains("act"))
                return "LegalAdvice";
            if (lower.Contains("document") || lower.Contains("file") || lower.Contains("fir") || lower.Contains("chargesheet"))
                return "DocumentRequired";
            if (lower.Contains("warning") || lower.Contains("caution") || lower.Contains("urgent"))
                return "Warning";
            
            return "NextStep";
        }
        
        /// <summary>
        /// Get default suggestions when AI is unavailable (fallback only)
        /// </summary>
        private async Task<List<CopilotSuggestion>> GetDefaultSuggestionsAsync(InvestigationSession session)
        {
            var suggestions = new List<CopilotSuggestion>
            {
                new CopilotSuggestion
                {
                    Type = "Evidence",
                    Content = "Collect and document all digital evidence related to the cybercrime. Ensure proper chain of custody as per Section 65B of BSA (formerly Indian Evidence Act).",
                    Priority = 5,
                    ConfidenceScore = 85,
                    Explanation = "Critical for case prosecution under Indian law",
                    Source = "SystemDefault"
                },
                new CopilotSuggestion
                {
                    Type = "BankFreeze",
                    Content = "Issue immediate account freeze request to banks through I4C 1930 portal within golden hour.",
                    Priority = 5,
                    ConfidenceScore = 90,
                    Explanation = "Quick action prevents fund siphoning",
                    Source = "SystemDefault"
                },
                new CopilotSuggestion
                {
                    Type = "TelecomRequest",
                    Content = "Request CDR and subscriber details from telecom providers for all suspect phone numbers.",
                    Priority = 4,
                    ConfidenceScore = 82,
                    Explanation = "Establishes communication pattern and location trail",
                    Source = "SystemDefault"
                }
            };
            
            return await Task.FromResult(suggestions);
        }
        
        /// <summary>
        /// Ask AI Copilot a direct question about the investigation
        /// </summary>
        public async Task<string> AskCopilotAsync(int sessionId, string question)
        {
            try
            {
                var session = await GetSessionAsync(sessionId);
                if (session == null)
                {
                    return "Investigation session not found.";
                }
                
                var caseEntity = await _caseService.GetCaseByIdAsync(session.CaseId);
                
                var context = $@"You are an Investigation Copilot assisting Indian Police in cybercrime investigation.
Case: {caseEntity?.Title ?? "Unknown"} (#{caseEntity?.CaseNumber ?? "N/A"})
Stage: {session.CurrentStage}
Applicable Laws: BNS 2023, BNSS 2023, BSA 2023, IT Act 2000

Answer the investigation officer's question with specific, actionable guidance based on Indian law and I4C cybercrime investigation procedures.";

                var response = await _aiService.SendMessageAsync(question, context);
                
                if (response.Success && !response.IsFallback)
                {
                    return response.Message;
                }
                
                return "I apologize, but I couldn't process your question at this time. Please try again or rephrase your question.";
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in AskCopilot: {ex.Message}");
                return $"Error processing question: {ex.Message}";
            }
        }
        
        /// <summary>
        /// Get AI analysis of call detail records
        /// </summary>
        public async Task<string> AnalyzeCDRAsync(int sessionId, string cdrData)
        {
            try
            {
                var prompt = $@"Analyze the following CDR (Call Detail Records) data for cybercrime investigation:

{cdrData}

Provide:
1. Key patterns identified (frequent contacts, call timing patterns)
2. Suspicious numbers to investigate further
3. Tower location analysis for movement tracking
4. Recommended next steps for investigation";

                var response = await _aiService.SendMessageAsync(prompt, "CDR Analysis for Cybercrime Investigation - Indian Police");
                
                return response.Success ? response.Message : "Unable to analyze CDR data at this time.";
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error analyzing CDR: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }
        
        /// <summary>
        /// Generate legal notice draft for banks/telecom
        /// </summary>
        public async Task<string> GenerateLegalNoticeAsync(int sessionId, string noticeType, string recipientDetails)
        {
            try
            {
                var session = await GetSessionAsync(sessionId);
                var caseEntity = session != null ? await _caseService.GetCaseByIdAsync(session.CaseId) : null;
                
                var prompt = $@"Generate a formal legal notice for {noticeType} in an Indian cybercrime case.

Case Number: {caseEntity?.CaseNumber ?? "[CASE_NUMBER]"}
Recipient: {recipientDetails}
Notice Type: {noticeType}

Generate a proper legal notice format as per Indian law (IT Act 2000, BNSS 2023) that can be sent to {(noticeType.ToLower().Contains("bank") ? "banks for account freeze/transaction details" : "telecom providers for CDR/subscriber details")}.

Include:
- Proper legal authority citation
- Specific information requested
- Timeline for compliance
- Consequences of non-compliance";

                var response = await _aiService.SendMessageAsync(prompt, "Legal Notice Generation - Indian Cybercrime Investigation");
                
                return response.Success ? response.Message : "Unable to generate legal notice at this time.";
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error generating legal notice: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }
        
        /// <summary>
        /// Build prompt for AI suggestion generation (kept for compatibility)
        /// </summary>
        private string BuildSuggestionPrompt(Case caseEntity, InvestigationSession session)
        {
            return BuildInvestigationPrompt(caseEntity, session);
        }
        
        /// <summary>
        /// Accept a suggestion and create action item
        /// </summary>
        public async Task<InvestigationAction> AcceptSuggestionAsync(int suggestionId, int sessionId)
        {
            try
            {
                var suggestion = _suggestions.FirstOrDefault(s => s.Id == suggestionId);
                if (suggestion == null)
                {
                    _logger.LogWarning($"Suggestion {suggestionId} not found");
                    return null;
                }
                
                // Mark suggestion as accepted
                suggestion.IsAccepted = true;
                suggestion.ActionTakenAt = DateTime.UtcNow;
                suggestion.UpdatedAt = DateTime.UtcNow;
                
                // Create action item from suggestion
                var actionItem = new InvestigationAction
                {
                    Id = _actionIdCounter++,
                    InvestigationSessionId = sessionId,
                    Type = suggestion.Type,
                    Description = suggestion.Content,
                    Status = "Pending",
                    Priority = suggestion.Priority,
                    DueDate = DateTime.UtcNow.AddDays(suggestion.Priority == 5 ? 1 : 3),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    EstimatedHours = (decimal)suggestion.EstimatedEffort
                };
                
                _actions.Add(actionItem);
                suggestion.ActionItemId = actionItem.Id;
                
                _logger.LogInformation($"Suggestion {suggestionId} accepted and action item created");
                
                return await Task.FromResult(actionItem);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error accepting suggestion: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Reject a suggestion
        /// </summary>
        public async Task<bool> RejectSuggestionAsync(int suggestionId, string reason = null)
        {
            try
            {
                var suggestion = _suggestions.FirstOrDefault(s => s.Id == suggestionId);
                if (suggestion == null)
                {
                    return false;
                }
                
                suggestion.IsRejected = true;
                suggestion.RejectionReason = reason;
                suggestion.ActionTakenAt = DateTime.UtcNow;
                suggestion.UpdatedAt = DateTime.UtcNow;
                
                _logger.LogInformation($"Suggestion {suggestionId} rejected");
                
                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error rejecting suggestion: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Get pending actions for a session
        /// </summary>
        public async Task<List<InvestigationAction>> GetPendingActionsAsync(int sessionId)
        {
            try
            {
                var actions = _actions
                    .Where(a => a.InvestigationSessionId == sessionId && a.Status != "Completed")
                    .OrderByDescending(a => a.Priority)
                    .ThenBy(a => a.DueDate)
                    .ToList();
                
                return await Task.FromResult(actions);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting pending actions: {ex.Message}");
                return new List<InvestigationAction>();
            }
        }
        
        /// <summary>
        /// Get all actions for a session
        /// </summary>
        public async Task<List<InvestigationAction>> GetAllActionsAsync(int sessionId)
        {
            try
            {
                var actions = _actions
                    .Where(a => a.InvestigationSessionId == sessionId)
                    .OrderByDescending(a => a.Priority)
                    .ThenBy(a => a.DueDate)
                    .ToList();
                
                return await Task.FromResult(actions);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting all actions: {ex.Message}");
                return new List<InvestigationAction>();
            }
        }
        
        /// <summary>
        /// Update action status
        /// </summary>
        public async Task<bool> UpdateActionStatusAsync(int actionId, string status)
        {
            try
            {
                var action = _actions.FirstOrDefault(a => a.Id == actionId);
                if (action == null)
                {
                    return false;
                }
                
                action.Status = status;
                action.UpdatedAt = DateTime.UtcNow;
                
                if (status == "InProgress" && !action.StartedAt.HasValue)
                {
                    action.StartedAt = DateTime.UtcNow;
                }
                
                if (status == "Completed")
                {
                    action.CompletedAt = DateTime.UtcNow;
                }
                
                // Update session completion count
                var session = _sessions.FirstOrDefault(s => s.Id == action.InvestigationSessionId);
                if (session != null)
                {
                    var completedCount = _actions
                        .Where(a => a.InvestigationSessionId == session.Id && a.Status == "Completed")
                        .Count();
                    
                    session.CompletedActionsCount = completedCount;
                    session.UpdatedAt = DateTime.UtcNow;
                }
                
                _logger.LogInformation($"Action {actionId} status updated to {status}");
                
                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating action status: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Get accepted suggestions (not yet actioned)
        /// </summary>
        public async Task<List<CopilotSuggestion>> GetAcceptedSuggestionsAsync(int sessionId)
        {
            try
            {
                var suggestions = _suggestions
                    .Where(s => s.InvestigationSessionId == sessionId && s.IsAccepted && !s.IsRejected)
                    .OrderByDescending(s => s.Priority)
                    .ToList();
                
                return await Task.FromResult(suggestions);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting accepted suggestions: {ex.Message}");
                return new List<CopilotSuggestion>();
            }
        }
        
        /// <summary>
        /// Complete investigation session
        /// </summary>
        public async Task<bool> CompleteSessionAsync(int sessionId)
        {
            try
            {
                var session = _sessions.FirstOrDefault(s => s.Id == sessionId);
                if (session == null)
                {
                    return false;
                }
                
                session.Status = "Completed";
                session.CompletedAt = DateTime.UtcNow;
                session.UpdatedAt = DateTime.UtcNow;
                
                _logger.LogInformation($"Investigation session {sessionId} completed");
                
                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error completing session: {ex.Message}");
                return false;
            }
        }
    }
}

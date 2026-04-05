using AILegalAsst.Models;
using System.Text;
using System.Text.Json;

namespace AILegalAsst.Services;

/// <summary>
/// FIR Draft Service - AI-powered First Information Report generation
/// Matches MahaCrimeOS FIR auto-generation capability
/// Integrates with Azure AI Agent for enhanced legal drafting
/// </summary>
public class FIRDraftService
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly ILogger<FIRDraftService> _logger;
    private readonly AzureAgentService _azureAgentService;
    private readonly List<FIRDraft> _drafts = new(); // In-memory storage (replace with DB)
    private readonly List<SimilarCaseMatch> _caseDatabase = new(); // Simulated case database

    public FIRDraftService(
        IConfiguration configuration, 
        ILogger<FIRDraftService> logger,
        AzureAgentService azureAgentService)
    {
        _configuration = configuration;
        _logger = logger;
        _azureAgentService = azureAgentService;
        _httpClient = new HttpClient();
        InitializeSampleCases();
    }

    /// <summary>
    /// Generate AI-drafted FIR from user's incident description
    /// </summary>
    public async Task<FIRDraft> GenerateFIRDraftAsync(FIRDraft draft)
    {
        try
        {
            // Get applicable legal sections
            draft.ApplicableSections = GetApplicableSections(draft.CrimeType, draft.CrimeSubType);

            // Pre-initialize agent before parallel calls to avoid race condition
            await _azureAgentService.InitializeAsync();

            // Run all AI calls in parallel for faster response
            var aiPrompt = BuildFIRPrompt(draft);
            var analysisPrompt = BuildLegalAnalysisPrompt(draft);

            var draftTask = GenerateAIContentAsync(aiPrompt, draft.Language);
            var analysisTask = GenerateAIContentAsync(analysisPrompt, draft.Language);
            var actionsTask = GetRecommendedActionsAsync(draft.CrimeType);
            var jurisdictionTask = GetJurisdictionInfoAsync(draft.State, draft.District);

            await Task.WhenAll(draftTask, analysisTask, actionsTask, jurisdictionTask);

            draft.AIDraftedFIR = draftTask.Result;
            draft.LegalAnalysis = analysisTask.Result;
            draft.RecommendedActions = actionsTask.Result;
            draft.JurisdictionInfo = jurisdictionTask.Result;

            draft.Status = FIRStatus.Generated;
            draft.UpdatedAt = DateTime.UtcNow;

            // Save draft
            _drafts.Add(draft);

            return draft;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating FIR draft");
            throw;
        }
    }

    /// <summary>
    /// Get applicable legal sections based on crime type
    /// </summary>
    public List<string> GetApplicableSections(FIRCrimeType crimeType, FIRCrimeSubType subType)
    {
        var sections = new List<string>();

        if (LegalSections.ApplicableSections.TryGetValue(crimeType, out var legalSections))
        {
            sections.AddRange(legalSections.Select(s => $"{s.Code} - {s.Title}"));
        }

        // Add specific sections for sub-types
        switch (subType)
        {
            case FIRCrimeSubType.InvestmentScam:
                sections.Add("SEBI Act Sec 12A - Prohibition of fraudulent trade practices");
                sections.Add("Companies Act Sec 447 - Punishment for fraud");
                break;
            case FIRCrimeSubType.DigitalArrest:
                sections.Add("BNS Sec 351 - Criminal Intimidation");
                sections.Add("BNS Sec 308 - Extortion");
                sections.Add("IT Act Sec 66D - Cheating by personation");
                break;
            case FIRCrimeSubType.Sextortion:
                sections.Add("IT Act Sec 66E - Privacy Violation");
                sections.Add("IT Act Sec 67 - Obscene material");
                sections.Add("POCSO Act (if minor involved)");
                break;
            case FIRCrimeSubType.LoanAppHarassment:
                sections.Add("RBI Guidelines on Digital Lending");
                sections.Add("IT Act Sec 66E - Privacy Violation");
                sections.Add("BNS Sec 351 - Criminal Intimidation");
                break;
            case FIRCrimeSubType.UPIFraud:
                sections.Add("Payment and Settlement Systems Act 2007");
                sections.Add("RBI Circular on Customer Liability");
                break;
        }

        return sections.Distinct().ToList();
    }

    /// <summary>
    /// Find similar cases for case linking
    /// </summary>
    public async Task<List<SimilarCaseMatch>> FindSimilarCasesAsync(FIRDraft draft)
    {
        var matches = new List<SimilarCaseMatch>();

        // AI-based similarity matching
        foreach (var existingCase in _caseDatabase)
        {
            double score = 0;
            var matchingFactors = new List<string>();

            // Match by crime type
            if (existingCase.CrimeType == draft.CrimeType.ToString())
            {
                score += 0.3;
                matchingFactors.Add("Same crime type");
            }

            // Match by location
            if (!string.IsNullOrEmpty(draft.State) && existingCase.Location.Contains(draft.State, StringComparison.OrdinalIgnoreCase))
            {
                score += 0.1;
                matchingFactors.Add("Same state");
            }

            // Match by amount range (for financial crimes)
            if (draft.AmountLost.HasValue && existingCase.AmountInvolved.HasValue)
            {
                var ratio = (double)Math.Min(draft.AmountLost.Value, existingCase.AmountInvolved.Value) /
                            (double)Math.Max(draft.AmountLost.Value, existingCase.AmountInvolved.Value);
                if (ratio > 0.5)
                {
                    score += 0.15;
                    matchingFactors.Add("Similar amount range");
                }
            }

            // Match by time proximity (within 30 days)
            var daysDiff = Math.Abs((draft.IncidentDate - existingCase.IncidentDate).TotalDays);
            if (daysDiff <= 30)
            {
                score += 0.15;
                matchingFactors.Add("Recent time period");
            }

            // Match by phone number
            if (!string.IsNullOrEmpty(draft.AccusedPhone) && 
                existingCase.CaseTitle.Contains(draft.AccusedPhone))
            {
                score += 0.4;
                matchingFactors.Add("Same accused phone number");
            }

            // Match by bank account
            if (!string.IsNullOrEmpty(draft.AccountNumber) &&
                existingCase.CaseTitle.Contains(draft.AccountNumber))
            {
                score += 0.4;
                matchingFactors.Add("Same bank account");
            }

            if (score >= 0.3 && matchingFactors.Count > 0)
            {
                matches.Add(new SimilarCaseMatch
                {
                    CaseId = existingCase.CaseId,
                    CaseTitle = existingCase.CaseTitle,
                    CrimeType = existingCase.CrimeType,
                    Location = existingCase.Location,
                    IncidentDate = existingCase.IncidentDate,
                    AmountInvolved = existingCase.AmountInvolved,
                    SimilarityScore = Math.Min(score, 1.0),
                    MatchingFactors = matchingFactors,
                    Status = existingCase.Status,
                    Outcome = existingCase.Outcome
                });
            }
        }

        // Sort by similarity score
        return matches.OrderByDescending(m => m.SimilarityScore).Take(10).ToList();
    }

    /// <summary>
    /// Generate Bank Freeze Request Letter
    /// </summary>
    public async Task<BankFreezeRequest> GenerateBankFreezeLetterAsync(BankFreezeRequest request)
    {
        var letter = new StringBuilder();

        // Generate formal letter
        letter.AppendLine("To,");
        letter.AppendLine("The Branch Manager,");
        letter.AppendLine($"{request.AccusedBankName}");
        letter.AppendLine($"{request.BankManagerAddress}");
        letter.AppendLine();
        letter.AppendLine($"Date: {DateTime.Now:dd MMMM yyyy}");
        letter.AppendLine();
        letter.AppendLine("Subject: Urgent Request for Freezing Bank Account - Cyber Fraud Case");
        letter.AppendLine();
        letter.AppendLine("Respected Sir/Madam,");
        letter.AppendLine();
        letter.AppendLine($"I, {request.ComplainantName}, residing at {request.ComplainantAddress}, hereby report that I have been a victim of cyber fraud and request immediate freezing of the following bank account(s):");
        letter.AppendLine();
        letter.AppendLine("**Fraudulent Account Details:**");
        letter.AppendLine($"- Bank Name: {request.AccusedBankName}");
        letter.AppendLine($"- Account Number: {request.AccusedAccountNumber}");
        letter.AppendLine($"- IFSC Code: {request.AccusedIFSC}");
        if (!string.IsNullOrEmpty(request.AccusedAccountHolderName))
            letter.AppendLine($"- Account Holder: {request.AccusedAccountHolderName}");
        if (!string.IsNullOrEmpty(request.AccusedUPIId))
            letter.AppendLine($"- UPI ID: {request.AccusedUPIId}");
        letter.AppendLine();
        letter.AppendLine("**Fraud Details:**");
        letter.AppendLine($"- Date of Fraud: {request.FraudDate:dd MMMM yyyy}");
        letter.AppendLine($"- Amount Defrauded: ₹{request.AmountDefrauded:N2}");
        letter.AppendLine($"- Transaction Reference: {request.TransactionReference}");
        letter.AppendLine();
        letter.AppendLine("**Brief Description:**");
        letter.AppendLine(request.FraudDescription);
        letter.AppendLine();

        if (!string.IsNullOrEmpty(request.CyberCrimePortalNumber))
        {
            letter.AppendLine($"**Cyber Crime Portal Complaint Number:** {request.CyberCrimePortalNumber}");
        }
        if (!string.IsNullOrEmpty(request.FIRNumber))
        {
            letter.AppendLine($"**FIR Number:** {request.FIRNumber}");
            letter.AppendLine($"**Police Station:** {request.PoliceStation}");
        }
        letter.AppendLine();
        letter.AppendLine("I request you to kindly:");
        letter.AppendLine("1. Immediately freeze the above-mentioned account(s) to prevent further withdrawal of fraudulent funds");
        letter.AppendLine("2. Provide the account holder's details and KYC documents to the investigating authority");
        letter.AppendLine("3. Preserve all transaction records related to this account");
        letter.AppendLine("4. Initiate the process for recovery of my funds as per RBI guidelines");
        letter.AppendLine();
        letter.AppendLine("This request is made under the provisions of:");
        letter.AppendLine("- RBI Master Direction on Fraud Risk Management");
        letter.AppendLine("- Indian Cyber Crime Coordination Centre (I4C) Guidelines");
        letter.AppendLine("- Prevention of Money Laundering Act, 2002");
        letter.AppendLine();
        letter.AppendLine("I am attaching copies of my complaint, transaction proof, and identification documents for your reference.");
        letter.AppendLine();
        letter.AppendLine("Your prompt action in this matter is highly appreciated.");
        letter.AppendLine();
        letter.AppendLine("Thanking you,");
        letter.AppendLine();
        letter.AppendLine("Yours faithfully,");
        letter.AppendLine($"{request.ComplainantName}");
        letter.AppendLine($"Contact: {request.ComplainantPhone}");
        letter.AppendLine($"Email: {request.ComplainantEmail}");
        letter.AppendLine();
        letter.AppendLine("---");
        letter.AppendLine("Enclosures:");
        letter.AppendLine("1. Copy of Cyber Crime Portal Complaint");
        letter.AppendLine("2. Copy of FIR (if filed)");
        letter.AppendLine("3. Bank Statement showing fraudulent transaction");
        letter.AppendLine("4. ID Proof (Aadhaar/PAN)");
        letter.AppendLine("5. Screenshots of communication with fraudster (if any)");

        request.GeneratedLetter = letter.ToString();
        request.Status = BankFreezeStatus.Generated;

        return request;
    }

    /// <summary>
    /// Translate content to specified language
    /// </summary>
    public async Task<string> TranslateToLanguageAsync(string content, string targetLanguage)
    {
        if (targetLanguage == "en") return content;

        var prompt = $@"Translate the following legal document to {GetLanguageName(targetLanguage)}. 
Maintain legal terminology and formal tone. Keep the structure intact.

Content to translate:
{content}";

        return await GenerateAIContentAsync(prompt, targetLanguage);
    }

    /// <summary>
    /// Get user's FIR drafts
    /// </summary>
    public List<FIRDraft> GetUserDrafts(string userId)
    {
        return _drafts.Where(d => d.UserId == userId).OrderByDescending(d => d.CreatedAt).ToList();
    }

    /// <summary>
    /// Get draft by ID
    /// </summary>
    public FIRDraft? GetDraftById(string id)
    {
        return _drafts.FirstOrDefault(d => d.Id == id);
    }

    /// <summary>
    /// Delete draft
    /// </summary>
    public bool DeleteDraft(string id)
    {
        var draft = _drafts.FirstOrDefault(d => d.Id == id);
        if (draft != null)
        {
            _drafts.Remove(draft);
            return true;
        }
        return false;
    }

    #region Private Helper Methods

    private string BuildFIRPrompt(FIRDraft draft)
    {
        return $@"Generate a formal First Information Report (FIR) draft for Indian Police based on the following details.

IMPORTANT: Output PLAIN TEXT only. Do NOT use any markdown formatting like asterisks (**), hashtags (#), or other special characters for formatting. Use UPPERCASE for headings and dashes/equals for separators.

COMPLAINANT DETAILS:
Name: {draft.ComplainantName}
Father's/Husband's Name: {draft.FatherOrHusbandName}
Address: {draft.Address}, {draft.City}, {draft.State} - {draft.PinCode}
Contact: {draft.PhoneNumber}
Email: {draft.Email}

INCIDENT DETAILS:
Date & Time: {draft.IncidentDate:dd MMMM yyyy} at {draft.IncidentTime}
Location: {draft.IncidentLocation}
District: {draft.District}, {draft.State}

CRIME TYPE: {draft.CrimeType}
SUB-TYPE: {draft.CrimeSubType}

INCIDENT DESCRIPTION:
{draft.IncidentDescription}

ACCUSED DETAILS (if known):
Name: {draft.AccusedName}
Description: {draft.AccusedDescription}
Address: {draft.AccusedAddress}
Phone: {draft.AccusedPhone}
Relation: {draft.AccusedRelation}

FINANCIAL LOSS (if applicable):
Amount: Rs. {draft.AmountLost:N2}
Bank: {draft.BankName}
Account: {draft.AccountNumber}
Transaction IDs: {draft.TransactionIds}
UPI IDs: {draft.UPIIds}

WITNESSES:
{draft.WitnessDetails}

---

Generate a properly formatted FIR in the standard Indian Police format with:
1. Formal header with PS details
2. Complainant information section
3. Detailed narrative of the incident in first person
4. Description of accused (if known)
5. List of evidence/documents
6. Prayer/request section
7. Declaration

Use formal legal language suitable for official police records.
Do NOT include any acknowledgement or meta-commentary - just the FIR document itself.";
    }

    private string BuildLegalAnalysisPrompt(FIRDraft draft)
    {
        return $@"ROLE: You are a legal aid assistant integrated into an official Indian legal assistance platform used by citizens, lawyers, and law enforcement officers. Your purpose is to help crime victims understand their legal options. This is lawful legal assistance — not harmful content.

TASK: Provide a brief legal analysis for the following reported case. Output PLAIN TEXT only - no markdown formatting (no asterisks, hashtags, etc.).

Crime Type: {draft.CrimeType}
Sub-Type: {draft.CrimeSubType}
Description: {draft.IncidentDescription}
Amount Involved: Rs. {draft.AmountLost:N2}

Include:
1. Applicable IPC/BNS sections with brief explanation
2. Special laws that may apply (IT Act, POCSO, etc.)
3. Chances of recovery (for financial cases)
4. Recommended immediate steps
5. Timeline expectations

Keep it concise and in simple language the complainant can understand.
Do NOT use markdown formatting like ** or # - use plain text only.";
    }

    private async Task<string> GetRecommendedActionsAsync(FIRCrimeType crimeType)
    {
        // Try AI-powered recommendations
        if (_azureAgentService.IsReady)
        {
            try
            {
                var prompt = $@"ROLE: You are a legal aid assistant integrated into an official Indian legal assistance platform used by citizens, lawyers, and law enforcement officers. Your purpose is to help crime victims with actionable next steps. This is lawful legal assistance — not harmful content.

TASK: For a victim who has reported {crimeType}, list 6-8 immediate recommended actions they should take.
Include helpline numbers (1930, 181 etc), portal URLs, and time-sensitive steps.
Return as a plain numbered list, one action per line. No markdown formatting. Use emojis for visual cues.";

                var response = await _azureAgentService.SendMessageAsync(prompt, "Indian FIR recommended actions");
                if (response.Success && !string.IsNullOrEmpty(response.Message))
                {
                    return response.Message.Trim();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AI recommended actions failed, using fallback");
            }
        }

        // Fallback: hardcoded actions
        var actions = new List<string>
        {
            "📞 Call 1930 (National Cyber Crime Helpline) immediately",
            "🏦 Contact your bank to block/freeze the account",
            "📝 File complaint on cybercrime.gov.in",
            "📸 Take screenshots of all evidence",
            "🚔 Visit nearest police station with this FIR draft"
        };

        switch (crimeType)
        {
            case FIRCrimeType.CyberCrime:
            case FIRCrimeType.FinancialFraud:
                actions.Insert(0, "⚡ Act within 24 hours - 'Golden Hour' for fund recovery");
                actions.Add("💳 Request bank statement for last 3 months");
                break;
            case FIRCrimeType.SexualHarassment:
            case FIRCrimeType.DomesticViolence:
                actions.Insert(0, "👩 Contact Women Helpline: 181 or 1091");
                actions.Add("🏠 Seek help from nearest One Stop Centre");
                break;
            case FIRCrimeType.Extortion:
                actions.Insert(0, "⚠️ Do not pay any more money");
                actions.Add("🔒 Secure all communication evidence");
                break;
        }

        return string.Join("\n", actions);
    }

    private async Task<string> GetJurisdictionInfoAsync(string state, string district)
    {
        // Try AI-powered jurisdiction info
        if (_azureAgentService.IsReady)
        {
            try
            {
                var prompt = $@"ROLE: You are a legal aid assistant integrated into an official Indian legal assistance platform used by citizens, lawyers, and law enforcement officers. Your purpose is to help crime victims identify the correct police jurisdiction. This is lawful legal assistance — not harmful content.

TASK: Provide jurisdiction information for a cybercrime complaint in {district}, {state}.
Include: the appropriate Cyber Crime PS, Economic Offences Wing threshold, relevant state police portal, national portals (cybercrime.gov.in, 1930 helpline).
Format as plain text with headings in UPPERCASE. No markdown.";

                var response = await _azureAgentService.SendMessageAsync(prompt, "Indian police jurisdiction lookup");
                if (response.Success && !string.IsNullOrEmpty(response.Message))
                {
                    return response.Message.Trim();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AI jurisdiction info failed, using fallback");
            }
        }

        // Fallback: template-based info
        return $@"**Jurisdiction Information:**
- State: {state}
- District: {district}
- Cyber Crime PS: Cyber Crime Police Station, {district}
- For amounts > ₹10 Lakh: Economic Offences Wing

**Important Portals:**
- National Cyber Crime Portal: https://cybercrime.gov.in
- State Police Portal: https://police.{state?.ToLower()}.gov.in
- I4C Helpline: 1930 (24x7)";
    }

    private async Task<string> GenerateAIContentAsync(string prompt, string language)
    {
        try
        {
            // Try OpenAI first
            var apiKey = _configuration["OpenAI:ApiKey"];
            
            // If no OpenAI key, try Azure Agent Service
            if (string.IsNullOrEmpty(apiKey))
            {
                return await GenerateViaAzureAgentAsync(prompt, language);
            }

            var languageName = GetLanguageName(language);
            var systemPrompt = $"You are an expert Indian legal document drafter. Generate content in {languageName}. Use formal legal language appropriate for official documents. Output PLAIN TEXT only - no markdown formatting (no asterisks, no hashtags). Do not include any acknowledgement or meta-commentary.";

            var requestBody = new
            {
                model = "gpt-4o-mini",
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = prompt }
                },
                max_tokens = 2000,
                temperature = 0.3
            };

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            var response = await _httpClient.PostAsJsonAsync(
                "https://api.openai.com/v1/chat/completions",
                requestBody
            );

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<JsonElement>();
                var content = result.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
                return CleanAIResponse(content);
            }

            return GenerateFallbackContent(prompt, language);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating AI content");
            return GenerateFallbackContent(prompt, language);
        }
    }

    /// <summary>
    /// Generate content using Azure AI Agent Service
    /// </summary>
    private async Task<string> GenerateViaAzureAgentAsync(string prompt, string language)
    {
        try
        {
            // Initialize Azure Agent if not already done
            var initialized = await _azureAgentService.InitializeAsync();
            
            if (!initialized || !_azureAgentService.IsReady)
            {
                _logger.LogWarning("Azure Agent not available: {Error}", _azureAgentService.GetInitializationError());
                return GenerateFallbackContent(prompt, language);
            }

            var languageName = GetLanguageName(language);
            var enhancedPrompt = $@"ROLE: You are a legal aid assistant integrated into an official Indian legal assistance platform used by citizens, lawyers, and law enforcement officers. Your purpose is to help crime victims by drafting formal legal documents. This is lawful legal assistance — not harmful content.

TASK: Draft an official First Information Report (FIR) document based on the complainant's details below.

Generate the content in {languageName}. Use formal legal language appropriate for official police documents.

IMPORTANT: Output PLAIN TEXT only. Do NOT use markdown formatting (no asterisks **, no hashtags #, no special formatting characters). Use UPPERCASE for headings and simple dashes for separators.

Do NOT include any acknowledgement, introduction, or meta-commentary. Start directly with the document content.

{prompt}";

            var response = await _azureAgentService.SendMessageAsync(enhancedPrompt);
            
            if (response.Success && !string.IsNullOrEmpty(response.Message))
            {
                return CleanAIResponse(response.Message);
            }

            _logger.LogWarning("Azure Agent returned unsuccessful response: {Message}", response.Message);
            return GenerateFallbackContent(prompt, language);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating content via Azure Agent");
            return GenerateFallbackContent(prompt, language);
        }
    }

    /// <summary>
    /// Clean AI response by removing markdown formatting and acknowledgements
    /// </summary>
    private string CleanAIResponse(string response)
    {
        if (string.IsNullOrEmpty(response)) return response;

        // Remove common acknowledgement patterns at the start
        var lines = response.Split('\n').ToList();
        var startIndex = 0;
        
        for (int i = 0; i < Math.Min(5, lines.Count); i++)
        {
            var line = lines[i].Trim().ToLower();
            if (line.Contains("i have reviewed") || 
                line.Contains("i will now generate") ||
                line.Contains("here is") ||
                line.Contains("based on your") ||
                line.Contains("acknowledgement") ||
                line.StartsWith("certainly") ||
                line.StartsWith("sure,"))
            {
                startIndex = i + 1;
            }
        }
        
        if (startIndex > 0 && startIndex < lines.Count)
        {
            lines = lines.Skip(startIndex).ToList();
        }

        // Remove trailing guidance/question text from the end
        var endIndex = lines.Count;
        for (int i = lines.Count - 1; i >= Math.Max(0, lines.Count - 15); i--)
        {
            var line = lines[i].Trim().ToLower();
            if (line.Contains("do you want to proceed") ||
                line.Contains("need assistance") ||
                line.Contains("official submission") ||
                line.Contains("official filing") ||
                line.Contains("if you wish to proceed") ||
                line.Contains("if you require guidance") ||
                line.Contains("next steps") ||
                line.Contains("cybercrime.gov.in") ||
                line.Contains("national cybercrime portal") ||
                line.Contains("further documents") ||
                line.Contains("please provide") ||
                line.Contains("let me know") ||
                line.Contains("feel free to") ||
                line.Contains("i can guide you") ||
                line.Contains("i can assist") ||
                line.StartsWith("---") ||
                line == "---")
            {
                endIndex = i;
            }
        }
        
        if (endIndex < lines.Count)
        {
            lines = lines.Take(endIndex).ToList();
        }

        var cleaned = string.Join("\n", lines);

        // Remove markdown bold markers
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\*\*([^*]+)\*\*", "$1");
        
        // Remove markdown italic markers
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\*([^*]+)\*", "$1");
        
        // Remove markdown headers
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"^#{1,6}\s*", "", System.Text.RegularExpressions.RegexOptions.Multiline);
        
        // Remove horizontal rule separators (---, ___, ***)
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"^[\-_\*]{3,}\s*$", "", System.Text.RegularExpressions.RegexOptions.Multiline);
        
        // Clean up multiple blank lines
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\n{3,}", "\n\n");

        return cleaned.Trim();
    }

    private string GenerateFallbackContent(string prompt, string language)
    {
        // Generate a basic FIR template when AI is unavailable
        // This provides users with a usable document structure
        var sb = new StringBuilder();
        
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine("                    FIRST INFORMATION REPORT");
        sb.AppendLine("              (Under Section 154 Cr.P.C. / Section 173 BNSS)");
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine();
        sb.AppendLine("To,");
        sb.AppendLine("The Station House Officer,");
        sb.AppendLine("_____________________ Police Station");
        sb.AppendLine("District: _____________________");
        sb.AppendLine();
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine("                         COMPLAINT");
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine();
        sb.AppendLine("Respected Sir/Madam,");
        sb.AppendLine();
        sb.AppendLine("I, the undersigned complainant, most respectfully submit this");
        sb.AppendLine("complaint for the registration of a First Information Report");
        sb.AppendLine("(FIR) and request appropriate legal action.");
        sb.AppendLine();
        sb.AppendLine("The facts of the case are stated hereinbelow:");
        sb.AppendLine();
        sb.AppendLine("[Your detailed incident description will appear here when AI");
        sb.AppendLine(" services are configured. Please fill in the details manually.]");
        sb.AppendLine();
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine("                          PRAYER");
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine();
        sb.AppendLine("In view of the above facts, I humbly request you to:");
        sb.AppendLine();
        sb.AppendLine("1. Register an FIR based on the facts stated above");
        sb.AppendLine("2. Investigate the matter thoroughly");
        sb.AppendLine("3. Take appropriate legal action against the accused");
        sb.AppendLine("4. Recover my loss/property (if applicable)");
        sb.AppendLine("5. Provide justice");
        sb.AppendLine();
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine("                       DECLARATION");
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine();
        sb.AppendLine("I hereby declare that the information provided above is true");
        sb.AppendLine("and correct to the best of my knowledge and belief. I am aware");
        sb.AppendLine("that providing false information is a punishable offence under");
        sb.AppendLine("Indian law.");
        sb.AppendLine();
        sb.AppendLine();
        sb.AppendLine("_________________________              _________________________");
        sb.AppendLine("Complainant's Signature                Date & Place");
        sb.AppendLine();
        sb.AppendLine();
        sb.AppendLine("───────────────────────────────────────────────────────────────");
        sb.AppendLine("⚠️  NOTE: To enable AI-powered FIR generation, please configure:");
        sb.AppendLine("    • OpenAI API key in appsettings.json, OR");
        sb.AppendLine("    • Azure AI Agent in AzureAgent settings");
        sb.AppendLine("───────────────────────────────────────────────────────────────");
        
        return sb.ToString();
    }

    private string GetLanguageName(string code)
    {
        return LanguageSupport.SupportedLanguages.TryGetValue(code, out var lang) ? lang.Name : "English";
    }

    private void InitializeSampleCases()
    {
        // Sample case database for demonstration
        _caseDatabase.AddRange(new[]
        {
            new SimilarCaseMatch
            {
                CaseId = "NCRC-2024-001234",
                CaseTitle = "Investment Trading App Scam - Multiple victims",
                CrimeType = "CyberCrime",
                Location = "Maharashtra, Mumbai",
                IncidentDate = DateTime.Now.AddDays(-15),
                AmountInvolved = 850000,
                Status = "Under Investigation",
                Outcome = "2 accounts frozen, ₹3.2L recovered"
            },
            new SimilarCaseMatch
            {
                CaseId = "NCRC-2024-001456",
                CaseTitle = "Digital Arrest Scam - CBI/Police impersonation",
                CrimeType = "CyberCrime",
                Location = "Maharashtra, Nagpur",
                IncidentDate = DateTime.Now.AddDays(-7),
                AmountInvolved = 2500000,
                Status = "Under Investigation",
                Outcome = "Chargesheet filed against 4 accused"
            },
            new SimilarCaseMatch
            {
                CaseId = "NCRC-2024-001789",
                CaseTitle = "Job Fraud - Fake IT Company recruitment",
                CrimeType = "CyberCrime",
                Location = "Karnataka, Bangalore",
                IncidentDate = DateTime.Now.AddDays(-30),
                AmountInvolved = 150000,
                Status = "Resolved",
                Outcome = "Full amount recovered, accused arrested"
            },
            new SimilarCaseMatch
            {
                CaseId = "NCRC-2024-002001",
                CaseTitle = "Loan App Harassment - Illegal lending apps",
                CrimeType = "CyberCrime",
                Location = "Tamil Nadu, Chennai",
                IncidentDate = DateTime.Now.AddDays(-20),
                AmountInvolved = 45000,
                Status = "Under Investigation",
                Outcome = "App removed from Play Store"
            },
            new SimilarCaseMatch
            {
                CaseId = "NCRC-2024-002345",
                CaseTitle = "UPI Fraud - Screen sharing scam",
                CrimeType = "CyberCrime",
                Location = "Delhi",
                IncidentDate = DateTime.Now.AddDays(-5),
                AmountInvolved = 180000,
                Status = "Under Investigation",
                Outcome = "Pending"
            }
        });
    }

    #endregion
}

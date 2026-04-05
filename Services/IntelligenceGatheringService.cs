using AILegalAsst.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AILegalAsst.Services;

/// <summary>
/// Master Intelligence Gathering Service
/// 
/// Orchestrates intelligence collection from all data sources:
/// - Telecom (CDR, Towers, SMS)
/// - Banking (Accounts, Transactions, UPI)
/// - OSINT (Social Media, IP addresses)
/// - Police (Criminal records, CCTNS)
/// 
/// Fuses data into unified intelligence profiles with timeline,
/// network analysis, and actionable intelligence.
/// </summary>
public class IntelligenceGatheringService
{
    private readonly DataSourceIntegrationService _dataSourceService;
    private readonly ILogger<IntelligenceGatheringService> _logger;
    private readonly IConfiguration _config;
    private readonly AzureAgentService _agentService;

    public IntelligenceGatheringService(
        DataSourceIntegrationService dataSourceService,
        ILogger<IntelligenceGatheringService> logger,
        IConfiguration config,
        AzureAgentService agentService)
    {
        _dataSourceService = dataSourceService;
        _logger = logger;
        _config = config;
        _agentService = agentService;
    }

    /// <summary>
    /// Main method: Gather complete intelligence on a phone number
    /// 
    /// Process:
    /// 1. Collect from all 4 data sources (parallel)
    /// 2. Fuse data from all sources
    /// 3. Build communication timeline
    /// 4. Create suspect network
    /// 5. Generate intelligence assessment
    /// 6. Provide recommendations
    /// 
    /// Timeline: 2-4 hours if all data available
    /// </summary>
    public async Task<Result<IntelligenceRecord>> GatherPhoneIntelligenceAsync(
        string phoneNumber,
        string? firNumber,
        string requestingOfficerId,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        try
        {
            _logger.LogInformation(
                "Starting intelligence gathering for phone: {Phone}, FIR: {FIR}",
                phoneNumber, firNumber ?? "Unknown");

            // Validate inputs
            if (string.IsNullOrEmpty(phoneNumber))
                return Result<IntelligenceRecord>.Failure("Phone number is required");

            // Default date range: last 90 days
            startDate ??= DateTime.UtcNow.AddDays(-90);
            endDate ??= DateTime.UtcNow;

            // Create base record
            var record = new IntelligenceRecord
            {
                PhoneNumber = phoneNumber,
                CreatedDate = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow,
                AssignedOfficerId = int.TryParse(requestingOfficerId, out int id) ? id : null
            };

            // PARALLEL: Collect from all 4 data sources
            _logger.LogInformation("Collecting data from all sources in parallel...");

            var telecomTask = CollectTelecomDataAsync(
                phoneNumber, firNumber, startDate.Value, endDate.Value, requestingOfficerId);

            var bankingTask = CollectBankingDataAsync(
                phoneNumber, firNumber, startDate.Value, endDate.Value, requestingOfficerId);

            var osintTask = CollectOSINTDataAsync(phoneNumber);

            var policeTask = CollectPoliceDataAsync(phoneNumber, requestingOfficerId);

            // Wait for all to complete
            await Task.WhenAll(telecomTask, bankingTask, osintTask, policeTask);
            var telecom = await telecomTask;
            var banking = await bankingTask;
            var osint = await osintTask;
            var police = await policeTask;

            // Store collected data
            record.TelecomData = telecom;
            record.BankingData = banking;
            record.OSINTData = osint;
            record.PoliceData = police;

            _logger.LogInformation("Data collection complete. Starting fusion and analysis...");

            // Step 2: Fuse data and build timeline
            await FuseDataAndBuildTimelineAsync(record);

            // Step 3: Link to cases
            await LinkToCasesAsync(record, firNumber);

            // Step 4: Identify linked suspects/phones
            await IdentifyLinkedSuspectsAsync(record);

            // Step 5: Generate assessment
            await GenerateAssessmentAsync(record);

            record.LastUpdated = DateTime.UtcNow;

            _logger.LogInformation(
                "Intelligence gathering complete for {Phone}. Timeline has {EventCount} events",
                phoneNumber, record.Timeline.Count);

            return Result<IntelligenceRecord>.Success(record);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error gathering intelligence for {Phone}", phoneNumber);
            return Result<IntelligenceRecord>.Failure($"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Collect telephony data (CDR, towers, SMS)
    /// </summary>
    private async Task<TelecomIntelligence?> CollectTelecomDataAsync(
        string phoneNumber,
        string? firNumber,
        DateTime startDate,
        DateTime endDate,
        string requestingOfficerId)
    {
        try
        {
            _logger.LogInformation("Collecting telecom data...");

            if (string.IsNullOrEmpty(firNumber))
            {
                _logger.LogWarning("FIR number required for telecom data access");
                return null;
            }

            var result = await _dataSourceService.GetTelecomCDRDataAsync(
                phoneNumber,
                firNumber,
                startDate,
                endDate,
                requestingOfficerId);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Failed to get telecom data: {Error}", result.ErrorMessage);
                return null;
            }

            _logger.LogInformation(
                "Telecom data collected: {CallCount} calls, {SMSCount} SMS",
                result.Data?.TotalCalls ?? 0,
                result.Data?.TotalSMSs ?? 0);

            return result.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting telecom data");
            return null;
        }
    }

    /// <summary>
    /// Collect banking data (accounts, transactions)
    /// </summary>
    private async Task<BankingIntelligence?> CollectBankingDataAsync(
        string phoneNumber,
        string? firNumber,
        DateTime startDate,
        DateTime endDate,
        string requestingOfficerId)
    {
        try
        {
            _logger.LogInformation("Collecting banking data...");

            // In real scenario: need to find account number linked to phone
            // For now, placeholder
            string? accountNumber = await FindBankAccountForPhoneAsync(phoneNumber);

            if (string.IsNullOrEmpty(accountNumber))
            {
                _logger.LogInformation("No bank account linked to {Phone}", phoneNumber);
                return null;
            }

            if (string.IsNullOrEmpty(firNumber))
            {
                _logger.LogWarning("Court order required for banking data access");
                return null;
            }

            var result = await _dataSourceService.GetBankingDataAsync(
                phoneNumber,
                accountNumber,
                firNumber,
                startDate,
                endDate,
                requestingOfficerId);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Failed to get banking data: {Error}", result.ErrorMessage);
                return null;
            }

            _logger.LogInformation(
                "Banking data collected: {TxCount} transactions",
                result.Data?.Transactions.Count ?? 0);

            return result.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting banking data");
            return null;
        }
    }

    /// <summary>
    /// Collect OSINT (social media, IP addresses)
    /// No special permission needed for public data
    /// </summary>
    private async Task<OSINTIntelligence?> CollectOSINTDataAsync(string phoneNumber)
    {
        try
        {
            _logger.LogInformation("Collecting OSINT data...");

            var result = await _dataSourceService.GetOSINTDataAsync(phoneNumber);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Failed to get OSINT data: {Error}", result.ErrorMessage);
                return null;
            }

            _logger.LogInformation(
                "OSINT data collected: {ProfileCount} profiles",
                result.Data?.Profiles.Count ?? 0);

            return result.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting OSINT data");
            return null;
        }
    }

    /// <summary>
    /// Collect police database data (criminal records, CCTNS)
    /// </summary>
    private async Task<PoliceIntelligence?> CollectPoliceDataAsync(
        string phoneNumber,
        string requestingOfficerId)
    {
        try
        {
            _logger.LogInformation("Collecting police database data...");

            var result = await _dataSourceService.GetCCTNSDataAsync(
                phoneNumber,
                requestingOfficerId);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Failed to get CCTNS data: {Error}", result.ErrorMessage);
                return null;
            }

            _logger.LogInformation(
                "Police data collected: {CaseCount} cases, {ArrestCount} arrests",
                result.Data?.RelatedCases.Count ?? 0,
                result.Data?.TotalArrests ?? 0);

            return result.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting police data");
            return null;
        }
    }

    /// <summary>
    /// Fuse data from all sources into unified timeline
    /// Events: Calls, SMS, Transactions, Messages, Arrests, Posts
    /// </summary>
    private async Task FuseDataAndBuildTimelineAsync(IntelligenceRecord record)
    {
        try
        {
            _logger.LogInformation("Fusing data and building timeline...");

            var timelineEvents = new List<IntelligenceTimelineEvent>();

            // Extract from CDR
            if (record.TelecomData?.CallRecords != null)
            {
                foreach (var call in record.TelecomData.CallRecords)
                {
                    timelineEvents.Add(new IntelligenceTimelineEvent
                    {
                        EventDateTime = call.CallDateTime,
                        EventType = "Call",
                        EventDescription = $"Call to {call.CalleeNumber} ({call.DurationSeconds}s)",
                        DataSource = "Telecom",
                        RelatedPhoneOrAccount = call.CalleeNumber,
                        Location = call.TowerLocation
                    });
                }
            }

            // Extract from Banking
            if (record.BankingData?.Transactions != null)
            {
                foreach (var tx in record.BankingData.Transactions)
                {
                    timelineEvents.Add(new IntelligenceTimelineEvent
                    {
                        EventDateTime = tx.TransactionDateTime,
                        EventType = "Transaction",
                        EventDescription = $"{tx.TransactionType} ₹{tx.Amount} to {tx.ReceiverName}",
                        DataSource = "Banking",
                        RelatedPhoneOrAccount = tx.ReceiverAccountNumber
                    });
                }
            }

            // Extract from OSINT
            if (record.OSINTData?.Posts != null)
            {
                foreach (var post in record.OSINTData.Posts)
                {
                    timelineEvents.Add(new IntelligenceTimelineEvent
                    {
                        EventDateTime = post.PostDateTime,
                        EventType = "SocialMedia",
                        EventDescription = $"Posted on {post.Platform}",
                        DataSource = "OSINT",
                        Location = post.PostLocation
                    });
                }
            }

            // Extract from Police
            if (record.PoliceData?.PreviousArrests != null)
            {
                foreach (var arrest in record.PoliceData.PreviousArrests)
                {
                    timelineEvents.Add(new IntelligenceTimelineEvent
                    {
                        EventDateTime = arrest.ArrestDate,
                        EventType = "Arrest",
                        EventDescription = $"Arrested at {arrest.PoliceStation}",
                        DataSource = "Police",
                        RelatedPhoneOrAccount = arrest.CaseNumber
                    });
                }
            }

            // Sort by date
            record.Timeline = timelineEvents.OrderBy(e => e.EventDateTime).ToList();

            _logger.LogInformation("Timeline built with {EventCount} events", record.Timeline.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fusing data and building timeline");
        }
    }

    /// <summary>
    /// Link intelligence to related cases
    /// </summary>
    private async Task LinkToCasesAsync(IntelligenceRecord record, string? firNumber)
    {
        try
        {
            if (string.IsNullOrEmpty(firNumber))
                return;

            _logger.LogInformation("Linking to cases...");

            // Add FIR number to related cases
            if (!record.RelatedCaseIds.Contains(int.TryParse(
                firNumber.Split('/').LastOrDefault(), out int caseId) ? caseId : 0))
            {
                // Placeholder: In real scenario, query case database
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error linking to cases");
        }
    }

    /// <summary>
    /// Identify linked suspects and phones through communication patterns
    /// </summary>
    private async Task IdentifyLinkedSuspectsAsync(IntelligenceRecord record)
    {
        try
        {
            _logger.LogInformation("Identifying linked suspects...");

            // Extract all phone numbers contacted
            var contactedPhones = new HashSet<string>();

            if (record.TelecomData?.CallRecords != null)
            {
                foreach (var call in record.TelecomData.CallRecords)
                {
                    if (!string.IsNullOrEmpty(call.CalleeNumber))
                        contactedPhones.Add(call.CalleeNumber);
                }
            }

            // Add frequency
            if (record.TelecomData?.CallFrequency != null)
            {
                foreach (var phone in record.TelecomData.CallFrequency.Keys)
                {
                    if (record.TelecomData.CallFrequency[phone] > 10) // High frequency
                    {
                        // Flag for investigation
                        _logger.LogInformation(
                            "High frequency contact detected: {Phone} ({Count} calls)",
                            phone, record.TelecomData.CallFrequency[phone]);
                    }
                }
            }

            // Extract from banking
            if (record.BankingData?.FrequentRecipients != null)
            {
                // Already identified in banking intelligence
                _logger.LogInformation(
                    "Frequent financial recipients: {Count}",
                    record.BankingData.FrequentRecipients.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error identifying linked suspects");
        }
    }

    /// <summary>
    /// Generate comprehensive assessment and recommendations
    /// </summary>
    private async Task GenerateAssessmentAsync(IntelligenceRecord record)
    {
        try
        {
            _logger.LogInformation("Generating intelligence assessment...");

            var assessment = new IntelligenceAssessment
            {
                AssessmentDate = DateTime.UtcNow,
                InvestigationRecommendations = new List<string>(),
                ActionItems = new List<string>()
            };

            // Build profile summary
            var summary = new System.Text.StringBuilder();
            summary.AppendLine($"Phone: {record.PhoneNumber}");

            if (record.TelecomData != null)
            {
                summary.AppendLine($"- {record.TelecomData.TotalCalls} calls in period");
                summary.AppendLine($"- {record.TelecomData.TotalSMSs} SMS in period");
                if (record.TelecomData.FrequentContacts?.Count > 0)
                {
                    summary.AppendLine($"- {record.TelecomData.FrequentContacts.Count} frequent contacts");
                }
            }

            if (record.BankingData != null)
            {
                summary.AppendLine($"- ₹{record.BankingData.TotalIncomeLast30Days} income (30 days)");
                summary.AppendLine($"- ₹{record.BankingData.TotalExpenseLast30Days} expenses (30 days)");
                if (record.BankingData.SuspiciousTransactionCount > 0)
                {
                    summary.AppendLine($"- {record.BankingData.SuspiciousTransactionCount} suspicious transactions flagged");
                    assessment.RiskScore += 20;
                }
            }

            if (record.OSINTData?.Profiles?.Count > 0)
            {
                summary.AppendLine($"- {record.OSINTData.Profiles.Count} social media profiles found");
                if (record.OSINTData.NumberOfFakeProfiles > 0)
                {
                    summary.AppendLine($"- {record.OSINTData.NumberOfFakeProfiles} potential fake profiles");
                    assessment.RiskScore += 15;
                }
            }

            if (record.PoliceData != null)
            {
                if (record.PoliceData.TotalArrests > 0)
                {
                    summary.AppendLine($"- {record.PoliceData.TotalArrests} previous arrests");
                    assessment.RiskScore += 25;
                }
                if (record.PoliceData.IsWanted)
                {
                    summary.AppendLine($"- ⚠️ WANTED: Yes");
                    assessment.RiskScore += 30;
                }
            }

            assessment.Summary = summary.ToString();

            // Try AI-powered assessment
            if (_agentService.IsReady)
            {
                try
                {
                    var prompt = $"Generate an intelligence assessment for Indian law enforcement based on this data:\n{assessment.Summary}\n" +
                        $"Current Risk Score: {assessment.RiskScore}/100\n\n" +
                        "Provide:\n1. Updated risk assessment narrative\n" +
                        "2. Investigation recommendations (numbered list)\n" +
                        "3. Immediate action items (numbered list)\n" +
                        "Separate sections with ---SECTION---";
                    var context = "You are an Indian police intelligence analyst. Provide actionable investigation recommendations under Indian law (IT Act, IPC/BNS, BNSS).";

                    var response = await _agentService.SendMessageAsync(prompt, context);
                    if (response.Success && !string.IsNullOrWhiteSpace(response.Message))
                    {
                        var sections = response.Message.Split("---SECTION---", StringSplitOptions.RemoveEmptyEntries);
                        if (sections.Length >= 2)
                        {
                            assessment.Summary += "\n\nAI Assessment:\n" + sections[0].Trim();
                            assessment.InvestigationRecommendations = sections[1]
                                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                                .Select(l => l.Trim().TrimStart('0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '.', '-', ' '))
                                .Where(l => l.Length > 5).ToList();
                            if (sections.Length > 2)
                                assessment.ActionItems = sections[2]
                                    .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                                    .Select(l => l.Trim().TrimStart('0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '.', '-', ' '))
                                    .Where(l => l.Length > 5).ToList();
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "AI assessment generation failed, using rule-based recommendations");
                }
            }

            // Fallback/supplement: Generate rule-based recommendations
            if (!assessment.InvestigationRecommendations.Any())
                GenerateRecommendations(record, assessment);

            record.Assessment = assessment;
            record.RiskLevel = assessment.RiskScore switch
            {
                >= 75 => "Critical",
                >= 50 => "High",
                >= 25 => "Medium",
                _ => "Low"
            };

            _logger.LogInformation(
                "Assessment complete. Risk Level: {Level}, Risk Score: {Score}",
                record.RiskLevel, assessment.RiskScore);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating assessment");
        }
    }

    /// <summary>
    /// Generate investigation recommendations based on intelligence
    /// </summary>
    private void GenerateRecommendations(
        IntelligenceRecord record,
        IntelligenceAssessment assessment)
    {
        // Banking red flags
        if (record.BankingData?.SuspiciousTransactionCount > 0)
        {
            assessment.InvestigationRecommendations.Add(
                $"Request bank freeze for account - {record.BankingData.SuspiciousTransactionCount} suspicious transactions detected");
            assessment.ActionItems.Add("Submit bank freeze application to court");
        }

        // High frequency communications
        if (record.TelecomData?.CallFrequency?.Values.Max() > 20)
        {
            assessment.InvestigationRecommendations.Add(
                "Conduct surveillance - High frequency communication pattern suggests coordination");
            assessment.ActionItems.Add("Request tower data for location tracking");
        }

        // Wanted status
        if (record.PoliceData?.IsWanted == true)
        {
            assessment.InvestigationRecommendations.Add(
                "PRIORITY: Issue lookout circular - Suspect is in wanted list");
            assessment.ActionItems.Add("Issue all-India lookout circular immediately");
        }

        // Multiple identities
        if (record.OSINTData?.NumberOfFakeProfiles > 0 ||
            record.OSINTData?.HasDuplicateAccounts == true)
        {
            assessment.InvestigationRecommendations.Add(
                "Investigate fake profiles - Multiple identity patterns suggest potential fraud gang activity");
            assessment.ActionItems.Add("Request private data from social media platforms");
        }

        // Next steps
        if (assessment.ActionItems.Count > 0)
        {
            assessment.NextInvestigationStep = assessment.ActionItems.First();
            assessment.RecommendedFollowUpDate = DateTime.UtcNow.AddDays(3);
        }
    }

    /// <summary>
    /// Placeholder: Find bank account linked to phone number
    /// In real scenario, would query banking database
    /// </summary>
    private async Task<string?> FindBankAccountForPhoneAsync(string phoneNumber)
    {
        // Placeholder - in real scenario would query a bank aggregator service
        return null;
    }
}

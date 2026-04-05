using AILegalAsst.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AILegalAsst.Services;

/// <summary>
/// Phone Intelligence Service
/// 
/// Analyzes a single phone number across all intelligence sources:
/// - Who is using this phone?
/// - What are their activities?
/// - What other phones/suspects are they connected to?
/// - What is their criminal risk profile?
/// - What investigations are they involved in?
/// 
/// Used by: Intelligence Dashboard, Investigation Copilot, Admin Reports
/// </summary>
public class PhoneIntelligenceService
{
    private readonly IntelligenceGatheringService _intelligenceService;
    private readonly PhoneIntelAPIClient _apiClient;
    private readonly ILogger<PhoneIntelligenceService> _logger;
    private readonly AzureAgentService _agentService;

    public PhoneIntelligenceService(
        IntelligenceGatheringService intelligenceService,
        PhoneIntelAPIClient apiClient,
        ILogger<PhoneIntelligenceService> logger,
        AzureAgentService agentService)
    {
        _intelligenceService = intelligenceService;
        _apiClient = apiClient;
        _logger = logger;
        _agentService = agentService;
    }

    /// <summary>
    /// Get quick intelligence summary for a phone number
    /// Used for dashboard widget, quick lookup
    /// Returns in <1 second (uses cache)
    /// </summary>
    public async Task<Result<PhoneIntelligenceSummary>> GetPhoneSummaryAsync(
        string phoneNumber)
    {
        try
        {
            _logger.LogInformation("Getting phone summary for {Phone}", phoneNumber);

            // Check if phone is cached
            var cached = await GetCachedIntelligenceAsync(phoneNumber);
            if (cached != null)
            {
                return Result<PhoneIntelligenceSummary>.Success(
                    CreateSummaryFromRecord(cached));
            }

            return Result<PhoneIntelligenceSummary>.Failure(
                "Intelligence not available. Run full intelligence gathering first.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting phone summary");
            return Result<PhoneIntelligenceSummary>.Failure($"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Get detailed intelligence profile for a phone
    /// Includes all data from all sources + analysis
    /// </summary>
    public async Task<Result<PhoneIntelligenceProfile>> GetDetailedProfileAsync(
        string phoneNumber)
    {
        try
        {
            _logger.LogInformation("Getting detailed profile for {Phone}", phoneNumber);

            var cached = await GetCachedIntelligenceAsync(phoneNumber);
            if (cached == null)
            {
                return Result<PhoneIntelligenceProfile>.Failure(
                    "Intelligence not available. Run full intelligence gathering first.");
            }

            var profile = new PhoneIntelligenceProfile
            {
                PhoneNumber = phoneNumber,
                CreatedDate = cached.CreatedDate,
                LastUpdated = cached.LastUpdated,
                RiskLevel = cached.RiskLevel ?? "Unknown",
                TrustScore = cached.TrustScore ?? 0,

                // Telecom Section
                TelecomSummary = CreateTelecomSummary(cached.TelecomData),

                // Banking Section
                BankingSummary = CreateBankingSummary(cached.BankingData),

                // OSINT Section
                OSINTSummary = CreateOSINTSummary(cached.OSINTData),

                // Police Section
                PoliceSummary = CreatePoliceSummary(cached.PoliceData),

                // Timeline
                TimelineEvents = cached.Timeline ?? new List<IntelligenceTimelineEvent>(),

                // Assessment
                Assessment = cached.Assessment,

                // Relationships
                RelatedCaseIds = cached.RelatedCaseIds,
                LinkedPhoneNumbers = cached.LinkedPhoneIds?.Count ?? 0,
                LinkedSuspects = cached.LinkedSuspectIds?.Count ?? 0
            };

            // Try AI-powered profile narrative
            if (_agentService.IsReady)
            {
                try
                {
                    var prompt = $"Generate a brief intelligence profile narrative for Indian law enforcement:\n" +
                        $"Phone: {phoneNumber}, Risk: {profile.RiskLevel}, Trust Score: {profile.TrustScore}/100\n" +
                        $"Calls: {cached.TelecomData?.TotalCalls ?? 0}, SMS: {cached.TelecomData?.TotalSMSs ?? 0}\n" +
                        $"Suspicious transactions: {cached.BankingData?.SuspiciousTransactionCount ?? 0}\n" +
                        $"Social profiles: {cached.OSINTData?.Profiles?.Count ?? 0}, Fake: {cached.OSINTData?.NumberOfFakeProfiles ?? 0}\n" +
                        $"Arrests: {cached.PoliceData?.TotalArrests ?? 0}, Wanted: {cached.PoliceData?.IsWanted ?? false}\n" +
                        $"Linked cases: {cached.RelatedCaseIds?.Count ?? 0}\n" +
                        "Provide a 2-3 sentence intelligence summary highlighting key risk indicators and recommended investigative focus.";
                    var context = "You are an Indian police intelligence analyst. Provide concise, actionable intelligence narratives.";
                    var response = await _agentService.SendMessageAsync(prompt, context);
                    if (response.Success && !string.IsNullOrWhiteSpace(response.Message))
                    {
                        profile.AiNarrative = response.Message.Trim();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "AI profile narrative generation failed for {Phone}", phoneNumber);
                }
            }

            _logger.LogInformation("Detailed profile retrieved for {Phone}", phoneNumber);
            return Result<PhoneIntelligenceProfile>.Success(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting detailed profile");
            return Result<PhoneIntelligenceProfile>.Failure($"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Get communication timeline for a phone
    /// All calls, messages, transactions in chronological order
    /// </summary>
    public async Task<Result<List<TimelineItemViewModel>>> GetCommunicationTimelineAsync(
        string phoneNumber)
    {
        try
        {
            _logger.LogInformation("Getting communication timeline for {Phone}", phoneNumber);

            var cached = await GetCachedIntelligenceAsync(phoneNumber);
            if (cached?.Timeline == null)
            {
                return Result<List<TimelineItemViewModel>>.Failure(
                    "No timeline data available");
            }

            var timeline = cached.Timeline
                .OrderByDescending(e => e.EventDateTime)
                .Select(e => new TimelineItemViewModel
                {
                    DateTime = e.EventDateTime,
                    EventType = e.EventType,
                    Description = e.EventDescription,
                    DataSource = e.DataSource,
                    RelatedEntity = e.RelatedPhoneOrAccount,
                    Location = e.Location,
                    Icon = GetIconForEventType(e.EventType)
                })
                .ToList();

            _logger.LogInformation(
                "Timeline retrieved with {Count} events",
                timeline.Count);

            return Result<List<TimelineItemViewModel>>.Success(timeline);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting timeline");
            return Result<List<TimelineItemViewModel>>.Failure($"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Get top contacts for a phone (people they call most)
    /// </summary>
    public async Task<Result<List<ContactSummary>>> GetTopContactsAsync(
        string phoneNumber,
        int topN = 10)
    {
        try
        {
            _logger.LogInformation(
                "Getting top {TopN} contacts for {Phone}",
                topN, phoneNumber);

            var cached = await GetCachedIntelligenceAsync(phoneNumber);
            if (cached?.TelecomData?.CallFrequency == null)
            {
                return Result<List<ContactSummary>>.Failure("No contact data available");
            }

            var topContacts = cached.TelecomData.CallFrequency
                .OrderByDescending(x => x.Value)
                .Take(topN)
                .Select(x => new ContactSummary
                {
                    PhoneNumber = x.Key,
                    CallCount = x.Value,
                    // Check if this contact is also a suspect
                    IsKnownSuspect = false, // Would check database
                    RiskLevel = "Unknown",
                    LastContactDate = cached.Timeline?
                        .Where(e => e.RelatedPhoneOrAccount == x.Key)
                        .OrderByDescending(e => e.EventDateTime)
                        .FirstOrDefault()?.EventDateTime
                })
                .ToList();

            return Result<List<ContactSummary>>.Success(topContacts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting top contacts");
            return Result<List<ContactSummary>>.Failure($"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Get financial activities for a phone's linked account
    /// </summary>
    public async Task<Result<FinancialActivitySummary>> GetFinancialActivityAsync(
        string phoneNumber)
    {
        try
        {
            _logger.LogInformation(
                "Getting financial activity for {Phone}",
                phoneNumber);

            var cached = await GetCachedIntelligenceAsync(phoneNumber);
            if (cached?.BankingData == null)
            {
                return Result<FinancialActivitySummary>.Failure(
                    "No banking data available");
            }

            var summary = new FinancialActivitySummary
            {
                TotalIncome = cached.BankingData.TotalIncomeLast30Days,
                TotalExpense = cached.BankingData.TotalExpenseLast30Days,
                AverageTransactionAmount = cached.BankingData.AverageTransactionAmount,
                TotalTransactions = cached.BankingData.Transactions?.Count ?? 0,
                SuspiciousTransactionCount = cached.BankingData.SuspiciousTransactionCount,
                TopRecipients = cached.BankingData.FrequentRecipients?.Take(5).ToList() ?? new(),
                TopSenders = cached.BankingData.FrequentSenders?.Take(5).ToList() ?? new(),
                IsAccountFrozen = cached.BankingData.HasAccountFreeze,
                RiskIndicators = GenerateFinancialRiskIndicators(cached.BankingData)
            };

            return Result<FinancialActivitySummary>.Success(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting financial activity");
            return Result<FinancialActivitySummary>.Failure($"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Get related cases for this phone number
    /// </summary>
    public async Task<Result<List<RelatedCaseSummary>>> GetRelatedCasesAsync(
        string phoneNumber)
    {
        try
        {
            _logger.LogInformation(
                "Getting related cases for {Phone}",
                phoneNumber);

            var cached = await GetCachedIntelligenceAsync(phoneNumber);
            if (cached?.PoliceData?.RelatedCases == null)
            {
                return Result<List<RelatedCaseSummary>>.Failure("No cases found");
            }

            var cases = cached.PoliceData.RelatedCases
                .Select(c => new RelatedCaseSummary
                {
                    CaseNumber = c.CaseNumber ?? "",
                    CaseTitle = c.CaseTitle ?? "",
                    Role = c.RoleInCase ?? "Unknown",
                    CaseDate = c.CaseDate,
                    Status = c.CaseStatus ?? "Unknown"
                })
                .ToList();

            return Result<List<RelatedCaseSummary>>.Success(cases);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting related cases");
            return Result<List<RelatedCaseSummary>>.Failure($"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Get criminal history for this phone's user
    /// </summary>
    public async Task<Result<CriminalHistorySummary>> GetCriminalHistoryAsync(
        string phoneNumber)
    {
        try
        {
            _logger.LogInformation(
                "Getting criminal history for {Phone}",
                phoneNumber);

            var cached = await GetCachedIntelligenceAsync(phoneNumber);
            if (cached?.PoliceData == null)
            {
                return Result<CriminalHistorySummary>.Failure("No criminal data");
            }

            var summary = new CriminalHistorySummary
            {
                TotalArrests = cached.PoliceData.TotalArrests,
                TotalCases = cached.PoliceData.TotalCases,
                Arrests = cached.PoliceData.PreviousArrests ?? new(),
                IsWanted = cached.PoliceData.IsWanted,
                IsAbsconder = cached.PoliceData.IsAbsconder,
                IsDangerous = cached.PoliceData.IsDangerous,
                Convictions = cached.PoliceData.ConvictionHistory ?? new(),
                KnownMethods = cached.PoliceData.KnownCrimeMethods ?? new(),
                CriminalSpecialty = cached.PoliceData.CriminalSpecialty ?? "Unknown"
            };

            return Result<CriminalHistorySummary>.Success(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting criminal history");
            return Result<CriminalHistorySummary>.Failure($"Error: {ex.Message}");
        }
    }

    // ===== Helper Methods =====

    private async Task<IntelligenceRecord?> GetCachedIntelligenceAsync(string phoneNumber)
    {
        try
        {
            // Fetch data from all APIs in parallel for performance
            var telecomTask = _apiClient.GetTelecomDataAsync(phoneNumber);
            var bankingTask = _apiClient.GetBankingDataAsync(phoneNumber);
            var osintTask = _apiClient.GetOSINTDataAsync(phoneNumber);
            var policeTask = _apiClient.GetPoliceDataAsync(phoneNumber);

            await Task.WhenAll(telecomTask, bankingTask, osintTask, policeTask);

            var telecomResponse = await telecomTask;
            var bankingResponse = await bankingTask;
            var osintResponse = await osintTask;
            var policeResponse = await policeTask;

            // Check if all APIs returned data
            if (!telecomResponse.Success || !bankingResponse.Success || 
                !osintResponse.Success || !policeResponse.Success)
            {
                _logger.LogWarning("Some API calls failed for {Phone}", phoneNumber);
            }

            // Create simplified IntelligenceRecord with basic data
            // TODO: Phase 2 - Implement full mapping between API responses and IntelligenceRecord models
            var record = new IntelligenceRecord
            {
                PhoneNumber = phoneNumber,
                CreatedDate = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow,
                RiskLevel = policeResponse.Data?.RiskLevel ?? "Low",
                TrustScore = CalculateTrustScore(telecomResponse.Data, bankingResponse.Data, 
                    osintResponse.Data, policeResponse.Data),
                
                // Simplified mapping - store raw counts
                TelecomData = telecomResponse.Success && telecomResponse.Data != null 
                    ? new TelecomIntelligence
                    {
                        PhoneNumber = phoneNumber,
                        SubscriberName = telecomResponse.Data.OwnerName,
                        Provider = telecomResponse.Data.CarrierName,
                        TotalCalls = telecomResponse.Data.TotalCalls,
                        TotalSMSs = telecomResponse.Data.TotalSMS,
                        FrequentContacts = telecomResponse.Data.CallFrequency.Keys.ToList(),
                        CallFrequency = telecomResponse.Data.CallFrequency,
                        LatestActivityDate = DateTime.TryParse(telecomResponse.Data.LastActiveDate, out var date) ? date : (DateTime?)null,
                        LastKnownLocation = telecomResponse.Data.RecentCalls.FirstOrDefault()?.Location
                    }
                    : null,
                
                // Banking data simplified  
                BankingData = bankingResponse.Success && bankingResponse.Data != null
                    ? new BankingIntelligence
                    {
                        PhoneNumber = phoneNumber,
                        PrimaryBank = bankingResponse.Data.LinkedAccounts.FirstOrDefault()?.BankName,
                        TotalIncomeLast30Days = bankingResponse.Data.TotalIncomeLast30Days,
                        TotalExpenseLast30Days = bankingResponse.Data.TotalExpenseLast30Days,
                        AverageTransactionAmount = bankingResponse.Data.AverageTransactionAmount,
                        FrequentRecipients = bankingResponse.Data.FrequentRecipients,
                        FrequentSenders = bankingResponse.Data.FrequentSenders
                    }
                    : null,
                
                // OSINT simplified
                OSINTData = osintResponse.Success && osintResponse.Data != null
                    ? new OSINTIntelligence
                    {
                        PhoneNumber = phoneNumber,
                        Profiles = osintResponse.Data.Profiles.Select(p => new SocialMediaProfile
                        {
                            Platform = p.Platform,
                            Username = p.Username,
                            FollowerCount = p.FollowerCount
                        }).ToList()
                    }
                    : null,
                
                // Police data simplified
                PoliceData = policeResponse.Success && policeResponse.Data != null
                    ? new PoliceIntelligence
                    {
                        PhoneNumber = phoneNumber,
                        IsWanted = policeResponse.Data.IsWanted,
                        IsAbsconder = policeResponse.Data.IsAbsconder,
                        IsDangerous = policeResponse.Data.IsDangerous,
                        TotalArrests = policeResponse.Data.TotalArrests,
                        TotalCases = policeResponse.Data.TotalCases,
                        PreviousArrests = policeResponse.Data.PreviousArrests.Select(a => new PreviousArrest
                        {
                            ArrestDate = a.ArrestDate,
                            Charges = a.Charges
                        }).ToList()
                    }
                    : null
            };

            _logger.LogInformation("Intelligence record created for {Phone} from APIs", phoneNumber);
            return record;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching intelligence for {Phone}", phoneNumber);
            return null;
        }
    }

    private double CalculateTrustScore(
        TelecomDataResponse? telecom,
        BankingDataResponse? banking,
        OSINTDataResponse? osint,
        PoliceDataResponse? police)
    {
        double score = 100.0; // Start with perfect score

        // Reduce score based on risk factors
        if (police != null)
        {
            if (police.IsWanted) score -= 50;
            if (police.IsAbsconder) score -= 40;
            if (police.IsDangerous) score -= 40;
            score -= police.TotalArrests * 5;
            score -= police.TotalCases * 3;
        }

        if (banking != null)
        {
            score -= banking.SuspiciousTransactionCount * 2;
            if (banking.HasAccountFreeze) score -= 30;
        }

        if (osint != null)
        {
            score -= osint.RiskScore * 0.5; // OSINT risk score contribution
        }

        return Math.Max(0, Math.Min(100, score)); // Clamp between 0-100
    }

    // TODO: Phase 2 - Implement detailed mapping methods:
    // - Map ToTelecomIntelligence (with CDR records, tower data)
    // - MapToBankingIntelligence (with transactions, accounts)
    // - MapToOSINTIntelligence (with social profiles, email addresses)
    // - MapToPoliceIntelligence (with arrest details, case records)
    // - GenerateTimeline (from all data sources)
    // - GenerateAssessment (with risk analysis)

    private PhoneIntelligenceSummary CreateSummaryFromRecord(IntelligenceRecord record)
    {
        return new PhoneIntelligenceSummary
        {
            PhoneNumber = record.PhoneNumber ?? "",
            RiskLevel = record.RiskLevel ?? "Unknown",
            CallCount = record.TelecomData?.TotalCalls ?? 0,
            TransactionCount = record.BankingData?.Transactions?.Count ?? 0,
            CaseCount = record.PoliceData?.TotalCases ?? 0,
            ProfilesFound = record.OSINTData?.Profiles?.Count ?? 0,
            LastActivityDate = record.Timeline?.Any() == true ? record.Timeline.Max(e => e.EventDateTime) : null,
            IsWanted = record.PoliceData?.IsWanted ?? false,
            TrustScore = record.TrustScore ?? 0
        };
    }

    private TelecomSummaryViewModel? CreateTelecomSummary(TelecomIntelligence? data)
    {
        if (data == null) return null;

        return new TelecomSummaryViewModel
        {
            Provider = data.Provider ?? "Unknown",
            TotalCalls = data.TotalCalls,
            TotalSMS = data.TotalSMSs,
            TopContactsCount = data.FrequentContacts?.Count ?? 0,
            LatestActivity = data.LatestActivityDate,
            LastKnownLocation = data.LastKnownLocation
        };
    }

    private BankingSummaryViewModel? CreateBankingSummary(BankingIntelligence? data)
    {
        if (data == null) return null;

        return new BankingSummaryViewModel
        {
            PrimaryBank = data.PrimaryBank ?? "Unknown",
            TotalIncome = data.TotalIncomeLast30Days,
            TotalExpense = data.TotalExpenseLast30Days,
            TransactionCount = data.Transactions?.Count ?? 0,
            SuspiciousCount = data.SuspiciousTransactionCount,
            IsAccountFrozen = data.HasAccountFreeze
        };
    }

    private OSINTSummaryViewModel? CreateOSINTSummary(OSINTIntelligence? data)
    {
        if (data == null) return null;

        return new OSINTSummaryViewModel
        {
            ProfilesFound = data.Profiles?.Count ?? 0,
            FakeProfilesDetected = data.NumberOfFakeProfiles,
            PlatformsUsed = data.Profiles?.Select(p => p.Platform).Distinct().ToList() ?? new(),
            IPAddressCount = data.AssociatedIPAddresses?.Count ?? 0
        };
    }

    private PoliceSummaryViewModel? CreatePoliceSummary(PoliceIntelligence? data)
    {
        if (data == null) return null;

        return new PoliceSummaryViewModel
        {
            TotalArrests = data.TotalArrests,
            TotalCases = data.TotalCases,
            IsWanted = data.IsWanted,
            IsAbsconder = data.IsAbsconder,
            IsDangerous = data.IsDangerous,
            CriminalSpecialty = data.CriminalSpecialty ?? "Unknown"
        };
    }

    private string GetIconForEventType(string? eventType)
    {
        return eventType switch
        {
            "Call" => "bi-telephone",
            "SMS" => "bi-chat-dots",
            "Transaction" => "bi-wallet2",
            "Arrest" => "bi-exclamation-triangle",
            "SocialMedia" => "bi-share",
            "Post" => "bi-pencil-square",
            _ => "bi-info-circle"
        };
    }

    private List<string> GenerateFinancialRiskIndicators(BankingIntelligence data)
    {
        var indicators = new List<string>();

        if (data.SuspiciousTransactionCount > 0)
            indicators.Add("Suspicious transactions detected");

        if (data.HasAccountFreeze)
            indicators.Add("Account is frozen by police");

        if (data.Transactions != null)
        {
            // Check for pattern: multiple deposits followed by withdrawal
            var deposits = data.Transactions.Where(t => t.TransactionType == "Credit").Count();
            var withdrawals = data.Transactions.Where(t => t.TransactionType == "Debit").Count();

            if (withdrawals > deposits * 2)
                indicators.Add("Unusual withdrawal pattern");
        }

        return indicators;
    }
}

// ===== VIEW MODELS =====

public class PhoneIntelligenceSummary
{
    public string PhoneNumber { get; set; } = "";
    public string RiskLevel { get; set; } = "";
    public int CallCount { get; set; }
    public int TransactionCount { get; set; }
    public int CaseCount { get; set; }
    public int ProfilesFound { get; set; }
    public DateTime? LastActivityDate { get; set; }
    public bool IsWanted { get; set; }
    public double TrustScore { get; set; }
}

public class PhoneIntelligenceProfile
{
    public string PhoneNumber { get; set; } = "";
    public DateTime CreatedDate { get; set; }
    public DateTime? LastUpdated { get; set; }
    public string RiskLevel { get; set; } = "";
    public double TrustScore { get; set; }

    public TelecomSummaryViewModel? TelecomSummary { get; set; }
    public BankingSummaryViewModel? BankingSummary { get; set; }
    public OSINTSummaryViewModel? OSINTSummary { get; set; }
    public PoliceSummaryViewModel? PoliceSummary { get; set; }

    public List<IntelligenceTimelineEvent> TimelineEvents { get; set; } = new();
    public IntelligenceAssessment? Assessment { get; set; }

    public List<int> RelatedCaseIds { get; set; } = new();
    public int LinkedPhoneNumbers { get; set; }
    public int LinkedSuspects { get; set; }
    public string? AiNarrative { get; set; }
}

public class TimelineItemViewModel
{
    public DateTime DateTime { get; set; }
    public string? EventType { get; set; }
    public string? Description { get; set; }
    public string? DataSource { get; set; }
    public string? RelatedEntity { get; set; }
    public string? Location { get; set; }
    public string? Icon { get; set; }
}

public class ContactSummary
{
    public string PhoneNumber { get; set; } = "";
    public int CallCount { get; set; }
    public bool IsKnownSuspect { get; set; }
    public string RiskLevel { get; set; } = "";
    public DateTime? LastContactDate { get; set; }
}

public class FinancialActivitySummary
{
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal AverageTransactionAmount { get; set; }
    public int TotalTransactions { get; set; }
    public int SuspiciousTransactionCount { get; set; }
    public List<string> TopRecipients { get; set; } = new();
    public List<string> TopSenders { get; set; } = new();
    public bool IsAccountFrozen { get; set; }
    public List<string> RiskIndicators { get; set; } = new();
}

public class RelatedCaseSummary
{
    public string CaseNumber { get; set; } = "";
    public string CaseTitle { get; set; } = "";
    public string Role { get; set; } = "";
    public DateTime CaseDate { get; set; }
    public string Status { get; set; } = "";
}

public class CriminalHistorySummary
{
    public int TotalArrests { get; set; }
    public int TotalCases { get; set; }
    public List<PreviousArrest> Arrests { get; set; } = new();
    public bool IsWanted { get; set; }
    public bool IsAbsconder { get; set; }
    public bool IsDangerous { get; set; }
    public List<string> Convictions { get; set; } = new();
    public List<string> KnownMethods { get; set; } = new();
    public string CriminalSpecialty { get; set; } = "";
}

public class TelecomSummaryViewModel
{
    public string Provider { get; set; } = "";
    public int TotalCalls { get; set; }
    public int TotalSMS { get; set; }
    public int TopContactsCount { get; set; }
    public DateTime? LatestActivity { get; set; }
    public string? LastKnownLocation { get; set; }
}

public class BankingSummaryViewModel
{
    public string PrimaryBank { get; set; } = "";
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public int TransactionCount { get; set; }
    public int SuspiciousCount { get; set; }
    public bool IsAccountFrozen { get; set; }
}

public class OSINTSummaryViewModel
{
    public int ProfilesFound { get; set; }
    public int FakeProfilesDetected { get; set; }
    public List<string?> PlatformsUsed { get; set; } = new();
    public int IPAddressCount { get; set; }
}

public class PoliceSummaryViewModel
{
    public int TotalArrests { get; set; }
    public int TotalCases { get; set; }
    public bool IsWanted { get; set; }
    public bool IsAbsconder { get; set; }
    public bool IsDangerous { get; set; }
    public string CriminalSpecialty { get; set; } = "";
}

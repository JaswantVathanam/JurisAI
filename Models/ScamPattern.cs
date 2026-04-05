using System;
using System.Collections.Generic;

namespace AILegalAsst.Models
{
    /// <summary>
    /// Represents a scam pattern identified from multiple complaints
    /// </summary>
    public class ScamPattern
    {
        public int Id { get; set; }
        public string PatternName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ScamCategory Category { get; set; }
        public List<string> Keywords { get; set; } = new();
        public List<string> PhoneNumbers { get; set; } = new();
        public List<string> UpiIds { get; set; } = new();
        public List<string> BankAccounts { get; set; } = new();
        public List<string> Emails { get; set; } = new();
        public List<string> Websites { get; set; } = new();
        public List<string> AffectedStates { get; set; } = new();
        public int TotalComplaints { get; set; }
        public decimal TotalFraudAmount { get; set; }
        public decimal RecoveredAmount { get; set; }
        public DateTime FirstReportedDate { get; set; }
        public DateTime LastReportedDate { get; set; }
        public bool IsActiveInvestigation { get; set; }
        public PatternSeverity Severity { get; set; }
        public List<string> ModusOperandi { get; set; } = new();
        public List<string> PreventionTips { get; set; } = new();
    }

    /// <summary>
    // Removed duplicate ScamReport class. Use the one in ScamReport.cs

    /// <summary>
    /// Represents a match between a report and existing pattern/reports
    /// </summary>
    public class ScamMatch
    {
        public int ReportId { get; set; }
        public string MatchType { get; set; } = string.Empty; // "phone", "upi", "account", "keyword", "pattern"
        public string MatchedValue { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public string State { get; set; } = string.Empty;
        public DateTime ReportedDate { get; set; }
        public decimal AmountLost { get; set; }
    }

    /// <summary>
    /// Analysis result when checking a new complaint
    /// </summary>
    public class ScamAnalysisResult
    {
        public bool HasMatches { get; set; }
        public int TotalMatches { get; set; }
        public ScamPattern? IdentifiedPattern { get; set; }
        public List<ScamMatch> Matches { get; set; } = new();
        public Dictionary<string, int> MatchesByState { get; set; } = new();
        public decimal TotalFraudAmount { get; set; }
        public int AffectedVictims { get; set; }
        public DateTime OldestReport { get; set; }
        public double OverallConfidence { get; set; }
        public RiskLevel RiskLevel { get; set; }
        public List<string> Warnings { get; set; } = new();
        public List<string> RecommendedActions { get; set; } = new();
        
        // AI Analysis Results
        public AIScamAnalysis? AIAnalysis { get; set; }
    }

    /// <summary>
    /// AI-powered analysis of scam complaint
    /// </summary>
    public class AIScamAnalysis
    {
        public string Summary { get; set; } = string.Empty;
        public string ScamType { get; set; } = string.Empty;
        public string MostLikelyScenario { get; set; } = string.Empty;
        public List<string> ExtractedEvidence { get; set; } = new();
        public List<string> ImmediateSteps { get; set; } = new();
        public List<string> LegalAdvice { get; set; } = new();
        public List<string> ApplicableLaws { get; set; } = new();
        public string RecoveryProspect { get; set; } = string.Empty;
        public List<string> PreventionTips { get; set; } = new();
        public string VictimSupportMessage { get; set; } = string.Empty;
        public int ScamComplexityScore { get; set; } // 1-10
        public bool NeedsUrgentAction { get; set; }
        public DateTime AnalyzedAt { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Statistics for the scam detector dashboard
    /// </summary>
    public class ScamStatistics
    {
        public int TotalReportsToday { get; set; }
        public int TotalReportsThisMonth { get; set; }
        public int TotalPatternsIdentified { get; set; }
        public decimal TotalAmountReported { get; set; }
        public decimal TotalAmountRecovered { get; set; }
        public Dictionary<ScamCategory, int> ReportsByCategory { get; set; } = new();
        public Dictionary<string, int> ReportsByState { get; set; } = new();
        public List<ScamPattern> TrendingPatterns { get; set; } = new();
        public List<string> RecentAlerts { get; set; } = new();
    }

    public enum ScamCategory
    {
        UPIFraud,
        LotteryScam,
        JobFraud,
        LoanFraud,
        InvestmentScam,
        RomanceScam,
        TechSupportScam,
        ImpersonationScam,
        PhishingScam,
        OTPFraud,
        SocialMediaScam,
        EcommerceFraud,
        InsuranceFraud,
        CryptoScam,
        SextortionScam,
        CustomsScam,
        CourierScam,
        BankingFraud,
        Other
    }

    public enum PatternSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }

    public enum RiskLevel
    {
        NoRisk,
        Low,
        Medium,
        High,
        Critical
    }
}

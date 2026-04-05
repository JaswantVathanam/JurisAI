using System;
using System.Collections.Generic;

namespace AILegalAsst.Models
{
    // Legacy model for existing features
    public enum ScamType
    {
        Phishing,
        InvestmentFraud,
        UPIFraud,
        Sextortion,
        FakeJob,
        Lottery,
        IdentityTheft,
        Other
    }

    public class ScamReport
    {
        public int Id { get; set; }
        public string ReporterId { get; set; } = string.Empty;
        public string ReporterName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ScamCategory Category { get; set; }
        public decimal AmountLost { get; set; }
        public DateTime IncidentDate { get; set; }
        public DateTime ReportedDate { get; set; } = DateTime.Now;
        public string State { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public List<string> PhoneNumbers { get; set; } = new();
        public List<string> UpiIds { get; set; } = new();
        public List<string> BankAccounts { get; set; } = new();
        public List<string> Emails { get; set; } = new();
        public List<string> Websites { get; set; } = new();
        public List<string> SocialMediaHandles { get; set; } = new();
        public int? MatchedPatternId { get; set; }
        public List<ScamMatch> SimilarReports { get; set; } = new();
        public double MatchConfidence { get; set; }
        public bool IsVerified { get; set; }
        public string? LinkedCaseNumber { get; set; }
    }
    public enum CommunityScamType
    {
        Phishing,
        InvestmentFraud,
        UPIFraud,
        Sextortion,
        FakeJob,
        Lottery,
        IdentityTheft,
        Other
    }

    public class CommunityScamReport
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public CommunityScamType Type { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty; // City/District/LatLng
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? ScreenshotUrl { get; set; }
        public string? ChatExportUrl { get; set; }
        public string ReporterType { get; set; } = "Citizen"; // Citizen, Police, Lawyer
        public string? PhoneNumber { get; set; }
        public string? UPIId { get; set; }
        public List<string> Keywords { get; set; } = new();
    }

    public class CommunityScamTrend
    {
        public CommunityScamType Type { get; set; }
        public int Count { get; set; }
        public List<string> Locations { get; set; } = new();
        public List<string> TrendingKeywords { get; set; } = new();
        public DateTime WindowStart { get; set; }
        public DateTime WindowEnd { get; set; }
        public string? AiInsight { get; set; }
    }
}

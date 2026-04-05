namespace AILegalAsst.Models;

public enum CybercrimeType
{
    Hacking,
    Phishing,
    IdentityTheft,
    OnlineFraud,
    Cyberstalking,
    Cyberbullying,
    DataBreach,
    Ransomware,
    ChildPornography,
    OnlineHarassment,
    SocialMediaAbuse,
    BankingFraud,
    CreditCardFraud,
    UPIFraud,
    EcommerceScam,
    JobScam,
    MatrimonialScam,
    Other
}

public enum ReportStatus
{
    Submitted,
    UnderReview,
    InvestigationStarted,
    EvidenceCollected,
    FIRFiled,
    UnderTrial,
    Closed,
    Rejected
}

public class CybercrimeReport
{
    public int Id { get; set; }
    public string ReportNumber { get; set; } = string.Empty; // e.g., "CYB-2025-001234"
    public CybercrimeType IncidentType { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime IncidentDate { get; set; }
    public DateTime ReportedDate { get; set; } = DateTime.UtcNow;
    
    // Victim Information
    public int VictimUserId { get; set; }
    public string VictimName { get; set; } = string.Empty;
    public string VictimEmail { get; set; } = string.Empty;
    public string VictimPhone { get; set; } = string.Empty;
    public string VictimAddress { get; set; } = string.Empty;
    
    // Incident Location
    public string Location { get; set; } = string.Empty;
    
    // Suspect Information
    public string SuspectName { get; set; } = string.Empty;
    public string SuspectInfo { get; set; } = string.Empty;
    public string SuspectContact { get; set; } = string.Empty;
    public string SuspectSocialMedia { get; set; } = string.Empty;
    
    // Evidence
    public List<string> EvidenceFiles { get; set; } = new(); // File paths or URLs
    public List<string> Screenshots { get; set; } = new();
    public string DigitalFootprint { get; set; } = string.Empty; // URLs, IP addresses, etc.
    public decimal? FinancialLoss { get; set; }
    public string TransactionDetails { get; set; } = string.Empty;
    
    // Investigation
    public ReportStatus Status { get; set; } = ReportStatus.Submitted;
    public int? AssignedOfficerId { get; set; }
    public string AssignedOfficerName { get; set; } = string.Empty;
    public string PoliceStationName { get; set; } = string.Empty;
    public string FIRNumber { get; set; } = string.Empty;
    public DateTime? FIRFiledDate { get; set; }
    public string InvestigationNotes { get; set; } = string.Empty;
    public List<string> ApplicableSections { get; set; } = new(); // IT Act sections
    
    // Priority and Category
    public bool IsUrgent { get; set; }
    public bool IsMinorInvolved { get; set; }
    public string Category { get; set; } = string.Empty; // Financial, Personal, Sexual, etc.
    
    // Lawyer Assignment (optional)
    public int? LawyerId { get; set; }
    public string LawyerName { get; set; } = string.Empty;
    public string LegalAdvice { get; set; } = string.Empty;
    
    // Updates and Communication
    public List<CybercrimeUpdate> Updates { get; set; } = new();
    public DateTime? LastUpdated { get; set; }
}

public class CybercrimeUpdate
{
    public int Id { get; set; }
    public int ReportId { get; set; }
    public DateTime UpdateDate { get; set; } = DateTime.UtcNow;
    public string UpdatedBy { get; set; } = string.Empty; // Officer/System
    public string Message { get; set; } = string.Empty;
    public ReportStatus NewStatus { get; set; }
}

public class CybercrimeResource
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // Prevention, Awareness, Legal, Emergency
    public string IconClass { get; set; } = string.Empty;
    public string Link { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public bool IsEmergency { get; set; }
}

public class CybercrimeStatistics
{
    public int TotalReports { get; set; }
    public int ActiveInvestigations { get; set; }
    public int FIRsFiled { get; set; }
    public int CasesClosed { get; set; }
    public decimal TotalFinancialLoss { get; set; }
    public Dictionary<CybercrimeType, int> ReportsByType { get; set; } = new();
    public Dictionary<string, int> ReportsByMonth { get; set; } = new();
    public string? AiInsight { get; set; }
}

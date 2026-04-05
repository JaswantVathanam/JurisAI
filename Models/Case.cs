namespace AILegalAsst.Models;

public class Case
{
    public int Id { get; set; }
    public string CaseNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public CaseType Type { get; set; }
    public CaseStatus Status { get; set; }
    public DateTime FiledDate { get; set; } = DateTime.UtcNow;
    public DateTime? LastUpdated { get; set; }
    
    // Parties involved
    public string Complainant { get; set; } = string.Empty;
    public string Accused { get; set; } = string.Empty;
    public string? Plaintiff { get; set; }
    public string? Defendant { get; set; }
    public string? LawyerName { get; set; }
    
    // Court information
    public string? Court { get; set; }
    public DateTime? NextHearingDate { get; set; }
    
    // Legal references
    public List<string> ApplicableLaws { get; set; } = new();
    public List<string> Sections { get; set; } = new();
    public List<string> ApplicableSections { get; set; } = new();
    
    // Case details
    public string? FIRNumber { get; set; }
    public string? PoliceStation { get; set; }
    public string? AssignedLawyer { get; set; }
    public string? AssignedJudge { get; set; }
    public string? InvestigatingOfficer { get; set; }
    
    // Cybercrime specific
    public bool IsCybercrime { get; set; }
    public string? CybercrimeCategory { get; set; }
    public string? DigitalEvidence { get; set; }
    public bool DigitalEvidenceCollected { get; set; }
    
    // Related cases
    public List<int> RelatedCaseIds { get; set; } = new();
    
    // Documents and evidence
    public List<CaseDocument> Documents { get; set; } = new();
    
    // Timeline events
    public List<CaseTimelineEvent> TimelineEvents { get; set; } = new();
    
    // AI Analysis
    public string? AIAnalysis { get; set; }
    public double? SuccessProbability { get; set; }
    
    // AI Action Attribution — tracks WHO authorized AI to file this case
    public bool FiledViaAI { get; set; }
    public string? AIActionHash { get; set; }
    public int? FiledByUserId { get; set; }
    public string? FiledByUserEmail { get; set; }
    public string? IdentityVerificationMethod { get; set; }
    public DateTime? IdentityVerifiedAt { get; set; }
    public string? DeviceFingerprint { get; set; }
    public string? FilingSessionId { get; set; }
}

public enum CaseType
{
    Criminal,
    Civil,
    Cybercrime,
    Constitutional,
    Other
}

public enum CaseStatus
{
    Draft,
    Filed,
    UnderInvestigation,
    ChargesheetFiled,
    InProgress,
    TrialInProgress,
    OnHold,
    Judgement,
    Closed,
    Dismissed,
    Appealed
}

public class CaseDocument
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public DateTime UploadedDate { get; set; } = DateTime.UtcNow;
    public string UploadedBy { get; set; } = string.Empty;
}

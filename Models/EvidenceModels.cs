namespace AILegalAsst.Models;

/// <summary>
/// Evidence item with chain of custody tracking
/// </summary>
public class EvidenceItem
{
    public string Id { get; set; } = string.Empty;
    public string EvidenceNumber { get; set; } = string.Empty;
    public string CaseId { get; set; } = string.Empty;
    public string CaseNumber { get; set; } = string.Empty;
    public string FIRNumber { get; set; } = string.Empty;
    
    // Evidence Details
    public EvidenceType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string MimeType { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    
    // Integrity Verification
    public string SHA256Hash { get; set; } = string.Empty;
    public string MD5Hash { get; set; } = string.Empty;
    public string OriginalHash { get; set; } = string.Empty;
    public bool ChainIntegrity { get; set; }
    public DateTime? LastVerifiedAt { get; set; }
    
    // Collection Details
    public DateTime CollectedAt { get; set; }
    public string CollectedBy { get; set; } = string.Empty;
    public string CollectionLocation { get; set; } = string.Empty;
    public double? CollectionLatitude { get; set; }
    public double? CollectionLongitude { get; set; }
    public string CollectionMethod { get; set; } = string.Empty;
    public string SeizureMemoNumber { get; set; } = string.Empty;
    
    // Current Status
    public EvidenceStatus Status { get; set; }
    public string CurrentCustodian { get; set; } = string.Empty;
    public string StorageLocation { get; set; } = string.Empty;
    
    // Witness Information
    public string WitnessName { get; set; } = string.Empty;
    public string WitnessBadgeNumber { get; set; } = string.Empty;
    
    // Transfer tracking
    public int TotalCustodyTransfers { get; set; }
    
    // Metadata
    public Dictionary<string, string> Metadata { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public string Notes { get; set; } = string.Empty;
    
    // Chain of Custody
    public List<CustodyLog> CustodyChain { get; set; } = new();
    
    // Court Submission
    public bool IsCourtSubmitted { get; set; }
    public DateTime? CourtSubmissionDate { get; set; }
    public string CourtExhibitNumber { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public enum EvidenceType
{
    DigitalDocument,
    PDFDocument,
    MobileDevice,
    Computer,
    StorageDevice,
    CCTV,
    VideoRecording,
    Photograph,
    AudioRecording,
    FinancialRecord,
    BankStatement,
    EmailCorrespondence,
    SocialMediaData,
    CDR,
    IPDR,
    ForensicReport,
    PhysicalDocument,
    PhysicalDevice,
    WitnessStatement,
    ExpertOpinion,
    Other
}

public enum EvidenceStatus
{
    Collected,
    InCustody,
    Secured,
    Verified,
    UnderAnalysis,
    InForensicLab,
    ReturnedToOwner,
    CourtSubmitted,
    Disposed,
    Lost,
    Damaged,
    Tampered
}

/// <summary>
/// Single entry in the chain of custody log
/// </summary>
public class CustodyLog
{
    public string Id { get; set; } = string.Empty;
    public string EvidenceId { get; set; } = string.Empty;
    public DateTime DateTime { get; set; }
    public CustodyAction Action { get; set; }
    
    // Transfer Details
    public string FromPerson { get; set; } = string.Empty;
    public string FromDesignation { get; set; } = string.Empty;
    public string ToPerson { get; set; } = string.Empty;
    public string ToDesignation { get; set; } = string.Empty;
    public string PerformedBy { get; set; } = string.Empty;
    
    // Location
    public string FromLocation { get; set; } = string.Empty;
    public string ToLocation { get; set; } = string.Empty;
    
    // Verification
    public string HashAtTransfer { get; set; } = string.Empty;
    public bool IntegrityVerified { get; set; }
    public string Condition { get; set; } = string.Empty;
    
    // Documentation
    public string Purpose { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string WitnessName { get; set; } = string.Empty;
    public string AuthorizingOfficer { get; set; } = string.Empty;
    public string IPAddress { get; set; } = string.Empty;
    
    // Digital Signature (for tamper-proofing)
    public string DigitalSignature { get; set; } = string.Empty;
    public string BlockHash { get; set; } = string.Empty;
    public string PreviousBlockHash { get; set; } = string.Empty;
}

public enum CustodyAction
{
    Collected,
    Received,
    Transferred,
    TransferredIn,
    TransferredOut,
    StoredInLocker,
    Examined,
    Analyzed,
    SentForAnalysis,
    ReturnedFromAnalysis,
    IntegrityVerified,
    Sealed,
    Unsealed,
    PhotoDocumented,
    SentToLab,
    ReceivedFromLab,
    SubmittedToCourt,
    ReturnedFromCourt,
    TamperDetected,
    StatusChanged,
    Disposed,
    Destroyed,
    ReturnedToOwner,
    StatusVerified
}

/// <summary>
/// Evidence verification result
/// </summary>
public class EvidenceVerification
{
    public string EvidenceId { get; set; } = string.Empty;
    public string EvidenceNumber { get; set; } = string.Empty;
    public DateTime VerifiedAt { get; set; }
    public string VerifiedBy { get; set; } = string.Empty;
    
    public bool IsValid { get; set; }
    public string Message { get; set; } = string.Empty;
    
    public bool HashValid { get; set; }
    public string CurrentHash { get; set; } = string.Empty;
    public string OriginalHash { get; set; } = string.Empty;
    
    public bool ChainValid { get; set; }
    public int TotalCustodyTransfers { get; set; }
    public List<string> ChainIssues { get; set; } = new();
    
    public string VerificationStatus { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

/// <summary>
/// Evidence statistics for a case
/// </summary>
public class EvidenceStatistics
{
    public string? CaseId { get; set; }
    public int TotalEvidence { get; set; }
    public int DigitalEvidence { get; set; }
    public int PhysicalEvidence { get; set; }
    public int VerifiedCount { get; set; }
    public int PendingVerification { get; set; }
    public int UnderAnalysis { get; set; }
    public int CourtSubmitted { get; set; }
    public int TamperedCount { get; set; }
    public int TotalCustodyTransfers { get; set; }
    public long TotalStorageSize { get; set; }
    public Dictionary<string, int> EvidenceByType { get; set; } = new();
    public Dictionary<string, int> EvidenceByStatus { get; set; } = new();
    public double ChainIntegrityRate { get; set; }
    public DateTime LastActivityAt { get; set; }
    public string? AiInsight { get; set; }
}

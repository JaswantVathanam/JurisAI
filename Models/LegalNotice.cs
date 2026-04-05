namespace AILegalAsst.Models;

/// <summary>
/// Types of legal notices that can be generated
/// </summary>
public enum LegalNoticeType
{
    BankAccountFreeze,
    CDRRequest,
    SocialMediaTakedown,
    UPIWalletFreeze,
    WitnessSummons,
    CourtFilingCover,
    VictimStatusUpdate,
    IPAddressRequest,
    MerchantDetailsRequest
}

/// <summary>
/// Represents a generated legal notice
/// </summary>
public class LegalNotice
{
    public int Id { get; set; }
    public string NoticeNumber { get; set; } = string.Empty;
    public LegalNoticeType Type { get; set; }
    public int CaseId { get; set; }
    public string CaseNumber { get; set; } = string.Empty;
    public string FIRNumber { get; set; } = string.Empty;
    
    // Recipient details
    public string RecipientName { get; set; } = string.Empty;
    public string RecipientDesignation { get; set; } = string.Empty;
    public string RecipientOrganization { get; set; } = string.Empty;
    public string RecipientAddress { get; set; } = string.Empty;
    
    // Notice content
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    
    // Request details (for bank/telecom requests)
    public List<string> AccountNumbers { get; set; } = new();
    public List<string> PhoneNumbers { get; set; } = new();
    public List<string> UPIIds { get; set; } = new();
    public List<string> SocialMediaHandles { get; set; } = new();
    public List<string> IPAddresses { get; set; } = new();
    
    // Date range for CDR/transaction requests
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    
    // Sender details (auto-filled from logged-in officer)
    public string SenderName { get; set; } = string.Empty;
    public string SenderDesignation { get; set; } = string.Empty;
    public string SenderStation { get; set; } = string.Empty;
    public string SenderContact { get; set; } = string.Empty;
    
    // Metadata
    public DateTime GeneratedDate { get; set; } = DateTime.Now;
    public string GeneratedBy { get; set; } = string.Empty;
    public bool IsSent { get; set; } = false;
    public DateTime? SentDate { get; set; }
    public string? ResponseReceived { get; set; }
}

/// <summary>
/// Pre-defined recipient organizations for quick selection
/// </summary>
public class NoticeRecipient
{
    public string Name { get; set; } = string.Empty;
    public string Designation { get; set; } = string.Empty;
    public string Organization { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // Bank, Telecom, SocialMedia, etc.
    public string EmailFormat { get; set; } = string.Empty; // For reference
}

/// <summary>
/// Request model for generating a legal notice
/// </summary>
public class LegalNoticeRequest
{
    public LegalNoticeType Type { get; set; }
    public int CaseId { get; set; }
    public string? RecipientId { get; set; } // For pre-defined recipients
    public NoticeRecipient? CustomRecipient { get; set; }
    
    // Data to include in notice
    public List<string> AccountNumbers { get; set; } = new();
    public List<string> PhoneNumbers { get; set; } = new();
    public List<string> UPIIds { get; set; } = new();
    public List<string> SocialMediaHandles { get; set; } = new();
    public List<string> IPAddresses { get; set; } = new();
    
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    
    public string? AdditionalDetails { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace AILegalAsst.Models;

/// <summary>
/// First Information Report (FIR) Draft Model
/// Used to generate police complaints from user descriptions
/// </summary>
public class FIRDraft
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    // Complainant Details
    [Required(ErrorMessage = "Full name is required")]
    public string ComplainantName { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Father's/Husband's name is required")]
    public string FatherOrHusbandName { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Address is required")]
    public string Address { get; set; } = string.Empty;
    
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PinCode { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Phone number is required")]
    [Phone(ErrorMessage = "Invalid phone number")]
    public string PhoneNumber { get; set; } = string.Empty;
    
    [EmailAddress(ErrorMessage = "Invalid email address")]
    public string Email { get; set; } = string.Empty;
    
    public string AadharNumber { get; set; } = string.Empty;
    
    // Incident Details
    [Required(ErrorMessage = "Incident date is required")]
    public DateTime IncidentDate { get; set; } = DateTime.Now;
    
    public TimeSpan IncidentTime { get; set; } = TimeSpan.Zero;
    
    [Required(ErrorMessage = "Incident location is required")]
    public string IncidentLocation { get; set; } = string.Empty;
    
    public string PoliceStation { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    
    // Crime Details
    [Required(ErrorMessage = "Please select crime type")]
    public FIRCrimeType CrimeType { get; set; } = FIRCrimeType.Other;
    
    public FIRCrimeSubType CrimeSubType { get; set; } = FIRCrimeSubType.Other;
    
    [Required(ErrorMessage = "Please describe the incident")]
    [MinLength(50, ErrorMessage = "Please provide at least 50 characters describing the incident")]
    public string IncidentDescription { get; set; } = string.Empty;
    
    // Accused Details (if known)
    public string AccusedName { get; set; } = string.Empty;
    public string AccusedDescription { get; set; } = string.Empty;
    public string AccusedAddress { get; set; } = string.Empty;
    public string AccusedPhone { get; set; } = string.Empty;
    public string AccusedRelation { get; set; } = string.Empty;
    
    // Financial Details (for fraud cases)
    public decimal? AmountLost { get; set; }
    public string BankName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string TransactionIds { get; set; } = string.Empty;
    public string UPIIds { get; set; } = string.Empty;
    
    // Evidence
    public List<string> EvidenceFiles { get; set; } = new();
    public List<string> WitnessNames { get; set; } = new();
    public string WitnessDetails { get; set; } = string.Empty;
    
    // AI Generated Content
    public string AIDraftedFIR { get; set; } = string.Empty;
    public List<string> ApplicableSections { get; set; } = new();
    public string LegalAnalysis { get; set; } = string.Empty;
    public string RecommendedActions { get; set; } = string.Empty;
    public string JurisdictionInfo { get; set; } = string.Empty;
    
    // Metadata
    public string Language { get; set; } = "en";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public FIRStatus Status { get; set; } = FIRStatus.Draft;
    public string UserId { get; set; } = string.Empty;
}

public enum FIRCrimeType
{
    [Display(Name = "Cyber Crime / Online Fraud")]
    CyberCrime,
    
    [Display(Name = "Financial Fraud")]
    FinancialFraud,
    
    [Display(Name = "Theft / Robbery")]
    Theft,
    
    [Display(Name = "Physical Assault")]
    Assault,
    
    [Display(Name = "Sexual Harassment")]
    SexualHarassment,
    
    [Display(Name = "Domestic Violence")]
    DomesticViolence,
    
    [Display(Name = "Property Dispute")]
    PropertyDispute,
    
    [Display(Name = "Cheating / Fraud")]
    Cheating,
    
    [Display(Name = "Extortion / Blackmail")]
    Extortion,
    
    [Display(Name = "Missing Person")]
    MissingPerson,
    
    [Display(Name = "Vehicle Theft")]
    VehicleTheft,
    
    [Display(Name = "Identity Theft")]
    IdentityTheft,
    
    [Display(Name = "Defamation")]
    Defamation,
    
    [Display(Name = "Stalking / Harassment")]
    Stalking,
    
    [Display(Name = "Other")]
    Other
}

public enum FIRCrimeSubType
{
    // Cyber Crime Sub-types
    [Display(Name = "Phishing Attack")]
    Phishing,
    
    [Display(Name = "Online Shopping Fraud")]
    OnlineShoppingFraud,
    
    [Display(Name = "Investment/Trading Scam")]
    InvestmentScam,
    
    [Display(Name = "Job Fraud")]
    JobFraud,
    
    [Display(Name = "Loan App Harassment")]
    LoanAppHarassment,
    
    [Display(Name = "Social Media Fraud")]
    SocialMediaFraud,
    
    [Display(Name = "OTP Fraud")]
    OTPFraud,
    
    [Display(Name = "Digital Arrest Scam")]
    DigitalArrest,
    
    [Display(Name = "Sextortion")]
    Sextortion,
    
    [Display(Name = "Matrimonial Fraud")]
    MatrimonialFraud,
    
    [Display(Name = "Crypto/NFT Scam")]
    CryptoScam,
    
    [Display(Name = "UPI Fraud")]
    UPIFraud,
    
    [Display(Name = "Card Cloning")]
    CardCloning,
    
    [Display(Name = "SIM Swap")]
    SIMSwap,
    
    [Display(Name = "Other")]
    Other
}

public enum FIRStatus
{
    Draft,
    Generated,
    ReadyToFile,
    Filed,
    Acknowledged,
    UnderInvestigation,
    Resolved,
    Closed
}

/// <summary>
/// Bank Account Freeze Request Model
/// </summary>
public class BankFreezeRequest
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    // Complainant Details
    public string ComplainantName { get; set; } = string.Empty;
    public string ComplainantAddress { get; set; } = string.Empty;
    public string ComplainantPhone { get; set; } = string.Empty;
    public string ComplainantEmail { get; set; } = string.Empty;
    public string ComplainantAccountNumber { get; set; } = string.Empty;
    public string ComplainantBankName { get; set; } = string.Empty;
    
    // Fraud Details
    public DateTime FraudDate { get; set; } = DateTime.Now;
    public decimal AmountDefrauded { get; set; }
    public string FraudDescription { get; set; } = string.Empty;
    public string TransactionReference { get; set; } = string.Empty;
    
    // Accused Bank Details
    public string AccusedBankName { get; set; } = string.Empty;
    public string AccusedAccountNumber { get; set; } = string.Empty;
    public string AccusedIFSC { get; set; } = string.Empty;
    public string AccusedAccountHolderName { get; set; } = string.Empty;
    public string AccusedUPIId { get; set; } = string.Empty;
    
    // Multiple Accounts (fraud often involves multiple)
    public List<FraudulentAccount> FraudulentAccounts { get; set; } = new();
    
    // Reference Numbers
    public string CyberCrimePortalNumber { get; set; } = string.Empty; // 1930 complaint number
    public string FIRNumber { get; set; } = string.Empty;
    public string PoliceStation { get; set; } = string.Empty;
    
    // Generated Documents
    public string GeneratedLetter { get; set; } = string.Empty;
    public string BankManagerAddress { get; set; } = string.Empty;
    public string NodalOfficerEmail { get; set; } = string.Empty;
    
    // Metadata
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string Language { get; set; } = "en";
    public BankFreezeStatus Status { get; set; } = BankFreezeStatus.Draft;
}

public class FraudulentAccount
{
    public string BankName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string IFSC { get; set; } = string.Empty;
    public string AccountHolderName { get; set; } = string.Empty;
    public string UPIId { get; set; } = string.Empty;
    public decimal AmountTransferred { get; set; }
    public DateTime TransactionDate { get; set; }
    public string TransactionId { get; set; } = string.Empty;
}

public enum BankFreezeStatus
{
    Draft,
    Generated,
    Sent,
    Acknowledged,
    AccountFrozen,
    AmountRecovered,
    Closed
}

/// <summary>
/// Case Link Model - Links related fraud cases
/// </summary>
public class CaseLink
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string SourceCaseId { get; set; } = string.Empty;
    public string LinkedCaseId { get; set; } = string.Empty;
    public CaseLinkType LinkType { get; set; }
    public double SimilarityScore { get; set; }
    public string LinkReason { get; set; } = string.Empty;
    public List<string> CommonElements { get; set; } = new();
    public DateTime LinkedAt { get; set; } = DateTime.UtcNow;
    public bool IsVerified { get; set; } = false;
}

public enum CaseLinkType
{
    SameAccused,
    SamePhoneNumber,
    SameBankAccount,
    SameUPIId,
    SameModus,
    SameWebsite,
    SameIPAddress,
    SimilarPattern,
    GeographicCluster,
    TemporalCluster
}

/// <summary>
/// Similar Case Match from AI Analysis
/// </summary>
public class SimilarCaseMatch
{
    public string CaseId { get; set; } = string.Empty;
    public string CaseTitle { get; set; } = string.Empty;
    public string CrimeType { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTime IncidentDate { get; set; }
    public decimal? AmountInvolved { get; set; }
    public double SimilarityScore { get; set; }
    public List<string> MatchingFactors { get; set; } = new();
    public string Status { get; set; } = string.Empty;
    public string Outcome { get; set; } = string.Empty;
}

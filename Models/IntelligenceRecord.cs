using System;
using System.Collections.Generic;

namespace AILegalAsst.Models;

/// <summary>
/// Represents complete intelligence gathered about a phone number or suspect
/// Contains data from all sources: Telecom, Banking, Social Media, Police
/// </summary>
public class IntelligenceRecord
{
    public int Id { get; set; }
    public string? PhoneNumber { get; set; }
    public string? SuspectName { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? LastUpdated { get; set; }
    
    // Intelligence Data from Different Sources
    public TelecomIntelligence? TelecomData { get; set; }
    public BankingIntelligence? BankingData { get; set; }
    public OSINTIntelligence? OSINTData { get; set; }
    public PoliceIntelligence? PoliceData { get; set; }
    
    // Linked Information
    public List<int> RelatedCaseIds { get; set; } = new();
    public List<int> LinkedPhoneIds { get; set; } = new();
    public List<int> LinkedSuspectIds { get; set; } = new();
    
    // Timeline
    public List<IntelligenceTimelineEvent> Timeline { get; set; } = new();
    
    // Analysis & Assessment
    public IntelligenceAssessment? Assessment { get; set; }
    public string? RiskLevel { get; set; } // Low, Medium, High, Critical
    public double? TrustScore { get; set; } // 0-100, based on data source reliability
    
    // Officer Notes
    public string? Notes { get; set; }
    public int? AssignedOfficerId { get; set; }
}

/// <summary>
/// Intelligence from Telecom Providers (CDR, Tower, SMS)
/// </summary>
public class TelecomIntelligence
{
    public int Id { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTime DataCollectionDate { get; set; }
    
    // Subscriber Info
    public string? SubscriberName { get; set; }
    public string? SubscriberAddress { get; set; }
    public string? Provider { get; set; } // Jio, Airtel, Vodafone, BSNL
    
    // Call Records
    public List<CallDetailRecord> CallRecords { get; set; } = new();
    public int TotalCalls { get; set; }
    public int TotalSMSs { get; set; }
    
    // Tower Data
    public List<TowerLocation> TowerLocations { get; set; } = new();
    
    // Communication Patterns
    public List<string> FrequentContacts { get; set; } = new(); // Top 10 contact numbers
    public Dictionary<string, int> CallFrequency { get; set; } = new(); // Phone -> call count
    public List<CommunicationPeak> PeakTimings { get; set; } = new();
    
    // Summary
    public DateTime? LatestActivityDate { get; set; }
    public string? LastKnownLocation { get; set; }
    public int? ApproximateDistanceFromPoliceStation { get; set; }
}

/// <summary>
/// Intelligence from Banking Sources
/// </summary>
public class BankingIntelligence
{
    public int Id { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTime DataCollectionDate { get; set; }
    
    // Account Information
    public List<BankAccount> Accounts { get; set; } = new();
    public string? PrimaryAccountNumber { get; set; }
    public string? PrimaryBank { get; set; }
    
    // Transaction Summary
    public decimal TotalIncomeLast30Days { get; set; }
    public decimal TotalExpenseLast30Days { get; set; }
    public decimal AverageTransactionAmount { get; set; }
    
    // Transaction Details
    public List<Transaction> Transactions { get; set; } = new();
    
    // Suspicious Patterns
    public List<SuspiciousTransaction> SuspiciousTransactions { get; set; } = new();
    public List<string> FrequentRecipients { get; set; } = new(); // Top recipient accounts
    public List<string> FrequentSenders { get; set; } = new(); // Top sender accounts
    
    // UPI/Digital Payments
    public List<UPITransaction> UPITransactions { get; set; } = new();
    
    // Risk Indicators
    public int SuspiciousTransactionCount { get; set; }
    public bool HasAccountFreeze { get; set; }
    public bool HasBeneficiaryAccount { get; set; }

    // AI Analysis
    public string? AiInsight { get; set; }
}

/// <summary>
/// Intelligence from Social Media and Online Sources (OSINT)
/// </summary>
public class OSINTIntelligence
{
    public int Id { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTime DataCollectionDate { get; set; }
    
    // Social Media Profiles
    public List<SocialMediaProfile> Profiles { get; set; } = new();
    
    // Public Information
    public List<string> UsedEmails { get; set; } = new();
    public List<string> UsedPhoneNumbers { get; set; } = new();
    public List<string> UsedUsernames { get; set; } = new();
    
    // Online Activities
    public List<SocialMediaPost> Posts { get; set; } = new();
    public List<OnlineConnection> OnlineConnections { get; set; } = new();
    
    // IP/Device Information
    public List<IPAddress> AssociatedIPAddresses { get; set; } = new();
    public List<string> UsedDevices { get; set; } = new();
    
    // Online Footprint
    public List<string> WebsitesVisited { get; set; } = new();
    public List<string> OnlineCommunities { get; set; } = new();
    
    // Risk Indicators
    public bool HasDuplicateAccounts { get; set; }
    public bool HasCybercriminalActivity { get; set; }
    public int NumberOfFakeProfiles { get; set; }

    // AI Analysis
    public string? AiInsight { get; set; }
}

/// <summary>
/// Intelligence from Police Database (Criminal Records)
/// </summary>
public class PoliceIntelligence
{
    public int Id { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTime DataCollectionDate { get; set; }
    
    // Criminal Record
    public List<PreviousArrest> PreviousArrests { get; set; } = new();
    public int TotalArrests { get; set; }
    
    // Cases
    public List<CaseReference> RelatedCases { get; set; } = new();
    public int TotalCases { get; set; }
    
    // Charges
    public List<string> ChargesAgainstSuspect { get; set; } = new();
    public List<string> ConvictionHistory { get; set; } = new();
    
    // Wanted Status
    public bool IsWanted { get; set; }
    public bool IsAbsconder { get; set; }
    
    // Known Associates
    public List<KnownAssociate> KnownAssociates { get; set; } = new();
    
    // Modus Operandi
    public List<string> KnownCrimeMethods { get; set; } = new();
    public string? CriminalSpecialty { get; set; } // e.g., "Financial Fraud", "Cybercrime"
    
    // Risk Assessment
    public bool IsDangerous { get; set; }
    public bool HasWeaponHistory { get; set; }
    public string? CriminalProfile { get; set; }
}

/// <summary>
/// A single call detail record from CDR data
/// </summary>
public class CallDetailRecord
{
    public int Id { get; set; }
    public string? CallerNumber { get; set; }
    public string? CalleeNumber { get; set; }
    public DateTime CallDateTime { get; set; }
    public int DurationSeconds { get; set; }
    public string? CallType { get; set; } // Voice, SMS, USSD
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? TowerLocation { get; set; }
    public string? TowerId { get; set; }
}

/// <summary>
/// Tower location data for geolocation
/// </summary>
public class TowerLocation
{
    public int Id { get; set; }
    public string? TowerId { get; set; }
    public string? Location { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public DateTime DetectionDateTime { get; set; }
    public int SignalStrength { get; set; }
}

/// <summary>
/// Communication pattern peaks (when most calls happen)
/// </summary>
public class CommunicationPeak
{
    public string? TimeOfDay { get; set; } // "Morning", "Afternoon", "Evening", "Night"
    public int CallCount { get; set; }
    public List<string> FrequentContactsInPeak { get; set; } = new();
}

/// <summary>
/// Bank account details
/// </summary>
public class BankAccount
{
    public int Id { get; set; }
    public string? AccountNumber { get; set; }
    public string? BankName { get; set; }
    public string? IFSC { get; set; }
    public string? AccountHolderName { get; set; }
    public decimal CurrentBalance { get; set; }
    public DateTime OpeningDate { get; set; }
    public bool IsActive { get; set; }
    public bool IsFrozen { get; set; }
}

/// <summary>
/// Financial transaction
/// </summary>
public class Transaction
{
    public int Id { get; set; }
    public DateTime TransactionDateTime { get; set; }
    public string? TransactionType { get; set; } // Credit, Debit
    public decimal Amount { get; set; }
    public string? SenderAccountNumber { get; set; }
    public string? ReceiverAccountNumber { get; set; }
    public string? SenderName { get; set; }
    public string? ReceiverName { get; set; }
    public string? Purpose { get; set; }
    public string? TransactionReference { get; set; }
    public string? Status { get; set; } // Success, Failed, Pending
}

/// <summary>
/// Suspicious transaction flagged by system
/// </summary>
public class SuspiciousTransaction
{
    public int Id { get; set; }
    public int TransactionId { get; set; }
    public string? SuspicionReason { get; set; }
    // e.g., "Round amount", "Multiple quick withdrawals", "New recipient", "After hours"
    public int SuspicionScore { get; set; } // 0-100
    public DateTime FlaggedDateTime { get; set; }
}

/// <summary>
/// UPI/Digital payment transaction
/// </summary>
public class UPITransaction
{
    public int Id { get; set; }
    public DateTime TransactionDateTime { get; set; }
    public string? PaymentApp { get; set; } // PayTM, GooglePay, PhonePe, WhatsAppPay
    public string? SenderUPI { get; set; }
    public string? ReceiverUPI { get; set; }
    public decimal Amount { get; set; }
    public string? Purpose { get; set; }
}

/// <summary>
/// Social media profile
/// </summary>
public class SocialMediaProfile
{
    public int Id { get; set; }
    public string? Platform { get; set; } // Facebook, Instagram, Twitter, Telegram, WhatsApp
    public string? Username { get; set; }
    public string? ProfileName { get; set; }
    public string? ProfileBio { get; set; }
    public int FollowerCount { get; set; }
    public int FollowingCount { get; set; }
    public DateTime ProfileCreationDate { get; set; }
    public bool IsVerified { get; set; }
    public string? ProfileImageURL { get; set; }
    public DateTime LastActivityDate { get; set; }
}

/// <summary>
/// Social media post
/// </summary>
public class SocialMediaPost
{
    public int Id { get; set; }
    public string? Platform { get; set; }
    public string? PostContent { get; set; }
    public DateTime PostDateTime { get; set; }
    public string? PostLocation { get; set; }
    public int LikeCount { get; set; }
    public int ShareCount { get; set; }
    public int CommentCount { get; set; }
}

/// <summary>
/// Online connection/friend relationship
/// </summary>
public class OnlineConnection
{
    public int Id { get; set; }
    public string? Platform { get; set; }
    public string? ConnectedUsername { get; set; }
    public string? ConnectionType { get; set; } // Friend, Follower, Subscriber, Member
    public DateTime ConnectedSinceDate { get; set; }
    public bool IsFrequentInteractor { get; set; }
}

/// <summary>
/// IP address associated with suspect
/// </summary>
public class IPAddress
{
    public int Id { get; set; }
    public string? IPAddressValue { get; set; }
    public string? Country { get; set; }
    public string? State { get; set; }
    public string? City { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? ISP { get; set; }
    public bool IsVPN { get; set; }
    public bool IsProxy { get; set; }
    public DateTime LastDetectedDate { get; set; }
}

/// <summary>
/// Previous arrest record
/// </summary>
public class PreviousArrest
{
    public int Id { get; set; }
    public DateTime ArrestDate { get; set; }
    public string? PoliceStation { get; set; }
    public string? CaseNumber { get; set; }
    public string? Charges { get; set; }
    public string? Outcome { get; set; } // Convicted, Acquitted, Bail
    public DateTime? ReleaseDate { get; set; }
}

/// <summary>
/// Reference to a case related to this intelligence
/// </summary>
public class CaseReference
{
    public int Id { get; set; }
    public int CaseId { get; set; }
    public string? CaseNumber { get; set; }
    public string? CaseTitle { get; set; }
    public string? RoleInCase { get; set; } // Suspect, Accused, Victim, Witness
    public DateTime CaseDate { get; set; }
    public string? CaseStatus { get; set; }
}

/// <summary>
/// Known associate of suspect
/// </summary>
public class KnownAssociate
{
    public int Id { get; set; }
    public string? AssociateName { get; set; }
    public string? AssociatePhone { get; set; }
    public string? RelationshipType { get; set; } // Contact, Accomplice, Friend, Family
    public string? CriminalActivity { get; set; }
    public bool IsSuspect { get; set; }
}

/// <summary>
/// Timeline event for intelligence
/// </summary>
public class IntelligenceTimelineEvent
{
    public int Id { get; set; }
    public DateTime EventDateTime { get; set; }
    public string? EventType { get; set; } // Call, SMS, Transaction, Arrest, Post, etc.
    public string? EventDescription { get; set; }
    public string? DataSource { get; set; } // Telecom, Banking, OSINT, Police
    public string? RelatedPhoneOrAccount { get; set; }
    public string? Location { get; set; }
}

/// <summary>
/// Overall assessment of intelligence
/// </summary>
public class IntelligenceAssessment
{
    public int Id { get; set; }
    public DateTime AssessmentDate { get; set; }
    
    // Summary
    public string? Summary { get; set; }
    public string? CriminalProfile { get; set; }
    
    // Confidence Scores
    public int IdentityConfidence { get; set; } // 0-100
    public int DataReliabilityScore { get; set; } // 0-100
    public int RiskScore { get; set; } // 0-100
    
    // Recommendations
    public List<string> InvestigationRecommendations { get; set; } = new();
    public List<string> ActionItems { get; set; } = new();
    
    // Next Steps
    public string? NextInvestigationStep { get; set; }
    public DateTime? RecommendedFollowUpDate { get; set; }
}

/// <summary>
/// Links between suspects
/// </summary>
public class SuspectLink
{
    public int Id { get; set; }
    public int Suspect1Id { get; set; }
    public int Suspect2Id { get; set; }
    public string? Suspect1Name { get; set; }
    public string? Suspect2Name { get; set; }
    public string? LinkType { get; set; } // Contact, Recipient, Associate, Family
    public string? Strength { get; set; } // Weak, Medium, Strong
    public int ConnectionCount { get; set; } // How many times they communicated
    public DateTime FirstConnectionDate { get; set; }
    public DateTime? LastConnectionDate { get; set; }
    public string? CommunicationPattern { get; set; }
}

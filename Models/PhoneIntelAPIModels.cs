using System;
using System.Collections.Generic;

namespace AILegalAsst.Models;

/// <summary>
/// Configuration for Phone Intelligence API
/// </summary>
public class PhoneIntelAPIConfig
{
    public string TelecomEndpoint { get; set; } = "";
    public string TelecomApiKey { get; set; } = "";
    
    public string BankingEndpoint { get; set; } = "";
    public string BankingApiKey { get; set; } = "";
    
    public string OSINTEndpoint { get; set; } = "";
    public string OSINTApiKey { get; set; } = "";
    
    public string PoliceDBEndpoint { get; set; } = "";
    public string PoliceDBApiKey { get; set; } = "";
    
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxRetries { get; set; } = 3;
    public int CacheDurationMinutes { get; set; } = 60;
    public bool UseMockData { get; set; } = true; // For development/testing
}

/// <summary>
/// Telecom API Response
/// </summary>
public class TelecomDataResponse
{
    public string PhoneNumber { get; set; } = "";
    public string RegistrationDate { get; set; } = "";
    public string CarrierName { get; set; } = "";
    public string OwnerName { get; set; } = "";
    public string Aadhaar { get; set; } = "";
    public int TotalCalls { get; set; }
    public int TotalSMS { get; set; }
    public int TotalMinutes { get; set; }
    public Dictionary<string, int> CallFrequency { get; set; } = new();
    public List<CallRecord> RecentCalls { get; set; } = new();
    public string? LastActiveDate { get; set; }
}

public class CallRecord
{
    public DateTime DateTime { get; set; }
    public string Type { get; set; } = ""; // Incoming/Outgoing
    public string OtherParty { get; set; } = "";
    public int DurationSeconds { get; set; }
    public string Location { get; set; } = "";
}

/// <summary>
/// Banking API Response
/// </summary>
public class BankingDataResponse
{
    public string PhoneNumber { get; set; } = "";
    public List<LinkedAccount> LinkedAccounts { get; set; } = new();
    public decimal TotalBalance { get; set; }
    public decimal TotalIncomeLast30Days { get; set; }
    public decimal TotalExpenseLast30Days { get; set; }
    public decimal AverageTransactionAmount { get; set; }
    public int SuspiciousTransactionCount { get; set; }
    public List<BankTransactionRecord> Transactions { get; set; } = new();
    public List<string> FrequentRecipients { get; set; } = new();
    public List<string> FrequentSenders { get; set; } = new();
    public bool HasAccountFreeze { get; set; }
}

public class LinkedAccount
{
    public string AccountNumber { get; set; } = "";
    public string BankName { get; set; } = "";
    public string AccountType { get; set; } = "";
    public decimal Balance { get; set; }
    public string Status { get; set; } = "";
}

public class BankTransactionRecord
{
    public DateTime DateTime { get; set; }
    public string Type { get; set; } = ""; // Credit/Debit
    public decimal Amount { get; set; }
    public string OtherParty { get; set; } = "";
    public string Description { get; set; } = "";
    public bool IsSuspicious { get; set; }
}

/// <summary>
/// OSINT API Response
/// </summary>
public class OSINTDataResponse
{
    public string PhoneNumber { get; set; } = "";
    public List<SocialProfile> Profiles { get; set; } = new();
    public List<string> EmailAddresses { get; set; } = new();
    public List<string> AlternatePhones { get; set; } = new();
    public List<string> Addresses { get; set; } = new();
    public List<string> Associates { get; set; } = new();
    public List<OnlineActivity> RecentActivities { get; set; } = new();
    public int RiskScore { get; set; }
}

public class SocialProfile
{
    public string Platform { get; set; } = "";
    public string Username { get; set; } = "";
    public string ProfileUrl { get; set; } = "";
    public string Bio { get; set; } = "";
    public int FollowerCount { get; set; }
    public DateTime LastActive { get; set; }
}

public class OnlineActivity
{
    public DateTime DateTime { get; set; }
    public string Platform { get; set; } = "";
    public string ActivityType { get; set; } = "";
    public string Description { get; set; } = "";
}

/// <summary>
/// Police Database API Response
/// </summary>
public class PoliceDataResponse
{
    public string PhoneNumber { get; set; } = "";
    public bool IsWanted { get; set; }
    public bool IsAbsconder { get; set; }
    public bool IsDangerous { get; set; }
    public int TotalArrests { get; set; }
    public int TotalCases { get; set; }
    public List<ArrestRecord> PreviousArrests { get; set; } = new();
    public List<CaseRecord> RelatedCases { get; set; } = new();
    public List<Conviction> ConvictionHistory { get; set; } = new();
    public List<string> KnownCrimeMethods { get; set; } = new();
    public string? CriminalSpecialty { get; set; }
    public string RiskLevel { get; set; } = "Unknown";
}

public class ArrestRecord
{
    public DateTime ArrestDate { get; set; }
    public string Charges { get; set; } = "";
    public string Location { get; set; } = "";
    public string ArrestingOfficer { get; set; } = "";
}

public class CaseRecord
{
    public string CaseNumber { get; set; } = "";
    public string CaseTitle { get; set; } = "";
    public string RoleInCase { get; set; } = "";
    public DateTime CaseDate { get; set; }
    public string CaseStatus { get; set; } = "";
}

public class Conviction
{
    public DateTime ConvictionDate { get; set; }
    public string Crime { get; set; } = "";
    public string Sentence { get; set; } = "";
    public string Court { get; set; } = "";
}

/// <summary>
/// API Response Wrapper
/// </summary>
public class APIResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public int StatusCode { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

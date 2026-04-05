namespace AILegalAsst.Models;

/// <summary>
/// Case deadline tracking based on BNSS (Bharatiya Nagarik Suraksha Sanhita) provisions
/// </summary>
public class CaseDeadline
{
    public string Id { get; set; } = string.Empty;
    public string CaseId { get; set; } = string.Empty;
    public string CaseTitle { get; set; } = string.Empty;
    public string CaseNumber { get; set; } = string.Empty;
    public string FIRNumber { get; set; } = string.Empty;
    
    // Deadline Details
    public BNSSDeadlineType DeadlineType { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string LegalProvision { get; set; } = string.Empty; // e.g., "Section 173 BNSS"
    
    // Dates
    public DateTime FIRDate { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? ExtendedDueDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    // Status & Priority
    public DeadlineStatus Status { get; set; }
    public DeadlinePriority Priority { get; set; }
    public int DaysRemaining => (DueDate - DateTime.Today).Days;
    public bool IsOverdue => DateTime.Today > DueDate && Status != DeadlineStatus.Completed;
    public bool IsUrgent => DaysRemaining <= 7 && DaysRemaining > 0;
    public bool IsCritical => DaysRemaining <= 3 && DaysRemaining > 0;
    
    // Extension Details
    public int ExtensionCount { get; set; }
    public int? ExtensionDays { get; set; }
    public string ExtensionReason { get; set; } = string.Empty;
    public string ExtensionOrderNumber { get; set; } = string.Empty;
    public DateTime? ExtensionGrantedDate { get; set; }
    
    // Responsibility
    public string AssignedOfficer { get; set; } = string.Empty;
    public string AssignedUnit { get; set; } = string.Empty;
    public string SupervisingOfficer { get; set; } = string.Empty;
    
    // Completion
    public string CompletionNotes { get; set; } = string.Empty;
    
    // Reminders
    public List<int> ReminderDays { get; set; } = new();
    public bool AlertsEnabled { get; set; } = true;
    
    // Notes
    public string Notes { get; set; } = string.Empty;
    public List<string> ActionsTaken { get; set; } = new();
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public enum DeadlineType
{
    // Investigation Deadlines
    Chargesheet_90Days,           // Section 173 BNSS - 90 days for offences < 10 years
    Chargesheet_60Days,           // Section 173 BNSS - 60 days for offences ≥ 10 years
    Chargesheet_180Days,          // For special cases under UAPA, NDPS, etc.
    
    // Remand Deadlines
    PoliceRemand_15Days,          // Section 167 BNSS - Max 15 days police custody
    JudicialRemand_60Days,        // Section 167 BNSS - Max 60 days for < 10 years
    JudicialRemand_90Days,        // Section 167 BNSS - Max 90 days for ≥ 10 years
    
    // Default Bail Entitlement
    DefaultBail_60Days,           // If chargesheet not filed within 60 days
    DefaultBail_90Days,           // If chargesheet not filed within 90 days
    
    // Other Deadlines
    FIR_Registration,             // Zero FIR must be transferred within 15 days
    InvestigationReport,          // Periodic investigation reports
    WitnessExamination,           // Witness examination timeline
    EvidencePreservation,         // Digital evidence preservation deadline
    CDR_Request,                  // CDR request validity
    BankFreeze_Extension,         // Bank account freeze renewal
    CourtHearing,                 // Next court hearing date
    BailHearing,                  // Bail hearing date
    ArgumentsSubmission,          // Written arguments deadline
    AppealFiling,                 // Appeal filing deadline
    Custom                        // User-defined deadline
}

// Alias enum for backwards compatibility
public enum BNSSDeadlineType
{
    Chargesheet_90Days,
    Chargesheet_60Days,
    Chargesheet_180Days,
    PoliceRemand_15Days,
    JudicialRemand_60Days,
    JudicialRemand_90Days,
    DefaultBail_60Days,
    DefaultBail_90Days,
    FIR_Registration,
    InvestigationReport,
    WitnessExamination,
    EvidencePreservation,
    CDR_Request,
    BankFreeze_Extension,
    CourtHearing,
    BailHearing,
    ArgumentsSubmission,
    AppealFiling,
    Custom
}

// Priority alias enum
public enum DeadlinePriority
{
    Low,
    Medium,
    High,
    Urgent,
    Critical
}

public enum DeadlineStatus
{
    Pending,
    InProgress,
    NearingDue,
    Overdue,
    ExtensionRequested,
    Extended,
    Completed,
    Cancelled,
    Waived,
    NotApplicable
}

/// <summary>
/// Alert/Notification for a deadline
/// </summary>
public class DeadlineAlert
{
    public string Id { get; set; } = string.Empty;
    public string DeadlineId { get; set; } = string.Empty;
    public string CaseId { get; set; } = string.Empty;
    public string CaseTitle { get; set; } = string.Empty;
    public string DeadlineType { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public int DaysRemaining { get; set; }
    public AlertSeverity Severity { get; set; }
    public string Message { get; set; } = string.Empty;
    public string AssignedOfficer { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
    public bool IsAcknowledged { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public string AcknowledgedBy { get; set; } = string.Empty;
}

public enum AlertSeverity
{
    Low,
    Medium,
    High,
    Critical
}

public enum AlertPriority
{
    Low,        // 30+ days
    Medium,     // 15-30 days
    High,       // 7-15 days
    Urgent,     // 3-7 days
    Critical    // <3 days or overdue
}

/// <summary>
/// BNSS deadline rules configuration
/// </summary>
public static class BNSSDeadlineRules
{
    public static readonly Dictionary<DeadlineType, DeadlineRule> Rules = new()
    {
        [DeadlineType.Chargesheet_90Days] = new DeadlineRule
        {
            Type = DeadlineType.Chargesheet_90Days,
            Title = "Chargesheet Filing",
            Days = 90,
            LegalProvision = "Section 173 BNSS",
            Description = "Chargesheet must be filed within 90 days for offences punishable with less than 10 years imprisonment",
            Consequence = "Accused entitled to default bail if not filed",
            AlertDays = new[] { 60, 45, 30, 15, 7, 3, 1 }
        },
        [DeadlineType.Chargesheet_60Days] = new DeadlineRule
        {
            Type = DeadlineType.Chargesheet_60Days,
            Title = "Chargesheet Filing (Serious Offences)",
            Days = 60,
            LegalProvision = "Section 173 BNSS",
            Description = "Chargesheet must be filed within 60 days for offences punishable with 10+ years or death",
            Consequence = "Accused entitled to default bail if not filed",
            AlertDays = new[] { 45, 30, 15, 7, 3, 1 }
        },
        [DeadlineType.PoliceRemand_15Days] = new DeadlineRule
        {
            Type = DeadlineType.PoliceRemand_15Days,
            Title = "Police Custody Remand",
            Days = 15,
            LegalProvision = "Section 167 BNSS",
            Description = "Maximum 15 days of police custody allowed",
            Consequence = "Must transfer to judicial custody",
            AlertDays = new[] { 10, 7, 5, 3, 1 }
        },
        [DeadlineType.JudicialRemand_60Days] = new DeadlineRule
        {
            Type = DeadlineType.JudicialRemand_60Days,
            Title = "Judicial Custody (Minor Offences)",
            Days = 60,
            LegalProvision = "Section 167 BNSS",
            Description = "Maximum 60 days judicial custody for offences < 10 years",
            Consequence = "Accused entitled to default bail",
            AlertDays = new[] { 45, 30, 15, 7, 3, 1 }
        },
        [DeadlineType.JudicialRemand_90Days] = new DeadlineRule
        {
            Type = DeadlineType.JudicialRemand_90Days,
            Title = "Judicial Custody (Serious Offences)",
            Days = 90,
            LegalProvision = "Section 167 BNSS",
            Description = "Maximum 90 days judicial custody for offences ≥ 10 years",
            Consequence = "Accused entitled to default bail",
            AlertDays = new[] { 60, 45, 30, 15, 7, 3, 1 }
        },
        [DeadlineType.FIR_Registration] = new DeadlineRule
        {
            Type = DeadlineType.FIR_Registration,
            Title = "Zero FIR Transfer",
            Days = 15,
            LegalProvision = "Section 173A BNSS",
            Description = "Zero FIR must be transferred to jurisdictional PS within 15 days",
            Consequence = "Disciplinary action against IO",
            AlertDays = new[] { 10, 7, 5, 3, 1 }
        },
        [DeadlineType.CDR_Request] = new DeadlineRule
        {
            Type = DeadlineType.CDR_Request,
            Title = "CDR Request Validity",
            Days = 180,
            LegalProvision = "Section 91 BNSS / IT Act",
            Description = "CDR request typically valid for 6 months, renewal needed",
            Consequence = "May need fresh court order",
            AlertDays = new[] { 30, 15, 7, 3 }
        },
        [DeadlineType.BankFreeze_Extension] = new DeadlineRule
        {
            Type = DeadlineType.BankFreeze_Extension,
            Title = "Bank Account Freeze Renewal",
            Days = 30,
            LegalProvision = "Section 102 BNSS",
            Description = "Bank account freeze typically needs renewal every 30 days",
            Consequence = "Freeze may be lifted",
            AlertDays = new[] { 15, 7, 5, 3, 1 }
        }
    };

    public static Dictionary<BNSSDeadlineType, DeadlineRule> GetAllRules()
    {
        var result = new Dictionary<BNSSDeadlineType, DeadlineRule>();
        foreach (var rule in Rules)
        {
            var bnssType = (BNSSDeadlineType)(int)rule.Key;
            result[bnssType] = rule.Value;
        }
        return result;
    }
}

/// <summary>
/// Rule definition for a deadline type
/// </summary>
public class DeadlineRule
{
    public DeadlineType Type { get; set; }
    public BNSSDeadlineType BNSSType { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Days { get; set; }
    public int DaysFromFIR { get; set; }
    public string LegalProvision { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Consequence { get; set; } = string.Empty;
    public string ConsequenceIfMissed => Consequence;
    public int[] AlertDays { get; set; } = Array.Empty<int>();
    public DeadlinePriority DefaultPriority { get; set; } = DeadlinePriority.High;
    public List<int> DefaultReminderDays { get; set; } = new() { 30, 15, 7, 3, 1 };
}

/// <summary>
/// Dashboard summary of all deadlines
/// </summary>
public class DeadlineDashboard
{
    public int TotalDeadlines { get; set; }
    public int PendingDeadlines { get; set; }
    public int CompletedDeadlines { get; set; }
    public int OverdueDeadlines { get; set; }
    public int ExtendedDeadlines { get; set; }
    public int DueTodayCount { get; set; }
    public int DueThisWeekCount { get; set; }
    public int CriticalAlerts { get; set; }
    public int HighAlerts { get; set; }
    public double ComplianceRate { get; set; }
    
    public List<CaseDeadline> UpcomingDeadlines { get; set; } = new();
    public List<CaseDeadline> OverdueList { get; set; } = new();
    public List<DeadlineAlert> RecentAlerts { get; set; } = new();
    
    public Dictionary<string, int> DeadlinesByType { get; set; } = new();
    public Dictionary<string, int> DeadlinesByStatus { get; set; } = new();
}

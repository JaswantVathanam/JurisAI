namespace AILegalAsst.Models;

/// <summary>
/// Represents a timeline event in a case's lifecycle
/// </summary>
public class CaseTimelineEvent
{
    public int Id { get; set; }
    public int CaseId { get; set; }
    public TimelineEventType EventType { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string? PerformedBy { get; set; }
    public string? PerformedByRole { get; set; }
    
    // For linking to related items
    public string? RelatedDocumentId { get; set; }
    public string? RelatedNoticeId { get; set; }
    
    // Milestone marker
    public bool IsMilestone { get; set; }
    public MilestoneType? MilestoneType { get; set; }
    
    // Status at this point
    public CaseStatus? StatusAtEvent { get; set; }
    
    // Additional metadata
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// Types of timeline events
/// </summary>
public enum TimelineEventType
{
    // Case lifecycle
    CaseCreated,
    CaseUpdated,
    StatusChanged,
    
    // FIR & Investigation
    FIRFiled,
    InvestigationStarted,
    EvidenceCollected,
    WitnessStatementRecorded,
    ForensicAnalysisCompleted,
    
    // Legal proceedings
    ChargesheetFiled,
    BailApplication,
    BailGranted,
    BailRejected,
    HearingScheduled,
    HearingCompleted,
    ArgumentsPresented,
    
    // Documents & Notices
    DocumentUploaded,
    LegalNoticeSent,
    NoticeResponseReceived,
    SummonsIssued,
    
    // Court decisions
    InterimOrder,
    JudgementPronounced,
    AppealFiled,
    
    // Case resolution
    CaseClosed,
    CaseDismissed,
    Conviction,
    Acquittal,
    Settlement,
    
    // Notes & Comments
    InternalNote,
    LawyerComment,
    PoliceUpdate,
    
    // Cybercrime specific
    DigitalEvidenceSecured,
    AccountFreezeRequested,
    AccountFrozen,
    IPTraceCompleted,
    SocialMediaTakedown
}

/// <summary>
/// Milestone types for key case events
/// </summary>
public enum MilestoneType
{
    CaseFiled,
    FIRRegistered,
    InvestigationComplete,
    ChargesheetSubmitted,
    TrialBegan,
    JudgementDelivered,
    CaseResolved
}

/// <summary>
/// Helper class to get event metadata
/// </summary>
public static class TimelineEventHelper
{
    public static (string Icon, string Color, string Label) GetEventDisplay(TimelineEventType eventType)
    {
        return eventType switch
        {
            // Case lifecycle
            TimelineEventType.CaseCreated => ("bi-folder-plus", "#10b981", "Case Created"),
            TimelineEventType.CaseUpdated => ("bi-pencil", "#6366f1", "Case Updated"),
            TimelineEventType.StatusChanged => ("bi-arrow-repeat", "#f59e0b", "Status Changed"),
            
            // FIR & Investigation
            TimelineEventType.FIRFiled => ("bi-file-earmark-text", "#ef4444", "FIR Filed"),
            TimelineEventType.InvestigationStarted => ("bi-search", "#8b5cf6", "Investigation Started"),
            TimelineEventType.EvidenceCollected => ("bi-collection", "#06b6d4", "Evidence Collected"),
            TimelineEventType.WitnessStatementRecorded => ("bi-person-lines-fill", "#84cc16", "Witness Statement"),
            TimelineEventType.ForensicAnalysisCompleted => ("bi-cpu", "#f97316", "Forensic Analysis"),
            
            // Legal proceedings
            TimelineEventType.ChargesheetFiled => ("bi-file-earmark-ruled", "#dc2626", "Chargesheet Filed"),
            TimelineEventType.BailApplication => ("bi-shield-check", "#0ea5e9", "Bail Application"),
            TimelineEventType.BailGranted => ("bi-check-circle", "#22c55e", "Bail Granted"),
            TimelineEventType.BailRejected => ("bi-x-circle", "#ef4444", "Bail Rejected"),
            TimelineEventType.HearingScheduled => ("bi-calendar-event", "#a855f7", "Hearing Scheduled"),
            TimelineEventType.HearingCompleted => ("bi-calendar-check", "#10b981", "Hearing Completed"),
            TimelineEventType.ArgumentsPresented => ("bi-chat-quote", "#6366f1", "Arguments Presented"),
            
            // Documents & Notices
            TimelineEventType.DocumentUploaded => ("bi-file-earmark-arrow-up", "#64748b", "Document Uploaded"),
            TimelineEventType.LegalNoticeSent => ("bi-envelope-paper", "#f59e0b", "Legal Notice Sent"),
            TimelineEventType.NoticeResponseReceived => ("bi-envelope-open", "#10b981", "Notice Response"),
            TimelineEventType.SummonsIssued => ("bi-person-badge", "#dc2626", "Summons Issued"),
            
            // Court decisions
            TimelineEventType.InterimOrder => ("bi-journal-text", "#8b5cf6", "Interim Order"),
            TimelineEventType.JudgementPronounced => ("bi-bank", "#059669", "Judgement"),
            TimelineEventType.AppealFiled => ("bi-arrow-up-circle", "#f97316", "Appeal Filed"),
            
            // Case resolution
            TimelineEventType.CaseClosed => ("bi-folder-x", "#64748b", "Case Closed"),
            TimelineEventType.CaseDismissed => ("bi-x-octagon", "#dc2626", "Case Dismissed"),
            TimelineEventType.Conviction => ("bi-exclamation-triangle", "#ef4444", "Conviction"),
            TimelineEventType.Acquittal => ("bi-check2-all", "#22c55e", "Acquittal"),
            TimelineEventType.Settlement => ("bi-handshake", "#10b981", "Settlement"),
            
            // Notes
            TimelineEventType.InternalNote => ("bi-sticky", "#94a3b8", "Internal Note"),
            TimelineEventType.LawyerComment => ("bi-chat-left-text", "#6366f1", "Lawyer Comment"),
            TimelineEventType.PoliceUpdate => ("bi-shield", "#0ea5e9", "Police Update"),
            
            // Cybercrime specific
            TimelineEventType.DigitalEvidenceSecured => ("bi-hdd", "#8b5cf6", "Digital Evidence Secured"),
            TimelineEventType.AccountFreezeRequested => ("bi-lock", "#f59e0b", "Account Freeze Requested"),
            TimelineEventType.AccountFrozen => ("bi-lock-fill", "#dc2626", "Account Frozen"),
            TimelineEventType.IPTraceCompleted => ("bi-globe", "#06b6d4", "IP Trace Completed"),
            TimelineEventType.SocialMediaTakedown => ("bi-trash", "#ef4444", "Social Media Takedown"),
            
            _ => ("bi-circle", "#64748b", "Event")
        };
    }
    
    public static (string Icon, string Color) GetMilestoneDisplay(MilestoneType milestoneType)
    {
        return milestoneType switch
        {
            MilestoneType.CaseFiled => ("bi-flag-fill", "#10b981"),
            MilestoneType.FIRRegistered => ("bi-file-earmark-text-fill", "#ef4444"),
            MilestoneType.InvestigationComplete => ("bi-search", "#8b5cf6"),
            MilestoneType.ChargesheetSubmitted => ("bi-file-earmark-ruled-fill", "#dc2626"),
            MilestoneType.TrialBegan => ("bi-bank2", "#f59e0b"),
            MilestoneType.JudgementDelivered => ("bi-patch-check-fill", "#059669"),
            MilestoneType.CaseResolved => ("bi-check-circle-fill", "#22c55e"),
            _ => ("bi-star-fill", "#f59e0b")
        };
    }
}

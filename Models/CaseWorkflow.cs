namespace AILegalAsst.Models;

/// <summary>
/// Represents a workflow step in the case lifecycle
/// </summary>
public class CaseWorkflowStep
{
    public string Stage { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending"; // Pending, InProgress, Completed
    public DateTime? Date { get; set; }
    public string Actor { get; set; } = string.Empty;
    public string ActorRole { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    
    // AI Action Attribution
    public bool IsAIInitiated { get; set; }
    public string? AIActionHash { get; set; }
    public string? VerifiedByUserId { get; set; }
}

/// <summary>
/// Standard workflow stages for case progression
/// </summary>
public static class WorkflowStages
{
    public const string Filed = "Filed";
    public const string FIRRegistered = "FIR Registered";
    public const string Investigation = "Investigation";
    public const string EvidenceCollection = "Evidence Collection";
    public const string ChargesheetFiled = "Chargesheet Filed";
    public const string LawyerAssigned = "Lawyer Assigned";
    public const string CourtHearing = "Court Hearing";
    public const string Judgement = "Judgement";
    public const string Appeal = "Appeal";
    public const string CaseClosed = "Case Closed";

    public static List<string> GetStandardFlow()
    {
        return new List<string>
        {
            Filed,
            FIRRegistered,
            Investigation,
            EvidenceCollection,
            ChargesheetFiled,
            LawyerAssigned,
            CourtHearing,
            Judgement,
            CaseClosed
        };
    }

    public static string GetRoleForStage(string stage)
    {
        return stage switch
        {
            Filed => "Citizen",
            FIRRegistered => "Police",
            Investigation => "Police",
            EvidenceCollection => "Police",
            ChargesheetFiled => "Police",
            LawyerAssigned => "Lawyer",
            CourtHearing => "Court",
            Judgement => "Court",
            Appeal => "Lawyer",
            CaseClosed => "Court",
            _ => "System"
        };
    }
}

/// <summary>
/// Case data transfer object for JSON serialization
/// </summary>
public class CaseDto
{
    public int Id { get; set; }
    public string CaseNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime FiledDate { get; set; }
    public DateTime? LastUpdated { get; set; }
    public string Complainant { get; set; } = string.Empty;
    public string ComplainantEmail { get; set; } = string.Empty;
    public string Accused { get; set; } = string.Empty;
    public string? Plaintiff { get; set; }
    public string? Defendant { get; set; }
    public string? LawyerName { get; set; }
    public string? Court { get; set; }
    public DateTime? NextHearingDate { get; set; }
    public List<string> ApplicableLaws { get; set; } = new();
    public List<string> Sections { get; set; } = new();
    public string? FirNumber { get; set; }
    public string? PoliceStation { get; set; }
    public string? AssignedLawyer { get; set; }
    public string? InvestigatingOfficer { get; set; }
    public bool IsCybercrime { get; set; }
    public string? CybercrimeCategory { get; set; }
    public string? DigitalEvidence { get; set; }
    public bool DigitalEvidenceCollected { get; set; }
    public string? AIAnalysis { get; set; }
    public double? SuccessProbability { get; set; }
    public List<CaseWorkflowStep> Workflow { get; set; } = new();
    
    // AI Action Attribution
    public bool FiledViaAI { get; set; }
    public string? AIActionHash { get; set; }
    public int? FiledByUserId { get; set; }
    public string? FiledByUserEmail { get; set; }
    public string? IdentityVerificationMethod { get; set; }
    public DateTime? IdentityVerifiedAt { get; set; }
    public string? DeviceFingerprint { get; set; }
    public string? FilingSessionId { get; set; }
}

/// <summary>
/// Root object for cases JSON file
/// </summary>
public class CasesJsonRoot
{
    public List<CaseDto> Cases { get; set; } = new();
    public DateTime LastUpdated { get; set; }
    public int NextId { get; set; }
}

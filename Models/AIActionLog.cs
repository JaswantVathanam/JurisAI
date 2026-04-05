using System.Text.Json.Serialization;

namespace AILegalAsst.Models;

/// <summary>
/// Audit trail for every AI-initiated legal action (case filing, FIR generation, etc.)
/// Provides non-repudiation: proves WHO authorized WHAT action and WHEN
/// </summary>
public class AIActionLog
{
    public int Id { get; set; }
    public string ActionId { get; set; } = Guid.NewGuid().ToString();
    
    // Who authorized this action
    public int UserId { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public UserRole UserRole { get; set; }
    
    // Session binding
    public string SessionId { get; set; } = string.Empty;
    public string DeviceFingerprint { get; set; } = string.Empty;
    public string? IPAddress { get; set; }
    public string? UserAgent { get; set; }
    
    // What was done
    public AIActionType ActionType { get; set; }
    public string ActionDescription { get; set; } = string.Empty;
    public string? RelatedCaseNumber { get; set; }
    public int? RelatedCaseId { get; set; }
    
    // Identity verification at time of action
    public IdentityVerificationMethod VerificationMethod { get; set; }
    public DateTime VerifiedAt { get; set; }
    public bool IdentityConfirmed { get; set; }
    
    // Timestamps
    public DateTime ActionTimestamp { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    
    // Result
    public bool ActionSucceeded { get; set; }
    public string? FailureReason { get; set; }
    
    // Digital signature (hash of userId + actionId + timestamp + fingerprint)
    public string ActionHash { get; set; } = string.Empty;
}

public enum AIActionType
{
    CaseFiled,
    CaseUpdated,
    FIRGenerated,
    EvidenceSubmitted,
    LegalNoticeGenerated,
    DocumentGenerated,
    CaseStatusChanged,
    CaseAssigned
}

public enum IdentityVerificationMethod
{
    None,
    PasswordReEntry,
    SessionPIN,
    TwoFactorOTP,
    BiometricConfirmation
}

/// <summary>
/// Detected device/platform profile from the browser — used for platform-aware security
/// </summary>
public class DeviceProfile
{
    public string Fingerprint { get; set; } = string.Empty;
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public DevicePlatform Platform { get; set; } = DevicePlatform.Unknown;
    public string Browser { get; set; } = string.Empty;
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public DeviceType DeviceType { get; set; } = DeviceType.Desktop;
    public string ScreenResolution { get; set; } = string.Empty;
    public double PixelRatio { get; set; } = 1;
    public string Timezone { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public bool TouchCapable { get; set; }
    public int TouchPoints { get; set; }
    public bool BiometricSupported { get; set; }
    public int HardwareConcurrency { get; set; }
    public int ColorDepth { get; set; }
    public string PlatformSignals { get; set; } = string.Empty;
}

public enum DevicePlatform
{
    Unknown,
    Windows,
    macOS,
    Android,
    iOS,
    Linux
}

public enum DeviceType
{
    Desktop,
    Mobile,
    Tablet
}

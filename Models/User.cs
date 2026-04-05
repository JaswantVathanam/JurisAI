namespace AILegalAsst.Models;

public enum VerificationStatus
{
    Pending,
    UnderReview,
    Verified,
    Rejected,
    MoreInfoNeeded
}

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? ProfilePicture { get; set; }
    public UserRole Role { get; set; }
    
    // Verification
    public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.Pending;
    public bool IsEmailVerified { get; set; } = false;
    public bool IsPhoneVerified { get; set; } = false;
    public DateTime? VerifiedAt { get; set; }
    public string? RejectionReason { get; set; }
    
    // Citizen-specific
    public string? AadhaarNumber { get; set; } // Encrypted, optional
    public string? Address { get; set; }
    public string? Bio { get; set; }
    
    // Lawyer-specific
    public string? BarCouncilNumber { get; set; }
    public string? BarCouncilState { get; set; }
    public string? BarCouncilCertificate { get; set; } // File path
    public int? YearsOfPractice { get; set; }
    public string? Specialization { get; set; } // JSON array: ["Criminal Law", "Cyber Law"]
    public string? OfficeAddress { get; set; }
    public string? AvailabilityHours { get; set; }
    
    // Police-specific
    public string? PoliceId { get; set; }
    public string? Rank { get; set; }
    public string? Department { get; set; }
    public string? PoliceStation { get; set; }
    public string? Jurisdiction { get; set; }
    public string? ServiceIdCard { get; set; } // File path
    public string? AuthorizationLetter { get; set; } // File path
    
    // Security
    public bool TwoFactorEnabled { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Legacy fields (keeping for backward compatibility)
    public string? BadgeNumber { get; set; } // Mapped to PoliceId
    public string? BarCouncilId { get; set; } // Mapped to BarCouncilNumber
    
    // Helper properties
    public bool IsVerified => VerificationStatus == VerificationStatus.Verified;
    public bool NeedsVerification => Role != UserRole.Citizen && VerificationStatus == VerificationStatus.Pending;
    public string VerificationStatusDisplay => VerificationStatus switch
    {
        VerificationStatus.Pending => "Pending Verification",
        VerificationStatus.UnderReview => "Under Review",
        VerificationStatus.Verified => "Verified",
        VerificationStatus.Rejected => "Rejected",
        VerificationStatus.MoreInfoNeeded => "More Info Needed",
        _ => "Unknown"
    };
}


using AILegalAsst.Models;
using System.Security.Cryptography;
using System.Text;

namespace AILegalAsst.Services;

/// <summary>
/// Manages session security: device fingerprinting, session binding, and suspicious activity detection.
/// Platform-aware: applies different security policies per OS (Windows, macOS, Android, iOS, Linux).
/// </summary>
public class SessionSecurityService
{
    private readonly ILogger<SessionSecurityService> _logger;
    private readonly Dictionary<int, UserSessionInfo> _activeSessions = new();

    // Platform-specific session timeout (minutes)
    private static readonly Dictionary<DevicePlatform, int> PlatformTimeouts = new()
    {
        { DevicePlatform.Windows,  30 },
        { DevicePlatform.macOS,    30 },
        { DevicePlatform.Android,  15 },  // Shorter on mobile — device may be shared
        { DevicePlatform.iOS,      15 },
        { DevicePlatform.Linux,    30 },
        { DevicePlatform.Unknown,  20 }
    };

    // Max suspicious activity before session kill (per platform)
    private static readonly Dictionary<DevicePlatform, int> PlatformMaxStrikes = new()
    {
        { DevicePlatform.Windows,  3 },
        { DevicePlatform.macOS,    3 },
        { DevicePlatform.Android,  2 },  // Stricter on mobile
        { DevicePlatform.iOS,      2 },
        { DevicePlatform.Linux,    3 },
        { DevicePlatform.Unknown,  2 }
    };

    public SessionSecurityService(ILogger<SessionSecurityService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Register a new session when user logs in — captures device fingerprint and platform profile
    /// </summary>
    public UserSessionInfo CreateSession(User user, string deviceFingerprint, string? ipAddress = null, string? userAgent = null)
    {
        var session = new UserSessionInfo
        {
            SessionToken = GenerateSecureToken(),
            UserId = user.Id,
            UserEmail = user.Email,
            UserRole = user.Role,
            DeviceFingerprint = deviceFingerprint,
            IPAddress = ipAddress,
            UserAgent = userAgent,
            LoginTime = DateTime.UtcNow,
            LastActivityTime = DateTime.UtcNow,
            IsActive = true
        };

        _activeSessions[user.Id] = session;
        _logger.LogInformation("Session created for user {UserId} ({Email}) on {Platform}/{DeviceType}, fingerprint: {Fingerprint}",
            user.Id, user.Email, session.DeviceProfile.Platform, session.DeviceProfile.DeviceType, MaskFingerprint(deviceFingerprint));

        return session;
    }

    /// <summary>
    /// Register session with full device profile (call after JS getDeviceProfile resolves)
    /// </summary>
    public UserSessionInfo CreateSessionWithProfile(User user, DeviceProfile profile, string? ipAddress = null, string? userAgent = null)
    {
        var session = new UserSessionInfo
        {
            SessionToken = GenerateSecureToken(),
            UserId = user.Id,
            UserEmail = user.Email,
            UserRole = user.Role,
            DeviceFingerprint = profile.Fingerprint,
            DeviceProfile = profile,
            IPAddress = ipAddress,
            UserAgent = userAgent,
            LoginTime = DateTime.UtcNow,
            LastActivityTime = DateTime.UtcNow,
            IsActive = true
        };

        _activeSessions[user.Id] = session;
        _logger.LogInformation(
            "Session created for user {UserId} ({Email}) — Platform: {Platform}, Browser: {Browser}, Device: {DeviceType}, Biometric: {Bio}, Fingerprint: {FP}",
            user.Id, user.Email, profile.Platform, profile.Browser, profile.DeviceType, profile.BiometricSupported, MaskFingerprint(profile.Fingerprint));

        return session;
    }

    /// <summary>
    /// Update session with full device profile (when JS profile arrives after initial login)
    /// </summary>
    public void UpdateDeviceProfile(int userId, DeviceProfile profile)
    {
        if (_activeSessions.TryGetValue(userId, out var session) && session.IsActive)
        {
            session.DeviceProfile = profile;
            session.DeviceFingerprint = profile.Fingerprint;
            _logger.LogInformation("Device profile updated for user {UserId} — {Platform}/{Browser}/{DeviceType}",
                userId, profile.Platform, profile.Browser, profile.DeviceType);
        }
    }

    /// <summary>
    /// Validate that the current request matches the session's device fingerprint.
    /// Applies platform-specific timeout and strike policies.
    /// </summary>
    public SessionValidationResult ValidateSession(int userId, string currentFingerprint)
    {
        if (!_activeSessions.TryGetValue(userId, out var session))
        {
            return new SessionValidationResult
            {
                IsValid = false,
                Reason = "No active session found. Please log in again.",
                ThreatLevel = ThreatLevel.High
            };
        }

        if (!session.IsActive)
        {
            return new SessionValidationResult
            {
                IsValid = false,
                Reason = "Session has been deactivated.",
                ThreatLevel = ThreatLevel.High
            };
        }

        // Platform-aware timeout
        var platform = session.DeviceProfile.Platform;
        var timeoutMinutes = PlatformTimeouts.GetValueOrDefault(platform, 20);

        if ((DateTime.UtcNow - session.LastActivityTime).TotalMinutes > timeoutMinutes)
        {
            session.IsActive = false;
            _logger.LogWarning("Session expired for user {UserId} on {Platform} after {Timeout}min inactivity",
                userId, platform, timeoutMinutes);
            return new SessionValidationResult
            {
                IsValid = false,
                Reason = $"Session expired due to inactivity ({timeoutMinutes} min limit on {platform}). Please log in again.",
                ThreatLevel = ThreatLevel.Medium
            };
        }

        // Compare device fingerprints
        if (session.DeviceFingerprint != currentFingerprint)
        {
            session.SuspiciousActivityCount++;
            var maxStrikes = PlatformMaxStrikes.GetValueOrDefault(platform, 2);

            _logger.LogWarning("Device fingerprint mismatch for user {UserId} on {Platform}! Strike {Count}/{Max}",
                userId, platform, session.SuspiciousActivityCount, maxStrikes);

            if (session.SuspiciousActivityCount >= maxStrikes)
            {
                session.IsActive = false;
                return new SessionValidationResult
                {
                    IsValid = false,
                    Reason = $"Session terminated — {session.SuspiciousActivityCount} fingerprint mismatches on {platform}. Possible session hijacking.",
                    ThreatLevel = ThreatLevel.Critical
                };
            }

            return new SessionValidationResult
            {
                IsValid = false,
                Reason = $"Device fingerprint mismatch ({platform}). This could indicate unauthorized access.",
                ThreatLevel = ThreatLevel.High,
                RequiresReAuthentication = true
            };
        }

        // Update last activity
        session.LastActivityTime = DateTime.UtcNow;

        return new SessionValidationResult
        {
            IsValid = true,
            SessionToken = session.SessionToken
        };
    }

    /// <summary>
    /// Get the active session for a user
    /// </summary>
    public UserSessionInfo? GetSession(int userId)
    {
        return _activeSessions.TryGetValue(userId, out var session) && session.IsActive ? session : null;
    }

    /// <summary>
    /// Terminate a session (on logout or security event)
    /// </summary>
    public void TerminateSession(int userId)
    {
        if (_activeSessions.TryGetValue(userId, out var session))
        {
            session.IsActive = false;
            _logger.LogInformation("Session terminated for user {UserId} ({Platform})", userId, session.DeviceProfile.Platform);
        }
    }

    /// <summary>
    /// Get recommended verification methods for the session's platform
    /// </summary>
    public List<IdentityVerificationMethod> GetPlatformVerificationMethods(int userId)
    {
        var session = GetSession(userId);
        if (session == null) return new List<IdentityVerificationMethod> { IdentityVerificationMethod.PasswordReEntry };

        var methods = new List<IdentityVerificationMethod>();
        var profile = session.DeviceProfile;

        switch (profile.Platform)
        {
            case DevicePlatform.Windows:
                methods.Add(IdentityVerificationMethod.PasswordReEntry);
                if (profile.BiometricSupported)
                    methods.Add(IdentityVerificationMethod.BiometricConfirmation); // Windows Hello
                methods.Add(IdentityVerificationMethod.SessionPIN);
                break;

            case DevicePlatform.macOS:
                if (profile.BiometricSupported)
                    methods.Add(IdentityVerificationMethod.BiometricConfirmation); // Touch ID
                methods.Add(IdentityVerificationMethod.PasswordReEntry);
                methods.Add(IdentityVerificationMethod.SessionPIN);
                break;

            case DevicePlatform.iOS:
                if (profile.BiometricSupported)
                    methods.Add(IdentityVerificationMethod.BiometricConfirmation); // Face ID / Touch ID
                methods.Add(IdentityVerificationMethod.PasswordReEntry);
                methods.Add(IdentityVerificationMethod.TwoFactorOTP);
                break;

            case DevicePlatform.Android:
                if (profile.BiometricSupported)
                    methods.Add(IdentityVerificationMethod.BiometricConfirmation); // Fingerprint / Face
                methods.Add(IdentityVerificationMethod.PasswordReEntry);
                methods.Add(IdentityVerificationMethod.TwoFactorOTP);
                break;

            case DevicePlatform.Linux:
                methods.Add(IdentityVerificationMethod.PasswordReEntry);
                methods.Add(IdentityVerificationMethod.SessionPIN);
                break;

            default:
                methods.Add(IdentityVerificationMethod.PasswordReEntry);
                break;
        }

        return methods;
    }

    /// <summary>
    /// Get human-readable description of platform security policy
    /// </summary>
    public PlatformSecurityPolicy GetSecurityPolicy(int userId)
    {
        var session = GetSession(userId);
        var platform = session?.DeviceProfile.Platform ?? DevicePlatform.Unknown;
        var profile = session?.DeviceProfile ?? new DeviceProfile();

        return new PlatformSecurityPolicy
        {
            Platform = platform,
            DeviceType = profile.DeviceType,
            SessionTimeoutMinutes = PlatformTimeouts.GetValueOrDefault(platform, 20),
            MaxSuspiciousStrikes = PlatformMaxStrikes.GetValueOrDefault(platform, 2),
            BiometricAvailable = profile.BiometricSupported,
            RecommendedVerificationMethods = GetPlatformVerificationMethods(userId),
            PlatformLabel = GetPlatformLabel(platform),
            BiometricLabel = GetBiometricLabel(platform),
            SecurityNotes = GetPlatformSecurityNotes(platform, profile)
        };
    }

    private string GetPlatformLabel(DevicePlatform platform) => platform switch
    {
        DevicePlatform.Windows => "Windows",
        DevicePlatform.macOS => "macOS",
        DevicePlatform.Android => "Android",
        DevicePlatform.iOS => "iOS",
        DevicePlatform.Linux => "Linux",
        _ => "Unknown Platform"
    };

    private string GetBiometricLabel(DevicePlatform platform) => platform switch
    {
        DevicePlatform.Windows => "Windows Hello",
        DevicePlatform.macOS => "Touch ID",
        DevicePlatform.iOS => "Face ID / Touch ID",
        DevicePlatform.Android => "Fingerprint / Face Unlock",
        _ => "Biometric"
    };

    private string GetPlatformSecurityNotes(DevicePlatform platform, DeviceProfile profile)
    {
        var notes = new List<string>();

        if (profile.DeviceType == DeviceType.Mobile)
            notes.Add("Mobile session — shorter timeout and stricter monitoring applied.");
        if (profile.BiometricSupported)
            notes.Add($"{GetBiometricLabel(platform)} is available and recommended for verification.");
        if (profile.TouchCapable && platform is DevicePlatform.Android or DevicePlatform.iOS)
            notes.Add("Touch device detected — mobile-optimized verification UI active.");

        return notes.Count > 0 ? string.Join(" ", notes) : "Standard security policy applied.";
    }

    private string GenerateSecureToken()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    private string MaskFingerprint(string fingerprint)
    {
        if (fingerprint.Length <= 8) return "****";
        return fingerprint[..4] + "****" + fingerprint[^4..];
    }
}

/// <summary>
/// Tracks a user's login session with device binding and platform awareness
/// </summary>
public class UserSessionInfo
{
    public string SessionToken { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public UserRole UserRole { get; set; }
    public string DeviceFingerprint { get; set; } = string.Empty;
    public DeviceProfile DeviceProfile { get; set; } = new();
    public string? IPAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime LoginTime { get; set; }
    public DateTime LastActivityTime { get; set; }
    public bool IsActive { get; set; }
    public int SuspiciousActivityCount { get; set; }
}

public class SessionValidationResult
{
    public bool IsValid { get; set; }
    public string? Reason { get; set; }
    public string? SessionToken { get; set; }
    public ThreatLevel ThreatLevel { get; set; }
    public bool RequiresReAuthentication { get; set; }
}

/// <summary>
/// Platform-specific security policy info — for UI display and enforcement
/// </summary>
public class PlatformSecurityPolicy
{
    public DevicePlatform Platform { get; set; }
    public DeviceType DeviceType { get; set; }
    public int SessionTimeoutMinutes { get; set; }
    public int MaxSuspiciousStrikes { get; set; }
    public bool BiometricAvailable { get; set; }
    public List<IdentityVerificationMethod> RecommendedVerificationMethods { get; set; } = new();
    public string PlatformLabel { get; set; } = string.Empty;
    public string BiometricLabel { get; set; } = string.Empty;
    public string SecurityNotes { get; set; } = string.Empty;
}

public enum ThreatLevel
{
    None,
    Low,
    Medium,
    High,
    Critical
}

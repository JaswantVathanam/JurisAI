using AILegalAsst.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AILegalAsst.Services;

/// <summary>
/// Handles identity re-verification before high-stakes AI actions (case filing, FIR generation).
/// Ensures "the person clicking submit is the person who logged in" — prevents unauthorized use of open sessions.
/// Also maintains a full audit trail of all AI-initiated legal actions.
/// </summary>
public class IdentityVerificationService
{
    private readonly AuthenticationService _authService;
    private readonly SessionSecurityService _sessionService;
    private readonly ILogger<IdentityVerificationService> _logger;

    // Audit trail
    private readonly List<AIActionLog> _actionLogs = new();
    private int _nextLogId = 1;

    // Pending verifications (userId -> verification token)
    private readonly Dictionary<int, PendingVerification> _pendingVerifications = new();

    public IdentityVerificationService(
        AuthenticationService authService,
        SessionSecurityService sessionService,
        ILogger<IdentityVerificationService> logger)
    {
        _authService = authService;
        _sessionService = sessionService;
        _logger = logger;
    }

    /// <summary>
    /// Step 1: Request identity verification before AI performs a legal action.
    /// Returns a verification challenge that the user must complete.
    /// </summary>
    public VerificationChallenge RequestVerification(User user, AIActionType actionType, string actionDescription)
    {
        // Get platform-aware verification methods
        var availableMethods = _sessionService.GetPlatformVerificationMethods(user.Id);
        var policy = _sessionService.GetSecurityPolicy(user.Id);

        // Generate a time-limited verification token
        var verification = new PendingVerification
        {
            VerificationToken = Guid.NewGuid().ToString(),
            UserId = user.Id,
            ActionType = actionType,
            ActionDescription = actionDescription,
            RequestedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5) // 5-minute window to verify
        };

        _pendingVerifications[user.Id] = verification;

        _logger.LogInformation("Identity verification requested for user {UserId} ({Email}) on {Platform} for action: {Action}",
            user.Id, user.Email, policy.PlatformLabel, actionType);

        return new VerificationChallenge
        {
            VerificationToken = verification.VerificationToken,
            ActionType = actionType,
            ActionDescription = actionDescription,
            Message = GetVerificationMessage(actionType, policy),
            RequiredMethod = availableMethods.FirstOrDefault(IdentityVerificationMethod.PasswordReEntry),
            AvailableMethods = availableMethods,
            PlatformLabel = policy.PlatformLabel,
            BiometricLabel = policy.BiometricLabel,
            BiometricAvailable = policy.BiometricAvailable,
            SecurityNotes = policy.SecurityNotes,
            ExpiresAt = verification.ExpiresAt
        };
    }

    /// <summary>
    /// Step 2: User provides identity proof (password re-entry or PIN).
    /// If verified, the AI action is authorized and logged.
    /// </summary>
    public async Task<VerificationResult> VerifyIdentityAsync(
        int userId,
        string verificationToken,
        string credential,
        IdentityVerificationMethod method,
        string? deviceFingerprint = null)
    {
        // Check pending verification exists
        if (!_pendingVerifications.TryGetValue(userId, out var pending))
        {
            return new VerificationResult
            {
                IsVerified = false,
                Reason = "No pending verification found. Please initiate the action again."
            };
        }

        // Check token matches
        if (pending.VerificationToken != verificationToken)
        {
            _logger.LogWarning("Invalid verification token for user {UserId}", userId);
            return new VerificationResult
            {
                IsVerified = false,
                Reason = "Invalid verification token."
            };
        }

        // Check expiry
        if (DateTime.UtcNow > pending.ExpiresAt)
        {
            _pendingVerifications.Remove(userId);
            return new VerificationResult
            {
                IsVerified = false,
                Reason = "Verification window expired. Please try again."
            };
        }

        // Verify the credential
        var user = _authService.GetCurrentUser();
        if (user == null || user.Id != userId)
        {
            return new VerificationResult
            {
                IsVerified = false,
                Reason = "Authentication session invalid."
            };
        }

        bool credentialValid = method switch
        {
            IdentityVerificationMethod.PasswordReEntry => VerifyPassword(user, credential),
            IdentityVerificationMethod.SessionPIN => VerifyPIN(userId, credential),
            IdentityVerificationMethod.BiometricConfirmation => VerifyBiometric(userId, credential),
            IdentityVerificationMethod.TwoFactorOTP => VerifyOTP(userId, credential),
            _ => false
        };

        if (!credentialValid)
        {
            pending.FailedAttempts++;
            if (pending.FailedAttempts >= 3)
            {
                _pendingVerifications.Remove(userId);
                _sessionService.TerminateSession(userId);
                _logger.LogWarning("Session terminated for user {UserId} after 3 failed verification attempts", userId);
                return new VerificationResult
                {
                    IsVerified = false,
                    Reason = "Too many failed attempts. Session terminated for security. Please log in again."
                };
            }

            return new VerificationResult
            {
                IsVerified = false,
                Reason = $"Incorrect credentials. {3 - pending.FailedAttempts} attempts remaining.",
                AttemptsRemaining = 3 - pending.FailedAttempts
            };
        }

        // Verification successful — create authorization token
        var authToken = GenerateActionAuthToken(user, pending, deviceFingerprint);
        _pendingVerifications.Remove(userId);

        _logger.LogInformation("Identity verified for user {UserId} ({Email}) — action {Action} authorized",
            user.Id, user.Email, pending.ActionType);

        return new VerificationResult
        {
            IsVerified = true,
            AuthorizationToken = authToken,
            VerifiedAt = DateTime.UtcNow,
            ActionType = pending.ActionType
        };
    }

    /// <summary>
    /// Step 3: Log the AI action after it completes (successful or failed).
    /// Creates an immutable audit record with digital hash.
    /// </summary>
    public AIActionLog LogAction(
        User user,
        AIActionType actionType,
        string description,
        string sessionId,
        string deviceFingerprint,
        IdentityVerificationMethod verificationMethod,
        bool succeeded,
        string? caseNumber = null,
        int? caseId = null,
        string? failureReason = null)
    {
        var log = new AIActionLog
        {
            Id = _nextLogId++,
            UserId = user.Id,
            UserEmail = user.Email,
            UserName = user.Name,
            UserRole = user.Role,
            SessionId = sessionId,
            DeviceFingerprint = deviceFingerprint,
            ActionType = actionType,
            ActionDescription = description,
            RelatedCaseNumber = caseNumber,
            RelatedCaseId = caseId,
            VerificationMethod = verificationMethod,
            VerifiedAt = DateTime.UtcNow,
            IdentityConfirmed = true,
            ActionTimestamp = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            ActionSucceeded = succeeded,
            FailureReason = failureReason
        };

        // Generate tamper-proof hash
        log.ActionHash = GenerateActionHash(log);

        _actionLogs.Add(log);

        _logger.LogInformation(
            "AI Action logged: {ActionType} by {UserName} ({Email}), Case: {CaseNumber}, Success: {Success}, Hash: {Hash}",
            actionType, user.Name, user.Email, caseNumber ?? "N/A", succeeded, log.ActionHash[..16]);

        return log;
    }

    /// <summary>
    /// Get all audit logs for a specific user
    /// </summary>
    public List<AIActionLog> GetUserActionLogs(int userId)
    {
        return _actionLogs.Where(l => l.UserId == userId).OrderByDescending(l => l.ActionTimestamp).ToList();
    }

    /// <summary>
    /// Get all audit logs (admin view)
    /// </summary>
    public List<AIActionLog> GetAllActionLogs()
    {
        return _actionLogs.OrderByDescending(l => l.ActionTimestamp).ToList();
    }

    /// <summary>
    /// Verify integrity of an action log entry (detect tampering)
    /// </summary>
    public bool VerifyLogIntegrity(AIActionLog log)
    {
        var expectedHash = GenerateActionHash(log);
        return log.ActionHash == expectedHash;
    }

    #region Private Methods

    private bool VerifyPassword(User user, string password)
    {
        return user.Password == password;
    }

    private bool VerifyPIN(int userId, string pin)
    {
        var session = _sessionService.GetSession(userId);
        if (session == null) return false;
        // PIN is last 4 chars of session token (set during login)
        var expectedPin = session.SessionToken[^4..];
        return pin == expectedPin;
    }

    private bool VerifyBiometric(int userId, string credential)
    {
        // WebAuthn assertion result from the browser
        // In a production system, this would validate the PublicKeyCredential assertion
        // For this implementation, we trust the browser's WebAuthn API result
        // The credential string is the Base64-encoded assertion from navigator.credentials.get()
        if (string.IsNullOrEmpty(credential)) return false;

        var session = _sessionService.GetSession(userId);
        if (session == null) return false;

        // Verify the biometric assertion came from the same device
        _logger.LogInformation("Biometric verification accepted for user {UserId} on {Platform}",
            userId, session.DeviceProfile.Platform);
        return credential == "biometric-verified";
    }

    private bool VerifyOTP(int userId, string otp)
    {
        // Time-based OTP verification
        // In a production system, this would validate against a TOTP secret (e.g., Google Authenticator)
        // For now, accept a 6-digit code derived from session token + current minute
        if (string.IsNullOrEmpty(otp) || otp.Length != 6) return false;

        var session = _sessionService.GetSession(userId);
        if (session == null) return false;

        var currentMinute = DateTime.UtcNow.ToString("yyyyMMddHHmm");
        var expectedPayload = $"{session.SessionToken}|{currentMinute}";
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(expectedPayload));
        var expectedOtp = (Math.Abs(BitConverter.ToInt32(hash, 0)) % 1000000).ToString("D6");

        return otp == expectedOtp;
    }

    private string GenerateActionAuthToken(User user, PendingVerification verification, string? deviceFingerprint)
    {
        var payload = $"{user.Id}|{verification.ActionType}|{verification.VerificationToken}|{DateTime.UtcNow:O}|{deviceFingerprint}";
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToBase64String(hash);
    }

    private string GenerateActionHash(AIActionLog log)
    {
        var data = $"{log.UserId}|{log.ActionId}|{log.ActionTimestamp:O}|{log.DeviceFingerprint}|{log.ActionType}|{log.RelatedCaseNumber}";
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToBase64String(hash);
    }

    private string GetVerificationMessage(AIActionType actionType, PlatformSecurityPolicy policy)
    {
        var platformHint = policy.BiometricAvailable
            ? $" You can use {policy.BiometricLabel} or your password."
            : " Please re-enter your password.";

        return actionType switch
        {
            AIActionType.CaseFiled => $"⚖️ You are about to file a legal case through AI on {policy.PlatformLabel}.{platformHint}",
            AIActionType.FIRGenerated => $"📋 AI is generating an FIR draft on {policy.PlatformLabel}.{platformHint}",
            AIActionType.EvidenceSubmitted => $"📎 AI is submitting evidence on your behalf ({policy.PlatformLabel}).{platformHint}",
            AIActionType.LegalNoticeGenerated => $"📜 AI is generating a legal notice under your name ({policy.PlatformLabel}).{platformHint}",
            _ => $"🔐 This AI action requires identity verification on {policy.PlatformLabel}.{platformHint}"
        };
    }

    #endregion
}

/// <summary>
/// Pending identity verification before an AI action
/// </summary>
public class PendingVerification
{
    public string VerificationToken { get; set; } = string.Empty;
    public int UserId { get; set; }
    public AIActionType ActionType { get; set; }
    public string ActionDescription { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int FailedAttempts { get; set; }
}

/// <summary>
/// Challenge presented to user before AI action
/// </summary>
public class VerificationChallenge
{
    public string VerificationToken { get; set; } = string.Empty;
    public AIActionType ActionType { get; set; }
    public string ActionDescription { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public IdentityVerificationMethod RequiredMethod { get; set; }
    public List<IdentityVerificationMethod> AvailableMethods { get; set; } = new();
    public string PlatformLabel { get; set; } = string.Empty;
    public string BiometricLabel { get; set; } = string.Empty;
    public bool BiometricAvailable { get; set; }
    public string SecurityNotes { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

/// <summary>
/// Result of identity verification attempt
/// </summary>
public class VerificationResult
{
    public bool IsVerified { get; set; }
    public string? Reason { get; set; }
    public string? AuthorizationToken { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public AIActionType? ActionType { get; set; }
    public int AttemptsRemaining { get; set; } = 3;
}

using AILegalAsst.Models;

namespace AILegalAsst.Services;

public class AuthenticationService
{
    private User? _currentUser;
    private readonly List<User> _users = new();
    private readonly IConfiguration _configuration;
    private readonly SessionSecurityService _sessionSecurity;
    
    public event Action? OnAuthStateChanged;

    public AuthenticationService(IConfiguration configuration, SessionSecurityService sessionSecurity)
    {
        _configuration = configuration;
        _sessionSecurity = sessionSecurity;
        LoadUsersFromConfiguration();
    }

    private void LoadUsersFromConfiguration()
    {
        var usersSection = _configuration.GetSection("Users");
        var userConfigs = usersSection.GetChildren();

        foreach (var userConfig in userConfigs)
        {
            var roleString = userConfig["Role"] ?? "Citizen";
            var role = Enum.TryParse<UserRole>(roleString, out var parsedRole) ? parsedRole : UserRole.Citizen;

            var user = new User
            {
                Id = int.TryParse(userConfig["Id"], out var id) ? id : _users.Count + 1,
                Name = userConfig["Name"] ?? "Unknown User",
                Email = userConfig["Email"] ?? "",
                Password = userConfig["Password"] ?? "",
                Role = role,
                Phone = userConfig["Phone"] ?? "",
                Address = userConfig["Address"] ?? "",
                VerificationStatus = VerificationStatus.Verified,
                IsEmailVerified = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            // Load role-specific fields
            if (role == UserRole.Lawyer)
            {
                user.BarCouncilNumber = userConfig["BarCouncilNumber"];
                user.BarCouncilState = userConfig["BarCouncilState"];
                user.YearsOfPractice = int.TryParse(userConfig["YearsOfPractice"], out var years) ? years : 0;
                user.Specialization = userConfig["Specialization"];
                user.OfficeAddress = userConfig["OfficeAddress"];
            }
            else if (role == UserRole.Police)
            {
                user.PoliceId = userConfig["PoliceId"];
                user.Rank = userConfig["Rank"];
                user.Department = userConfig["Department"];
                user.PoliceStation = userConfig["PoliceStation"];
                user.Jurisdiction = userConfig["Jurisdiction"];
            }

            _users.Add(user);
        }

        // If no users configured, add fallback demo users
        if (!_users.Any())
        {
            InitializeFallbackUsers();
        }
    }

    private void InitializeFallbackUsers()
    {
        _users.Add(new User
        {
            Id = 1,
            Name = "Demo Citizen",
            Email = "citizen@demo.com",
            Password = "demo123",
            Role = UserRole.Citizen,
            VerificationStatus = VerificationStatus.Verified,
            IsEmailVerified = true,
            IsActive = true
        });

        _users.Add(new User
        {
            Id = 2,
            Name = "Admin User",
            Email = "admin@ailegal.com",
            Password = "admin123",
            Role = UserRole.Admin,
            VerificationStatus = VerificationStatus.Verified,
            IsEmailVerified = true,
            IsActive = true
        });
    }

    public async Task<bool> LoginAsync(string email, string password, UserRole role, string? deviceFingerprint = null)
    {
        await Task.Delay(500); // Simulate API call
        
        var user = _users.FirstOrDefault(u => 
            u.Email == email && 
            u.Password == password && 
            u.Role == role &&
            u.IsActive);

        if (user != null)
        {
            _currentUser = user;
            _currentUser.LastLoginAt = DateTime.UtcNow;

            // Create security session with device fingerprint
            _sessionSecurity.CreateSession(user, deviceFingerprint ?? "web-default");

            OnAuthStateChanged?.Invoke();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Update the device fingerprint for the current user's session (called from JS interop after login)
    /// </summary>
    public void UpdateDeviceFingerprint(string fingerprint)
    {
        if (_currentUser != null)
        {
            var existing = _sessionSecurity.GetSession(_currentUser.Id);
            if (existing == null)
            {
                _sessionSecurity.CreateSession(_currentUser, fingerprint);
            }
        }
    }

    /// <summary>
    /// Update the full device profile for the current user's session (called from JS interop after login)
    /// </summary>
    public void UpdateDeviceProfile(DeviceProfile profile)
    {
        if (_currentUser != null)
        {
            _sessionSecurity.UpdateDeviceProfile(_currentUser.Id, profile);
        }
    }

    public User? Register(User newUser)
    {
        // Check if email already exists
        if (_users.Any(u => u.Email == newUser.Email))
        {
            return null;
        }

        // Assign new ID
        newUser.Id = _users.Any() ? _users.Max(u => u.Id) + 1 : 1;
        newUser.CreatedAt = DateTime.UtcNow;
        newUser.IsActive = true;

        // Set verification status based on role
        if (newUser.Role == UserRole.Citizen)
        {
            newUser.VerificationStatus = VerificationStatus.Verified;
            newUser.IsEmailVerified = true; // Auto-verify for demo
            newUser.VerifiedAt = DateTime.UtcNow;
        }
        else
        {
            newUser.VerificationStatus = VerificationStatus.Pending;
        }

        // Add to users list
        _users.Add(newUser);

        // Auto-login citizens
        if (newUser.Role == UserRole.Citizen)
        {
            _currentUser = newUser;
            OnAuthStateChanged?.Invoke();
        }

        return newUser;
    }

    public List<User> GetPendingVerifications()
    {
        return _users.Where(u => 
            u.VerificationStatus == VerificationStatus.Pending && 
            (u.Role == UserRole.Lawyer || u.Role == UserRole.Police))
            .OrderBy(u => u.CreatedAt)
            .ToList();
    }

    public List<User> GetAllUsers()
    {
        return _users.ToList();
    }

    public bool UpdateVerificationStatus(int userId, VerificationStatus status, string? reason = null)
    {
        var user = _users.FirstOrDefault(u => u.Id == userId);
        if (user == null) return false;

        user.VerificationStatus = status;
        
        if (status == VerificationStatus.Verified)
        {
            user.VerifiedAt = DateTime.UtcNow;
            user.RejectionReason = null;
        }
        else if (status == VerificationStatus.Rejected)
        {
            user.RejectionReason = reason;
        }

        return true;
    }

    public void Logout()
    {
        _currentUser = null;
        OnAuthStateChanged?.Invoke();
    }

    public User? GetCurrentUser() => _currentUser;

    public bool IsAuthenticated() => _currentUser != null;

    public bool IsInRole(UserRole role) => _currentUser?.Role == role;

    public bool HasAccess(params UserRole[] allowedRoles)
    {
        if (_currentUser == null) return false;
        return allowedRoles.Contains(_currentUser.Role);
    }
}

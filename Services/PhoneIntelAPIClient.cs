using AILegalAsst.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace AILegalAsst.Services;

/// <summary>
/// Phone Intelligence API Client
/// Handles HTTP communication with external APIs (Telecom, Banking, OSINT, Police DB)
/// </summary>
public class PhoneIntelAPIClient
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly PhoneIntelAPIConfig _config;
    private readonly ILogger<PhoneIntelAPIClient> _logger;

    public PhoneIntelAPIClient(
        HttpClient httpClient,
        IMemoryCache cache,
        IOptions<PhoneIntelAPIConfig> config,
        ILogger<PhoneIntelAPIClient> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _config = config.Value;
        _logger = logger;

        _httpClient.Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds);
    }

    /// <summary>
    /// Get Telecom Data (CDR, Call Records, SMS)
    /// </summary>
    public async Task<APIResponse<TelecomDataResponse>> GetTelecomDataAsync(string phoneNumber)
    {
        var cacheKey = $"telecom-{phoneNumber}";

        // Check cache first
        if (_cache.TryGetValue(cacheKey, out TelecomDataResponse? cached))
        {
            _logger.LogInformation("Telecom data retrieved from cache for {Phone}", phoneNumber);
            return new APIResponse<TelecomDataResponse>
            {
                Success = true,
                Data = cached,
                StatusCode = 200
            };
        }

        try
        {
            if (_config.UseMockData)
            {
                var mockData = GenerateMockTelecomData(phoneNumber);
                CacheData(cacheKey, mockData);
                return new APIResponse<TelecomDataResponse>
                {
                    Success = true,
                    Data = mockData,
                    StatusCode = 200
                };
            }

            // Real API call
            var url = $"{_config.TelecomEndpoint}/cdr?phone={phoneNumber}";
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _config.TelecomApiKey);

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var data = await response.Content.ReadFromJsonAsync<TelecomDataResponse>();

            if (data != null)
            {
                CacheData(cacheKey, data);
                _logger.LogInformation("Telecom data fetched successfully for {Phone}", phoneNumber);
            }

            return new APIResponse<TelecomDataResponse>
            {
                Success = true,
                Data = data,
                StatusCode = (int)response.StatusCode
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching telecom data for {Phone}", phoneNumber);
            return new APIResponse<TelecomDataResponse>
            {
                Success = false,
                ErrorMessage = ex.Message,
                StatusCode = 500
            };
        }
    }

    /// <summary>
    /// Get Banking Data (Accounts, Transactions)
    /// </summary>
    public async Task<APIResponse<BankingDataResponse>> GetBankingDataAsync(string phoneNumber)
    {
        var cacheKey = $"banking-{phoneNumber}";

        if (_cache.TryGetValue(cacheKey, out BankingDataResponse? cached))
        {
            _logger.LogInformation("Banking data retrieved from cache for {Phone}", phoneNumber);
            return new APIResponse<BankingDataResponse>
            {
                Success = true,
                Data = cached,
                StatusCode = 200
            };
        }

        try
        {
            if (_config.UseMockData)
            {
                var mockData = GenerateMockBankingData(phoneNumber);
                CacheData(cacheKey, mockData);
                return new APIResponse<BankingDataResponse>
                {
                    Success = true,
                    Data = mockData,
                    StatusCode = 200
                };
            }

            var url = $"{_config.BankingEndpoint}/accounts?phone={phoneNumber}";
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _config.BankingApiKey);

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var data = await response.Content.ReadFromJsonAsync<BankingDataResponse>();

            if (data != null)
            {
                CacheData(cacheKey, data);
                _logger.LogInformation("Banking data fetched successfully for {Phone}", phoneNumber);
            }

            return new APIResponse<BankingDataResponse>
            {
                Success = true,
                Data = data,
                StatusCode = (int)response.StatusCode
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching banking data for {Phone}", phoneNumber);
            return new APIResponse<BankingDataResponse>
            {
                Success = false,
                ErrorMessage = ex.Message,
                StatusCode = 500
            };
        }
    }

    /// <summary>
    /// Get OSINT Data (Social Media, Online Presence)
    /// </summary>
    public async Task<APIResponse<OSINTDataResponse>> GetOSINTDataAsync(string phoneNumber)
    {
        var cacheKey = $"osint-{phoneNumber}";

        if (_cache.TryGetValue(cacheKey, out OSINTDataResponse? cached))
        {
            _logger.LogInformation("OSINT data retrieved from cache for {Phone}", phoneNumber);
            return new APIResponse<OSINTDataResponse>
            {
                Success = true,
                Data = cached,
                StatusCode = 200
            };
        }

        try
        {
            if (_config.UseMockData)
            {
                var mockData = GenerateMockOSINTData(phoneNumber);
                CacheData(cacheKey, mockData);
                return new APIResponse<OSINTDataResponse>
                {
                    Success = true,
                    Data = mockData,
                    StatusCode = 200
                };
            }

            var url = $"{_config.OSINTEndpoint}/search?phone={phoneNumber}";
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _config.OSINTApiKey);

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var data = await response.Content.ReadFromJsonAsync<OSINTDataResponse>();

            if (data != null)
            {
                CacheData(cacheKey, data);
                _logger.LogInformation("OSINT data fetched successfully for {Phone}", phoneNumber);
            }

            return new APIResponse<OSINTDataResponse>
            {
                Success = true,
                Data = data,
                StatusCode = (int)response.StatusCode
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching OSINT data for {Phone}", phoneNumber);
            return new APIResponse<OSINTDataResponse>
            {
                Success = false,
                ErrorMessage = ex.Message,
                StatusCode = 500
            };
        }
    }

    /// <summary>
    /// Get Police Database Data (Criminal Records, Cases)
    /// </summary>
    public async Task<APIResponse<PoliceDataResponse>> GetPoliceDataAsync(string phoneNumber)
    {
        var cacheKey = $"police-{phoneNumber}";

        if (_cache.TryGetValue(cacheKey, out PoliceDataResponse? cached))
        {
            _logger.LogInformation("Police data retrieved from cache for {Phone}", phoneNumber);
            return new APIResponse<PoliceDataResponse>
            {
                Success = true,
                Data = cached,
                StatusCode = 200
            };
        }

        try
        {
            if (_config.UseMockData)
            {
                var mockData = GenerateMockPoliceData(phoneNumber);
                CacheData(cacheKey, mockData);
                return new APIResponse<PoliceDataResponse>
                {
                    Success = true,
                    Data = mockData,
                    StatusCode = 200
                };
            }

            var url = $"{_config.PoliceDBEndpoint}/records?phone={phoneNumber}";
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _config.PoliceDBApiKey);

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var data = await response.Content.ReadFromJsonAsync<PoliceDataResponse>();

            if (data != null)
            {
                CacheData(cacheKey, data);
                _logger.LogInformation("Police data fetched successfully for {Phone}", phoneNumber);
            }

            return new APIResponse<PoliceDataResponse>
            {
                Success = true,
                Data = data,
                StatusCode = (int)response.StatusCode
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching police data for {Phone}", phoneNumber);
            return new APIResponse<PoliceDataResponse>
            {
                Success = false,
                ErrorMessage = ex.Message,
                StatusCode = 500
            };
        }
    }

    // ===== Helper Methods =====

    private void CacheData<T>(string key, T data)
    {
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_config.CacheDurationMinutes),
            Priority = CacheItemPriority.Normal
        };

        _cache.Set(key, data, cacheOptions);
    }

    // ===== Mock Data Generators (for development/testing) =====

    private TelecomDataResponse GenerateMockTelecomData(string phoneNumber)
    {
        return new TelecomDataResponse
        {
            PhoneNumber = phoneNumber,
            RegistrationDate = "2019-05-15",
            CarrierName = "Airtel",
            OwnerName = "John Doe",
            Aadhaar = "XXXX-XXXX-1234",
            TotalCalls = 1250,
            TotalSMS = 523,
            TotalMinutes = 15670,
            CallFrequency = new Dictionary<string, int>
            {
                { "+91 98765 43200", 125 },
                { "+91 98765 43201", 98 },
                { "+91 98765 43202", 67 },
                { "+91 98765 43203", 45 },
                { "+91 98765 43204", 32 }
            },
            RecentCalls = new List<CallRecord>
            {
                new() { DateTime = DateTime.Now.AddHours(-2), Type = "Outgoing", OtherParty = "+91 98765 43200", DurationSeconds = 320, Location = "Chennai" },
                new() { DateTime = DateTime.Now.AddHours(-5), Type = "Incoming", OtherParty = "+91 98765 43201", DurationSeconds = 180, Location = "Chennai" },
                new() { DateTime = DateTime.Now.AddHours(-8), Type = "Outgoing", OtherParty = "+91 98765 43202", DurationSeconds = 540, Location = "Coimbatore" }
            },
            LastActiveDate = DateTime.Now.AddHours(-2).ToString("yyyy-MM-dd HH:mm:ss")
        };
    }

    private BankingDataResponse GenerateMockBankingData(string phoneNumber)
    {
        return new BankingDataResponse
        {
            PhoneNumber = phoneNumber,
            LinkedAccounts = new List<LinkedAccount>
            {
                new() { AccountNumber = "XXXX-XXXX-1234", BankName = "HDFC Bank", AccountType = "Savings", Balance = 125000, Status = "Active" },
                new() { AccountNumber = "XXXX-XXXX-5678", BankName = "SBI", AccountType = "Current", Balance = 87500, Status = "Active" }
            },
            TotalBalance = 212500,
            TotalIncomeLast30Days = 95000,
            TotalExpenseLast30Days = 67500,
            AverageTransactionAmount = 2500,
            SuspiciousTransactionCount = 2,
            Transactions = new List<BankTransactionRecord>
            {
                new() { DateTime = DateTime.Now.AddDays(-1), Type = "Credit", Amount = 25000, OtherParty = "Salary", Description = "Monthly salary", IsSuspicious = false },
                new() { DateTime = DateTime.Now.AddDays(-2), Type = "Debit", Amount = 45000, OtherParty = "Unknown", Description = "Large cash withdrawal", IsSuspicious = true },
                new() { DateTime = DateTime.Now.AddDays(-3), Type = "Credit", Amount = 15000, OtherParty = "Transfer", Description = "Received from +91 98765 43200", IsSuspicious = false }
            },
            FrequentRecipients = new List<string> { "Vendor A", "Utility Company", "Online Store" },
            FrequentSenders = new List<string> { "Employer", "Family Member", "Friend" },
            HasAccountFreeze = false
        };
    }

    private OSINTDataResponse GenerateMockOSINTData(string phoneNumber)
    {
        return new OSINTDataResponse
        {
            PhoneNumber = phoneNumber,
            Profiles = new List<SocialProfile>
            {
                new() { Platform = "Facebook", Username = "john.doe", ProfileUrl = "https://facebook.com/john.doe", Bio = "Software Engineer", FollowerCount = 523, LastActive = DateTime.Now.AddDays(-1) },
                new() { Platform = "Twitter", Username = "@johndoe", ProfileUrl = "https://twitter.com/johndoe", Bio = "Tech enthusiast", FollowerCount = 234, LastActive = DateTime.Now.AddHours(-5) },
                new() { Platform = "LinkedIn", Username = "john-doe", ProfileUrl = "https://linkedin.com/in/john-doe", Bio = "Senior Developer", FollowerCount = 876, LastActive = DateTime.Now.AddDays(-3) }
            },
            EmailAddresses = new List<string> { "john.doe@example.com", "johndoe@gmail.com" },
            AlternatePhones = new List<string> { "+91 98765 43299", "+91 98765 43298" },
            Addresses = new List<string> { "Chennai, Tamil Nadu", "Coimbatore, Tamil Nadu" },
            Associates = new List<string> { "+91 98765 43200", "+91 98765 43201", "+91 98765 43202" },
            RecentActivities = new List<OnlineActivity>
            {
                new() { DateTime = DateTime.Now.AddHours(-3), Platform = "Facebook", ActivityType = "Post", Description = "Posted a status update" },
                new() { DateTime = DateTime.Now.AddHours(-12), Platform = "Twitter", ActivityType = "Tweet", Description = "Tweeted about tech news" }
            },
            RiskScore = 35
        };
    }

    private PoliceDataResponse GenerateMockPoliceData(string phoneNumber)
    {
        return new PoliceDataResponse
        {
            PhoneNumber = phoneNumber,
            IsWanted = false,
            IsAbsconder = false,
            IsDangerous = false,
            TotalArrests = 0,
            TotalCases = 1,
            PreviousArrests = new List<ArrestRecord>(),
            RelatedCases = new List<CaseRecord>
            {
                new() { CaseNumber = "CASE-2026-001", CaseTitle = "Cyber Fraud Investigation", RoleInCase = "Witness", CaseDate = DateTime.Now.AddMonths(-2), CaseStatus = "Open" }
            },
            ConvictionHistory = new List<Conviction>(),
            KnownCrimeMethods = new List<string>(),
            CriminalSpecialty = null,
            RiskLevel = "Low"
        };
    }
}

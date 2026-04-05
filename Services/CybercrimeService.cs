using AILegalAsst.Models;

namespace AILegalAsst.Services;

public class CybercrimeService
{
    private readonly List<CybercrimeReport> _reports;
    private readonly List<CybercrimeResource> _resources;
    private readonly AzureAgentService _agentService;
    private readonly ILogger<CybercrimeService> _logger;
    private int _nextReportId = 1;

    public CybercrimeService(AzureAgentService agentService, ILogger<CybercrimeService> logger)
    {
        _agentService = agentService;
        _logger = logger;
        _reports = InitializeSampleReports();
        _resources = InitializeResources();
    }

    public List<CybercrimeReport> GetAllReports()
    {
        return _reports;
    }

    public List<CybercrimeReport> GetReportsByUser(int userId)
    {
        return _reports.Where(r => r.VictimUserId == userId).OrderByDescending(r => r.ReportedDate).ToList();
    }

    public List<CybercrimeReport> GetReportsByOfficer(int officerId)
    {
        return _reports.Where(r => r.AssignedOfficerId == officerId).OrderByDescending(r => r.ReportedDate).ToList();
    }

    public List<CybercrimeReport> GetReportsByStatus(ReportStatus status)
    {
        return _reports.Where(r => r.Status == status).OrderByDescending(r => r.ReportedDate).ToList();
    }

    public CybercrimeReport? GetReportById(int id)
    {
        return _reports.FirstOrDefault(r => r.Id == id);
    }

    public CybercrimeReport? GetReportByNumber(string reportNumber)
    {
        return _reports.FirstOrDefault(r => r.ReportNumber == reportNumber);
    }

    public CybercrimeReport CreateReport(CybercrimeReport report)
    {
        report.Id = _nextReportId++;
        report.ReportNumber = $"CYB-{DateTime.Now.Year}-{report.Id:D6}";
        report.ReportedDate = DateTime.UtcNow;
        report.Status = ReportStatus.Submitted;

        // AI-powered auto-categorization of applicable legal sections
        if (_agentService.IsReady && (report.ApplicableSections == null || report.ApplicableSections.Count == 0))
        {
            try
            {
                var prompt = $@"You are an Indian cybercrime legal expert. For this cybercrime report, identify the applicable legal sections.

Incident Type: {report.IncidentType}
Title: {report.Title}
Description: {report.Description}
Financial Loss: ₹{report.FinancialLoss:N0}

Return ONLY a comma-separated list of applicable Indian legal sections (BNS, IT Act, BNSS). Example: IT Act Section 66D, BNS Section 318, IT Act Section 43";

                var response = _agentService.SendMessageAsync(prompt, "Indian cybercrime legal section identification").GetAwaiter().GetResult();
                if (response.Success && !string.IsNullOrEmpty(response.Message))
                {
                    report.ApplicableSections = response.Message
                        .Split(',', StringSplitOptions.TrimEntries)
                        .Where(s => !string.IsNullOrEmpty(s))
                        .Take(8)
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AI section categorization failed");
            }
        }

        _reports.Add(report);
        return report;
    }

    public void UpdateReportStatus(int reportId, ReportStatus newStatus, string updatedBy, string message)
    {
        var report = _reports.FirstOrDefault(r => r.Id == reportId);
        if (report != null)
        {
            report.Status = newStatus;
            report.LastUpdated = DateTime.UtcNow;
            
            report.Updates.Add(new CybercrimeUpdate
            {
                Id = report.Updates.Count + 1,
                ReportId = reportId,
                UpdateDate = DateTime.UtcNow,
                UpdatedBy = updatedBy,
                Message = message,
                NewStatus = newStatus
            });
        }
    }

    public void AssignOfficer(int reportId, int officerId, string officerName)
    {
        var report = _reports.FirstOrDefault(r => r.Id == reportId);
        if (report != null)
        {
            report.AssignedOfficerId = officerId;
            report.AssignedOfficerName = officerName;
            report.Status = ReportStatus.InvestigationStarted;
            report.LastUpdated = DateTime.UtcNow;
        }
    }

    public void FileFIR(int reportId, string firNumber, string policeStation)
    {
        var report = _reports.FirstOrDefault(r => r.Id == reportId);
        if (report != null)
        {
            report.FIRNumber = firNumber;
            report.FIRFiledDate = DateTime.UtcNow;
            report.PoliceStationName = policeStation;
            report.Status = ReportStatus.FIRFiled;
            report.LastUpdated = DateTime.UtcNow;
        }
    }

    public void UpdateReport(CybercrimeReport updatedReport)
    {
        var report = _reports.FirstOrDefault(r => r.Id == updatedReport.Id);
        if (report != null)
        {
            // Update all editable fields
            report.Title = updatedReport.Title;
            report.Description = updatedReport.Description;
            report.Status = updatedReport.Status;
            report.AssignedOfficerName = updatedReport.AssignedOfficerName;
            report.FIRNumber = updatedReport.FIRNumber;
            report.FIRFiledDate = updatedReport.FIRFiledDate;
            report.PoliceStationName = updatedReport.PoliceStationName;
            report.LastUpdated = DateTime.UtcNow;
        }
    }

    public List<CybercrimeResource> GetResources()
    {
        return _resources;
    }

    public List<CybercrimeResource> GetResourcesByCategory(string category)
    {
        return _resources.Where(r => r.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public List<CybercrimeResource> GetEmergencyResources()
    {
        return _resources.Where(r => r.IsEmergency).ToList();
    }

    public CybercrimeStatistics GetStatistics()
    {
        var stats = new CybercrimeStatistics
        {
            TotalReports = _reports.Count,
            ActiveInvestigations = _reports.Count(r => r.Status == ReportStatus.InvestigationStarted || r.Status == ReportStatus.EvidenceCollected),
            FIRsFiled = _reports.Count(r => !string.IsNullOrEmpty(r.FIRNumber)),
            CasesClosed = _reports.Count(r => r.Status == ReportStatus.Closed),
            TotalFinancialLoss = _reports.Where(r => r.FinancialLoss.HasValue).Sum(r => r.FinancialLoss!.Value)
        };

        // Reports by type
        foreach (CybercrimeType type in Enum.GetValues(typeof(CybercrimeType)))
        {
            var count = _reports.Count(r => r.IncidentType == type);
            if (count > 0)
            {
                stats.ReportsByType[type] = count;
            }
        }

        return stats;
    }

    public async Task<string?> GetTrendInsightAsync(CybercrimeStatistics stats, CancellationToken ct = default)
    {
        if (!_agentService.IsReady) return null;
        try
        {
            var typeSummary = string.Join(", ", stats.ReportsByType.Select(kv => $"{kv.Key}: {kv.Value}"));
            var prompt = $@"You are an Indian cybercrime analyst. Provide a brief 2-3 sentence trend analysis based on these cybercrime statistics.

Total Reports: {stats.TotalReports}
Active Investigations: {stats.ActiveInvestigations}
FIRs Filed: {stats.FIRsFiled}
Total Financial Loss: ₹{stats.TotalFinancialLoss:N0}
Reports by Type: {typeSummary}

Provide actionable insight about the current cybercrime trend and key risks. Be concise.";

            var response = await _agentService.SendMessageAsync(prompt, "Indian cybercrime trend analysis", ct);
            if (response.Success && !string.IsNullOrEmpty(response.Message))
                return response.Message.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI trend insight generation failed");
        }
        return null;
    }

    public List<CybercrimeReport> SearchReports(string searchTerm, CybercrimeType? type = null, ReportStatus? status = null)
    {
        var query = _reports.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(r =>
                r.ReportNumber.ToLower().Contains(term) ||
                r.Title.ToLower().Contains(term) ||
                r.Description.ToLower().Contains(term) ||
                r.VictimName.ToLower().Contains(term) ||
                r.FIRNumber.ToLower().Contains(term)
            );
        }

        if (type.HasValue)
        {
            query = query.Where(r => r.IncidentType == type.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        return query.OrderByDescending(r => r.ReportedDate).ToList();
    }

    private List<CybercrimeReport> InitializeSampleReports()
    {
        return new List<CybercrimeReport>
        {
            new CybercrimeReport
            {
                Id = _nextReportId++,
                ReportNumber = "CYB-2025-000001",
                IncidentType = CybercrimeType.OnlineFraud,
                Title = "UPI Payment Fraud",
                Description = "I received a call from someone claiming to be from my bank. They asked me to share an OTP, and Rs. 45,000 was debited from my account.",
                IncidentDate = DateTime.Now.AddDays(-5),
                ReportedDate = DateTime.Now.AddDays(-4),
                VictimUserId = 1,
                VictimName = "Rajesh Kumar",
                VictimEmail = "rajesh@example.com",
                VictimPhone = "9876543210",
                FinancialLoss = 45000,
                TransactionDetails = "UPI Transaction ID: 123456789, Date: " + DateTime.Now.AddDays(-5).ToShortDateString(),
                Status = ReportStatus.InvestigationStarted,
                AssignedOfficerId = 3,
                AssignedOfficerName = "Inspector Suresh Patil",
                PoliceStationName = "Cyber Crime Cell, Mumbai",
                IsUrgent = true,
                ApplicableSections = new List<string> { "IT Act Section 66D", "IPC Section 420" },
                Updates = new List<CybercrimeUpdate>
                {
                    new CybercrimeUpdate
                    {
                        Id = 1,
                        UpdateDate = DateTime.Now.AddDays(-3),
                        UpdatedBy = "Inspector Suresh Patil",
                        Message = "Report reviewed. Investigation started. Bank contacted for transaction details.",
                        NewStatus = ReportStatus.InvestigationStarted
                    }
                }
            },
            new CybercrimeReport
            {
                Id = _nextReportId++,
                ReportNumber = "CYB-2025-000002",
                IncidentType = CybercrimeType.Phishing,
                Title = "Fake Email Scam",
                Description = "Received email claiming to be from income tax department asking to verify PAN card details and pay pending dues.",
                IncidentDate = DateTime.Now.AddDays(-2),
                ReportedDate = DateTime.Now.AddDays(-1),
                VictimUserId = 1,
                VictimName = "Rajesh Kumar",
                VictimEmail = "rajesh@example.com",
                VictimPhone = "9876543210",
                Status = ReportStatus.UnderReview,
                ApplicableSections = new List<string> { "IT Act Section 66C", "IT Act Section 66D" },
                Screenshots = new List<string> { "phishing_email_1.png", "phishing_email_2.png" }
            },
            new CybercrimeReport
            {
                Id = _nextReportId++,
                ReportNumber = "CYB-2025-000003",
                IncidentType = CybercrimeType.SocialMediaAbuse,
                Title = "Harassment on Social Media",
                Description = "Continuous harassment and threatening messages on Instagram from unknown account.",
                IncidentDate = DateTime.Now.AddDays(-10),
                ReportedDate = DateTime.Now.AddDays(-8),
                VictimUserId = 2,
                VictimName = "Priya Sharma",
                VictimEmail = "priya@example.com",
                VictimPhone = "9876543211",
                Status = ReportStatus.FIRFiled,
                FIRNumber = "FIR-2025-1234",
                FIRFiledDate = DateTime.Now.AddDays(-6),
                AssignedOfficerId = 3,
                AssignedOfficerName = "Inspector Suresh Patil",
                PoliceStationName = "Cyber Crime Cell, Mumbai",
                ApplicableSections = new List<string> { "IT Act Section 67", "IPC Section 354D", "IPC Section 509" },
                Screenshots = new List<string> { "instagram_messages.png" }
            }
        };
    }

    private List<CybercrimeResource> InitializeResources()
    {
        return new List<CybercrimeResource>
        {
            // Emergency Contacts
            new CybercrimeResource
            {
                Id = 1,
                Title = "National Cybercrime Helpline",
                Description = "24x7 helpline for reporting cybercrimes",
                Category = "Emergency",
                IconClass = "bi-telephone-fill",
                PhoneNumber = "1930",
                IsEmergency = true
            },
            new CybercrimeResource
            {
                Id = 2,
                Title = "Women Cybercrime Helpline",
                Description = "Dedicated helpline for women facing online harassment",
                Category = "Emergency",
                IconClass = "bi-shield-fill-check",
                PhoneNumber = "1091",
                IsEmergency = true
            },
            new CybercrimeResource
            {
                Id = 3,
                Title = "Cyber Crime Portal",
                Description = "Official portal to report cybercrimes online",
                Category = "Emergency",
                IconClass = "bi-globe",
                Link = "https://cybercrime.gov.in",
                IsEmergency = true
            },
            
            // Prevention Resources
            new CybercrimeResource
            {
                Id = 4,
                Title = "How to Spot Phishing Emails",
                Description = "Learn to identify fake emails and protect yourself from scams",
                Category = "Prevention",
                IconClass = "bi-envelope-exclamation"
            },
            new CybercrimeResource
            {
                Id = 5,
                Title = "Secure Online Banking",
                Description = "Best practices for safe online transactions",
                Category = "Prevention",
                IconClass = "bi-bank"
            },
            new CybercrimeResource
            {
                Id = 6,
                Title = "Social Media Safety",
                Description = "Protect your privacy on social media platforms",
                Category = "Prevention",
                IconClass = "bi-people"
            },
            
            // Awareness
            new CybercrimeResource
            {
                Id = 7,
                Title = "Common Cybercrime Types",
                Description = "Understanding different types of cybercrimes in India",
                Category = "Awareness",
                IconClass = "bi-info-circle"
            },
            new CybercrimeResource
            {
                Id = 8,
                Title = "Digital Evidence Collection",
                Description = "How to preserve evidence for cybercrime cases",
                Category = "Awareness",
                IconClass = "bi-camera"
            },
            
            // Legal Resources
            new CybercrimeResource
            {
                Id = 9,
                Title = "IT Act 2000 - Cyber Laws",
                Description = "Complete guide to Information Technology Act sections",
                Category = "Legal",
                IconClass = "bi-book"
            },
            new CybercrimeResource
            {
                Id = 10,
                Title = "Filing FIR Online",
                Description = "Step-by-step guide to filing cybercrime FIR",
                Category = "Legal",
                IconClass = "bi-file-earmark-text"
            },
            new CybercrimeResource
            {
                Id = 11,
                Title = "Victim Support & Counseling",
                Description = "Psychological support for cybercrime victims",
                Category = "Support",
                IconClass = "bi-heart"
            }
        };
    }
}

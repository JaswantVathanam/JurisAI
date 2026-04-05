using AILegalAsst.Models;
using System.Text.Json;

namespace AILegalAsst.Services
{
    /// <summary>
    /// Service for tracking statutory deadlines under BNSS (Bharatiya Nagarik Suraksha Sanhita)
    /// and other Indian legal provisions. Ensures compliance with mandatory timelines.
    /// </summary>
    public class DeadlineTrackerService
    {
        private readonly List<CaseDeadline> _deadlines = new();
        private readonly List<DeadlineAlert> _alerts = new();
        private readonly ILogger<DeadlineTrackerService> _logger;
        private readonly AzureAgentService _agentService;
        private bool _aiAlertsEnhanced;

        public DeadlineTrackerService(ILogger<DeadlineTrackerService> logger, AzureAgentService agentService)
        {
            _logger = logger;
            _agentService = agentService;
            InitializeSampleData();
        }

        private void InitializeSampleData()
        {
            var baseFIRDate = DateTime.Now.AddDays(-45);

            // Sample deadlines for a case
            var deadlines = new List<CaseDeadline>
            {
                new()
                {
                    Id = "DL-2024-001",
                    CaseId = "FIR-2024-1247",
                    CaseTitle = "Online Banking Fraud - Cyber Cell",
                    FIRDate = baseFIRDate,
                    DeadlineType = BNSSDeadlineType.Chargesheet_90Days,
                    DueDate = baseFIRDate.AddDays(90),
                    Description = "Chargesheet filing deadline for offenses punishable with imprisonment up to 10 years",
                    LegalProvision = "Section 193 BNSS (formerly Section 167 CrPC)",
                    Status = DeadlineStatus.Pending,
                    Priority = DeadlinePriority.High,
                    AssignedOfficer = "Inspector Rajesh Kumar",
                    AssignedUnit = "Cyber Crime Cell, Mumbai",
                    Notes = "Complex financial fraud case - multiple accused",
                    CreatedAt = baseFIRDate,
                    ReminderDays = new List<int> { 30, 15, 7, 3, 1 },
                    ExtensionCount = 0
                },
                new()
                {
                    Id = "DL-2024-002",
                    CaseId = "FIR-2024-1247",
                    CaseTitle = "Online Banking Fraud - Cyber Cell",
                    FIRDate = baseFIRDate,
                    DeadlineType = BNSSDeadlineType.PoliceRemand_15Days,
                    DueDate = baseFIRDate.AddDays(15),
                    Description = "Maximum police custody remand period",
                    LegalProvision = "Section 187 BNSS",
                    Status = DeadlineStatus.Completed,
                    Priority = DeadlinePriority.Critical,
                    AssignedOfficer = "Inspector Rajesh Kumar",
                    CompletedAt = baseFIRDate.AddDays(12),
                    CompletionNotes = "Accused produced before Magistrate, judicial custody ordered",
                    CreatedAt = baseFIRDate
                },
                new()
                {
                    Id = "DL-2024-003",
                    CaseId = "FIR-2024-1247",
                    CaseTitle = "Online Banking Fraud - Cyber Cell",
                    FIRDate = baseFIRDate,
                    DeadlineType = BNSSDeadlineType.JudicialRemand_60Days,
                    DueDate = baseFIRDate.AddDays(60),
                    Description = "First judicial remand period for offenses up to 10 years",
                    LegalProvision = "Section 187 BNSS",
                    Status = DeadlineStatus.Pending,
                    Priority = DeadlinePriority.High,
                    AssignedOfficer = "Inspector Rajesh Kumar",
                    CreatedAt = baseFIRDate,
                    ReminderDays = new List<int> { 7, 3, 1 }
                },
                new()
                {
                    Id = "DL-2024-004",
                    CaseId = "FIR-2024-1289",
                    CaseTitle = "Murder Case - Thane Rural",
                    FIRDate = DateTime.Now.AddDays(-20),
                    DeadlineType = BNSSDeadlineType.Chargesheet_60Days,
                    DueDate = DateTime.Now.AddDays(40),
                    Description = "Chargesheet filing for heinous offenses (death penalty/life imprisonment)",
                    LegalProvision = "Section 193 BNSS",
                    Status = DeadlineStatus.Pending,
                    Priority = DeadlinePriority.Critical,
                    AssignedOfficer = "DCP Sunita Sharma",
                    AssignedUnit = "Crime Branch, Thane",
                    CreatedAt = DateTime.Now.AddDays(-20),
                    ReminderDays = new List<int> { 15, 7, 3, 1 }
                },
                new()
                {
                    Id = "DL-2024-005",
                    CaseId = "FIR-2024-1156",
                    CaseTitle = "Drug Trafficking - NCB",
                    FIRDate = DateTime.Now.AddDays(-85),
                    DeadlineType = BNSSDeadlineType.Chargesheet_90Days,
                    DueDate = DateTime.Now.AddDays(5),
                    Description = "Chargesheet filing deadline - CRITICAL",
                    LegalProvision = "Section 193 BNSS read with NDPS Act",
                    Status = DeadlineStatus.Pending,
                    Priority = DeadlinePriority.Critical,
                    AssignedOfficer = "Superintendent Vikram Mehta",
                    AssignedUnit = "NCB Mumbai Zonal Unit",
                    Notes = "URGENT: Only 5 days remaining. Forensic report awaited.",
                    CreatedAt = DateTime.Now.AddDays(-85),
                    ReminderDays = new List<int> { 3, 1 }
                },
                new()
                {
                    Id = "DL-2024-006",
                    CaseId = "FIR-2024-1178",
                    CaseTitle = "Cheating Case - EOW",
                    FIRDate = DateTime.Now.AddDays(-100),
                    DeadlineType = BNSSDeadlineType.Chargesheet_90Days,
                    DueDate = DateTime.Now.AddDays(-10),
                    Description = "Chargesheet deadline - OVERDUE",
                    LegalProvision = "Section 193 BNSS",
                    Status = DeadlineStatus.Overdue,
                    Priority = DeadlinePriority.Critical,
                    AssignedOfficer = "Inspector Priya Patel",
                    AssignedUnit = "EOW, Mumbai",
                    Notes = "Deadline missed - default bail provisions may apply",
                    CreatedAt = DateTime.Now.AddDays(-100)
                },
                new()
                {
                    Id = "DL-2024-007",
                    CaseId = "FIR-2024-1247",
                    CaseTitle = "Online Banking Fraud - Cyber Cell",
                    FIRDate = baseFIRDate,
                    DeadlineType = BNSSDeadlineType.CDR_Request,
                    DueDate = baseFIRDate.AddDays(7),
                    Description = "CDR request submission to telecom provider",
                    LegalProvision = "Section 91 BNSS",
                    Status = DeadlineStatus.Completed,
                    Priority = DeadlinePriority.Medium,
                    AssignedOfficer = "SI Ramesh Patil",
                    CompletedAt = baseFIRDate.AddDays(3),
                    CompletionNotes = "CDR request submitted to all major telecom operators",
                    CreatedAt = baseFIRDate
                },
                new()
                {
                    Id = "DL-2024-008",
                    CaseId = "FIR-2024-1247",
                    CaseTitle = "Online Banking Fraud - Cyber Cell",
                    FIRDate = baseFIRDate,
                    DeadlineType = BNSSDeadlineType.BankFreeze_Extension,
                    DueDate = baseFIRDate.AddDays(180),
                    Description = "Bank account freeze extension application",
                    LegalProvision = "Section 106 BNSS",
                    Status = DeadlineStatus.Pending,
                    Priority = DeadlinePriority.Medium,
                    AssignedOfficer = "Inspector Rajesh Kumar",
                    Notes = "Initial freeze order valid for 180 days - extension required",
                    CreatedAt = baseFIRDate,
                    ReminderDays = new List<int> { 30, 15, 7 }
                }
            };

            _deadlines.AddRange(deadlines);

            // Generate alerts for critical and overdue deadlines
            GenerateAlerts();
        }

        private void GenerateAlerts()
        {
            _alerts.Clear();
            var today = DateTime.Now.Date;

            foreach (var deadline in _deadlines.Where(d => d.Status == DeadlineStatus.Pending || d.Status == DeadlineStatus.Overdue))
            {
                var daysRemaining = (deadline.DueDate.Date - today).Days;

                AlertSeverity severity;
                string message;

                if (daysRemaining < 0)
                {
                    severity = AlertSeverity.Critical;
                    message = $"OVERDUE by {Math.Abs(daysRemaining)} days - Default bail provisions may apply";
                    deadline.Status = DeadlineStatus.Overdue;
                }
                else if (daysRemaining == 0)
                {
                    severity = AlertSeverity.Critical;
                    message = "Due TODAY - Immediate action required";
                }
                else if (daysRemaining <= 3)
                {
                    severity = AlertSeverity.Critical;
                    message = $"Due in {daysRemaining} day(s) - Critical deadline approaching";
                }
                else if (daysRemaining <= 7)
                {
                    severity = AlertSeverity.High;
                    message = $"Due in {daysRemaining} days - Action required this week";
                }
                else if (daysRemaining <= 15)
                {
                    severity = AlertSeverity.Medium;
                    message = $"Due in {daysRemaining} days - Plan completion";
                }
                else
                {
                    continue; // No alert needed for deadlines > 15 days away
                }

                _alerts.Add(new DeadlineAlert
                {
                    Id = $"ALERT-{Guid.NewGuid():N}",
                    DeadlineId = deadline.Id,
                    CaseId = deadline.CaseId,
                    CaseTitle = deadline.CaseTitle,
                    DeadlineType = deadline.DeadlineType.ToString().Replace("_", " "),
                    DueDate = deadline.DueDate,
                    DaysRemaining = daysRemaining,
                    Severity = severity,
                    Message = message,
                    AssignedOfficer = deadline.AssignedOfficer,
                    CreatedAt = DateTime.Now,
                    IsRead = false,
                    IsAcknowledged = false
                });
            }
        }

        /// <summary>
        /// Enhances critical alerts with AI-powered advice. Call this AFTER the page has rendered
        /// to avoid blocking the UI. Safe to call multiple times (idempotent after first success).
        /// </summary>
        public async Task EnhanceAlertsWithAIAsync()
        {
            if (_aiAlertsEnhanced) return;

            var today = DateTime.Now.Date;
            var criticalDeadlines = _deadlines.Where(d =>
                (d.Status == DeadlineStatus.Pending || d.Status == DeadlineStatus.Overdue) &&
                (d.DueDate.Date - today).Days <= 7).ToList();

            if (!_agentService.IsReady || !criticalDeadlines.Any()) return;

            try
            {
                var deadlineSummary = string.Join("\n", criticalDeadlines.Select(d =>
                    $"- {d.CaseTitle}: {d.DeadlineType} due {d.DueDate:dd MMM yyyy} ({(d.DueDate.Date - today).Days} days), Provision: {d.LegalProvision}"));
                var prompt = $"As an Indian criminal law expert, provide brief actionable advice for these critical deadlines:\n{deadlineSummary}\n" +
                    "For each deadline, provide one line of specific advice including legal consequences of missing the deadline.";
                var context = "You are an expert in BNSS (Bharatiya Nagarik Suraksha Sanhita) and Indian criminal procedure deadlines.";
                var response = await _agentService.SendMessageAsync(prompt, context);
                if (response.Success && !string.IsNullOrEmpty(response.Message))
                {
                    var aiAdvice = response.Message;
                    // Enhance existing critical alerts with AI advice
                    foreach (var alert in _alerts.Where(a => a.DaysRemaining <= 7))
                    {
                        var caseRef = alert.CaseTitle?.Split('-').FirstOrDefault()?.Trim();
                        var adviceLines = aiAdvice.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                        var specificAdvice = adviceLines.FirstOrDefault(l =>
                            (!string.IsNullOrEmpty(caseRef) && l.Contains(caseRef, StringComparison.OrdinalIgnoreCase)) ||
                            (!string.IsNullOrEmpty(alert.DeadlineType) && l.Contains(alert.DeadlineType.Replace("_", " "), StringComparison.OrdinalIgnoreCase)));
                        if (!string.IsNullOrWhiteSpace(specificAdvice))
                            alert.Message += " | AI: " + specificAdvice.Trim().TrimStart('-', '•', ' ', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '.');
                    }
                    _aiAlertsEnhanced = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AI deadline advice generation failed");
            }
        }

        /// <summary>
        /// Create deadlines for a new case based on BNSS rules
        /// </summary>
        public async Task<List<CaseDeadline>> CreateDeadlinesForCaseAsync(
            string caseId,
            string caseTitle,
            DateTime firDate,
            string offenseCategory,
            string assignedOfficer,
            string assignedUnit)
        {
            var createdDeadlines = new List<CaseDeadline>();

            // Determine applicable rules based on offense category
            var applicableRules = GetApplicableRules(offenseCategory);

            foreach (var rule in applicableRules)
            {
                var deadline = new CaseDeadline
                {
                    Id = $"DL-{DateTime.Now:yyyy}-{Guid.NewGuid().ToString()[..6].ToUpper()}",
                    CaseId = caseId,
                    CaseTitle = caseTitle,
                    FIRDate = firDate,
                    DeadlineType = (BNSSDeadlineType)(int)rule.Type,
                    DueDate = firDate.AddDays(rule.Days),
                    Description = rule.Description,
                    LegalProvision = rule.LegalProvision,
                    Status = DeadlineStatus.Pending,
                    Priority = rule.DefaultPriority,
                    AssignedOfficer = assignedOfficer,
                    AssignedUnit = assignedUnit,
                    CreatedAt = DateTime.Now,
                    ReminderDays = rule.DefaultReminderDays
                };

                _deadlines.Add(deadline);
                createdDeadlines.Add(deadline);
            }

            GenerateAlerts();
            _logger.LogInformation("Created {Count} deadlines for case {CaseId}", createdDeadlines.Count, caseId);

            return createdDeadlines;
        }

        private List<DeadlineRule> GetApplicableRules(string offenseCategory)
        {
            var rules = new List<DeadlineRule>();
            var allRules = BNSSDeadlineRules.GetAllRules();

            switch (offenseCategory.ToLower())
            {
                case "heinous":
                case "murder":
                case "death penalty":
                    rules.Add(allRules[BNSSDeadlineType.Chargesheet_60Days]);
                    rules.Add(allRules[BNSSDeadlineType.PoliceRemand_15Days]);
                    rules.Add(allRules[BNSSDeadlineType.JudicialRemand_90Days]);
                    break;

                case "cyber":
                case "fraud":
                case "economic offense":
                    rules.Add(allRules[BNSSDeadlineType.Chargesheet_90Days]);
                    rules.Add(allRules[BNSSDeadlineType.PoliceRemand_15Days]);
                    rules.Add(allRules[BNSSDeadlineType.JudicialRemand_60Days]);
                    rules.Add(allRules[BNSSDeadlineType.CDR_Request]);
                    rules.Add(allRules[BNSSDeadlineType.BankFreeze_Extension]);
                    break;

                case "cognizable":
                default:
                    rules.Add(allRules[BNSSDeadlineType.Chargesheet_90Days]);
                    rules.Add(allRules[BNSSDeadlineType.PoliceRemand_15Days]);
                    rules.Add(allRules[BNSSDeadlineType.JudicialRemand_60Days]);
                    rules.Add(allRules[BNSSDeadlineType.FIR_Registration]);
                    break;
            }

            return rules;
        }

        /// <summary>
        /// Mark deadline as completed
        /// </summary>
        public async Task<bool> CompleteDeadlineAsync(string deadlineId, string completionNotes, string completedBy)
        {
            var deadline = _deadlines.FirstOrDefault(d => d.Id == deadlineId);
            if (deadline == null) return false;

            deadline.Status = DeadlineStatus.Completed;
            deadline.CompletedAt = DateTime.Now;
            deadline.CompletionNotes = completionNotes;
            deadline.UpdatedAt = DateTime.Now;

            // Remove associated alerts
            _alerts.RemoveAll(a => a.DeadlineId == deadlineId);

            _logger.LogInformation("Deadline {Id} completed by {User}", deadlineId, completedBy);
            return true;
        }

        /// <summary>
        /// Request extension for a deadline
        /// </summary>
        public async Task<bool> RequestExtensionAsync(string deadlineId, int additionalDays, string reason, string requestedBy)
        {
            var deadline = _deadlines.FirstOrDefault(d => d.Id == deadlineId);
            if (deadline == null) return false;

            deadline.DueDate = deadline.DueDate.AddDays(additionalDays);
            deadline.ExtensionCount++;
            deadline.ExtensionReason = reason;
            deadline.Status = DeadlineStatus.Extended;
            deadline.UpdatedAt = DateTime.Now;
            deadline.Notes = $"{deadline.Notes}\n[{DateTime.Now:dd-MMM-yyyy}] Extension of {additionalDays} days granted. Reason: {reason}";

            GenerateAlerts();
            _logger.LogInformation("Extension of {Days} days granted for deadline {Id}", additionalDays, deadlineId);
            return true;
        }

        /// <summary>
        /// Get dashboard statistics
        /// </summary>
        public Task<DeadlineDashboard> GetDashboardAsync(string? officerFilter = null, string? unitFilter = null)
        {
            var filteredDeadlines = _deadlines.AsEnumerable();

            if (!string.IsNullOrEmpty(officerFilter))
                filteredDeadlines = filteredDeadlines.Where(d => d.AssignedOfficer == officerFilter);
            if (!string.IsNullOrEmpty(unitFilter))
                filteredDeadlines = filteredDeadlines.Where(d => d.AssignedUnit == unitFilter);

            var deadlinesList = filteredDeadlines.ToList();
            var today = DateTime.Now.Date;

            var dashboard = new DeadlineDashboard
            {
                TotalDeadlines = deadlinesList.Count,
                PendingDeadlines = deadlinesList.Count(d => d.Status == DeadlineStatus.Pending),
                CompletedDeadlines = deadlinesList.Count(d => d.Status == DeadlineStatus.Completed),
                OverdueDeadlines = deadlinesList.Count(d => d.Status == DeadlineStatus.Overdue || (d.Status == DeadlineStatus.Pending && d.DueDate.Date < today)),
                ExtendedDeadlines = deadlinesList.Count(d => d.Status == DeadlineStatus.Extended),
                DueTodayCount = deadlinesList.Count(d => d.DueDate.Date == today && d.Status == DeadlineStatus.Pending),
                DueThisWeekCount = deadlinesList.Count(d => d.DueDate.Date > today && d.DueDate.Date <= today.AddDays(7) && d.Status == DeadlineStatus.Pending),
                CriticalAlerts = _alerts.Count(a => a.Severity == AlertSeverity.Critical),
                HighAlerts = _alerts.Count(a => a.Severity == AlertSeverity.High),
                ComplianceRate = deadlinesList.Any()
                    ? (double)deadlinesList.Count(d => d.Status == DeadlineStatus.Completed) / 
                      deadlinesList.Count(d => d.DueDate.Date <= today || d.Status == DeadlineStatus.Completed) * 100
                    : 100,
                UpcomingDeadlines = deadlinesList
                    .Where(d => d.Status == DeadlineStatus.Pending && d.DueDate.Date >= today)
                    .OrderBy(d => d.DueDate)
                    .Take(10)
                    .ToList(),
                OverdueList = deadlinesList
                    .Where(d => d.Status == DeadlineStatus.Overdue || (d.Status == DeadlineStatus.Pending && d.DueDate.Date < today))
                    .OrderBy(d => d.DueDate)
                    .ToList(),
                RecentAlerts = _alerts.OrderByDescending(a => a.CreatedAt).Take(10).ToList(),
                DeadlinesByType = deadlinesList.GroupBy(d => d.DeadlineType.ToString()).ToDictionary(g => g.Key, g => g.Count()),
                DeadlinesByStatus = deadlinesList.GroupBy(d => d.Status.ToString()).ToDictionary(g => g.Key, g => g.Count())
            };

            return Task.FromResult(dashboard);
        }

        /// <summary>
        /// Get upcoming deadlines for calendar view
        /// </summary>
        public Task<List<CaseDeadline>> GetCalendarDeadlinesAsync(DateTime startDate, DateTime endDate)
        {
            return Task.FromResult(_deadlines
                .Where(d => d.DueDate.Date >= startDate.Date && d.DueDate.Date <= endDate.Date)
                .OrderBy(d => d.DueDate)
                .ToList());
        }

        /// <summary>
        /// Acknowledge an alert
        /// </summary>
        public async Task<bool> AcknowledgeAlertAsync(string alertId, string acknowledgedBy)
        {
            var alert = _alerts.FirstOrDefault(a => a.Id == alertId);
            if (alert == null) return false;

            alert.IsAcknowledged = true;
            alert.AcknowledgedBy = acknowledgedBy;
            alert.AcknowledgedAt = DateTime.Now;
            return true;
        }

        /// <summary>
        /// Get BNSS deadline rules reference
        /// </summary>
        public Task<Dictionary<BNSSDeadlineType, DeadlineRule>> GetBNSSRulesAsync()
        {
            return Task.FromResult(BNSSDeadlineRules.GetAllRules());
        }

        // Public retrieval methods
        public Task<List<CaseDeadline>> GetAllDeadlinesAsync() => Task.FromResult(_deadlines.OrderBy(d => d.DueDate).ToList());
        public Task<CaseDeadline?> GetDeadlineByIdAsync(string id) => Task.FromResult(_deadlines.FirstOrDefault(d => d.Id == id));
        public Task<List<CaseDeadline>> GetDeadlinesByCaseIdAsync(string caseId) => Task.FromResult(_deadlines.Where(d => d.CaseId == caseId).OrderBy(d => d.DueDate).ToList());
        public Task<List<CaseDeadline>> GetDeadlinesByOfficerAsync(string officer) => Task.FromResult(_deadlines.Where(d => d.AssignedOfficer == officer).OrderBy(d => d.DueDate).ToList());
        public Task<List<DeadlineAlert>> GetActiveAlertsAsync() => Task.FromResult(_alerts.Where(a => !a.IsAcknowledged).OrderByDescending(a => a.Severity).ThenBy(a => a.DaysRemaining).ToList());
        public Task<List<DeadlineAlert>> GetAlertsByOfficerAsync(string officer) => Task.FromResult(_alerts.Where(a => a.AssignedOfficer == officer && !a.IsAcknowledged).ToList());

        public Task<List<CaseDeadline>> GetOverdueDeadlinesAsync()
        {
            var today = DateTime.Now.Date;
            return Task.FromResult(_deadlines
                .Where(d => (d.Status == DeadlineStatus.Pending && d.DueDate.Date < today) || d.Status == DeadlineStatus.Overdue)
                .OrderBy(d => d.DueDate)
                .ToList());
        }

        public Task<List<CaseDeadline>> GetCriticalDeadlinesAsync()
        {
            var today = DateTime.Now.Date;
            return Task.FromResult(_deadlines
                .Where(d => d.Status == DeadlineStatus.Pending && d.DueDate.Date <= today.AddDays(7))
                .OrderBy(d => d.DueDate)
                .ToList());
        }

        public async Task RefreshAlertsAsync()
        {
            GenerateAlerts();
        }

        public Task DeleteDeadlineAsync(string id)
        {
            var deadline = _deadlines.FirstOrDefault(d => d.Id == id);
            if (deadline != null)
            {
                _deadlines.Remove(deadline);
                _alerts.RemoveAll(a => a.DeadlineId == id);
            }
            return Task.CompletedTask;
        }
    }
}

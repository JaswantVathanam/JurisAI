using AILegalAsst.Models;
using System.Text.Json;

namespace AILegalAsst.Services;

/// <summary>
/// Service for managing case timeline events
/// </summary>
public class CaseTimelineService
{
    private readonly CaseService _caseService;
    private readonly ILogger<CaseTimelineService> _logger;
    private readonly string _dataFilePath;
    private Dictionary<int, List<CaseTimelineEvent>> _timelineData;
    private int _nextEventId = 1;

    public CaseTimelineService(CaseService caseService, ILogger<CaseTimelineService> logger)
    {
        _caseService = caseService;
        _logger = logger;
        _dataFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "case_timelines.json");
        _timelineData = LoadTimelineData();
        
        // Initialize timelines for existing cases
        InitializeExistingCaseTimelines();
    }

    #region Data Persistence

    private Dictionary<int, List<CaseTimelineEvent>> LoadTimelineData()
    {
        try
        {
            if (File.Exists(_dataFilePath))
            {
                var json = File.ReadAllText(_dataFilePath);
                var data = JsonSerializer.Deserialize<TimelineDataStore>(json);
                if (data != null)
                {
                    _nextEventId = data.NextEventId;
                    return data.Timelines;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading timeline data");
        }
        return new Dictionary<int, List<CaseTimelineEvent>>();
    }

    private void SaveTimelineData()
    {
        try
        {
            var directory = Path.GetDirectoryName(_dataFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var data = new TimelineDataStore
            {
                NextEventId = _nextEventId,
                Timelines = _timelineData
            };
            
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_dataFilePath, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving timeline data");
        }
    }

    private class TimelineDataStore
    {
        public int NextEventId { get; set; } = 1;
        public Dictionary<int, List<CaseTimelineEvent>> Timelines { get; set; } = new();
    }

    #endregion

    #region Timeline Initialization

    /// <summary>
    /// Initialize timelines for existing cases that don't have timeline data
    /// </summary>
    private void InitializeExistingCaseTimelines()
    {
        var cases = _caseService.GetAllCasesAsync().GetAwaiter().GetResult();
        bool hasChanges = false;

        foreach (var caseItem in cases)
        {
            if (!_timelineData.ContainsKey(caseItem.Id))
            {
                _timelineData[caseItem.Id] = GenerateInitialTimeline(caseItem);
                hasChanges = true;
                _logger.LogInformation("Generated initial timeline for case {CaseId}", caseItem.Id);
            }
        }

        if (hasChanges)
        {
            SaveTimelineData();
        }
    }

    /// <summary>
    /// Generate initial timeline events based on existing case data
    /// </summary>
    private List<CaseTimelineEvent> GenerateInitialTimeline(Case caseItem)
    {
        var events = new List<CaseTimelineEvent>();

        // 1. Case Created event
        events.Add(new CaseTimelineEvent
        {
            Id = _nextEventId++,
            CaseId = caseItem.Id,
            EventType = TimelineEventType.CaseCreated,
            Title = "Case Filed",
            Description = $"Case {caseItem.CaseNumber} was created: {caseItem.Title}",
            EventDate = caseItem.FiledDate,
            IsMilestone = true,
            MilestoneType = Models.MilestoneType.CaseFiled,
            StatusAtEvent = CaseStatus.Filed,
            PerformedBy = caseItem.Complainant,
            PerformedByRole = "Complainant"
        });

        // 2. FIR Filed (if exists)
        if (!string.IsNullOrEmpty(caseItem.FIRNumber))
        {
            events.Add(new CaseTimelineEvent
            {
                Id = _nextEventId++,
                CaseId = caseItem.Id,
                EventType = TimelineEventType.FIRFiled,
                Title = "FIR Registered",
                Description = $"FIR No. {caseItem.FIRNumber} registered at {caseItem.PoliceStation ?? "Police Station"}",
                EventDate = caseItem.FiledDate.AddHours(2), // Assume 2 hours after filing
                IsMilestone = true,
                MilestoneType = Models.MilestoneType.FIRRegistered,
                StatusAtEvent = CaseStatus.Filed,
                PerformedBy = caseItem.InvestigatingOfficer ?? "IO",
                PerformedByRole = "Police"
            });
        }

        // 3. Investigation started (if status indicates)
        if (caseItem.Status >= CaseStatus.UnderInvestigation)
        {
            events.Add(new CaseTimelineEvent
            {
                Id = _nextEventId++,
                CaseId = caseItem.Id,
                EventType = TimelineEventType.InvestigationStarted,
                Title = "Investigation Initiated",
                Description = $"Investigation started by {caseItem.InvestigatingOfficer ?? "Investigating Officer"}",
                EventDate = caseItem.FiledDate.AddDays(1),
                StatusAtEvent = CaseStatus.UnderInvestigation,
                PerformedBy = caseItem.InvestigatingOfficer,
                PerformedByRole = "Police"
            });
        }

        // 4. Digital evidence collected (for cybercrime cases)
        if (caseItem.IsCybercrime && caseItem.DigitalEvidenceCollected)
        {
            events.Add(new CaseTimelineEvent
            {
                Id = _nextEventId++,
                CaseId = caseItem.Id,
                EventType = TimelineEventType.DigitalEvidenceSecured,
                Title = "Digital Evidence Secured",
                Description = caseItem.DigitalEvidence ?? "Digital evidence has been collected and secured",
                EventDate = caseItem.FiledDate.AddDays(3),
                StatusAtEvent = CaseStatus.UnderInvestigation,
                PerformedBy = caseItem.InvestigatingOfficer,
                PerformedByRole = "Cyber Cell"
            });
        }

        // 5. Chargesheet filed (if status indicates)
        if (caseItem.Status >= CaseStatus.ChargesheetFiled)
        {
            events.Add(new CaseTimelineEvent
            {
                Id = _nextEventId++,
                CaseId = caseItem.Id,
                EventType = TimelineEventType.ChargesheetFiled,
                Title = "Chargesheet Filed",
                Description = $"Chargesheet submitted to {caseItem.Court ?? "Court"}",
                EventDate = caseItem.FiledDate.AddDays(30),
                IsMilestone = true,
                MilestoneType = Models.MilestoneType.ChargesheetSubmitted,
                StatusAtEvent = CaseStatus.ChargesheetFiled,
                PerformedBy = caseItem.InvestigatingOfficer,
                PerformedByRole = "Police"
            });
        }

        // 6. Trial in progress
        if (caseItem.Status >= CaseStatus.TrialInProgress)
        {
            events.Add(new CaseTimelineEvent
            {
                Id = _nextEventId++,
                CaseId = caseItem.Id,
                EventType = TimelineEventType.HearingScheduled,
                Title = "Trial Commenced",
                Description = $"Trial proceedings started at {caseItem.Court ?? "Court"}",
                EventDate = caseItem.FiledDate.AddDays(60),
                IsMilestone = true,
                MilestoneType = Models.MilestoneType.TrialBegan,
                StatusAtEvent = CaseStatus.TrialInProgress,
                PerformedBy = caseItem.AssignedJudge,
                PerformedByRole = "Court"
            });
        }

        // 7. Next hearing scheduled
        if (caseItem.NextHearingDate.HasValue && caseItem.NextHearingDate > DateTime.Now)
        {
            events.Add(new CaseTimelineEvent
            {
                Id = _nextEventId++,
                CaseId = caseItem.Id,
                EventType = TimelineEventType.HearingScheduled,
                Title = "Next Hearing Scheduled",
                Description = $"Hearing scheduled at {caseItem.Court ?? "Court"}",
                EventDate = caseItem.NextHearingDate.Value,
                PerformedBy = caseItem.AssignedJudge,
                PerformedByRole = "Court"
            });
        }

        // 8. Case closed/judgement
        if (caseItem.Status == CaseStatus.Closed || caseItem.Status == CaseStatus.Judgement)
        {
            events.Add(new CaseTimelineEvent
            {
                Id = _nextEventId++,
                CaseId = caseItem.Id,
                EventType = TimelineEventType.JudgementPronounced,
                Title = "Judgement Delivered",
                Description = "Court has delivered its judgement in this case",
                EventDate = caseItem.LastUpdated ?? DateTime.Now.AddDays(-7),
                IsMilestone = true,
                MilestoneType = Models.MilestoneType.JudgementDelivered,
                StatusAtEvent = CaseStatus.Judgement,
                PerformedBy = caseItem.AssignedJudge,
                PerformedByRole = "Court"
            });
        }

        // 9. Case dismissed
        if (caseItem.Status == CaseStatus.Dismissed)
        {
            events.Add(new CaseTimelineEvent
            {
                Id = _nextEventId++,
                CaseId = caseItem.Id,
                EventType = TimelineEventType.CaseDismissed,
                Title = "Case Dismissed",
                Description = "Case has been dismissed by the court",
                EventDate = caseItem.LastUpdated ?? DateTime.Now.AddDays(-7),
                IsMilestone = true,
                MilestoneType = Models.MilestoneType.CaseResolved,
                StatusAtEvent = CaseStatus.Dismissed,
                PerformedBy = caseItem.AssignedJudge,
                PerformedByRole = "Court"
            });
        }

        // 10. Add documents as events
        foreach (var doc in caseItem.Documents)
        {
            events.Add(new CaseTimelineEvent
            {
                Id = _nextEventId++,
                CaseId = caseItem.Id,
                EventType = TimelineEventType.DocumentUploaded,
                Title = $"Document: {doc.FileName}",
                Description = $"{doc.FileType} uploaded by {doc.UploadedBy}",
                EventDate = doc.UploadedDate,
                PerformedBy = doc.UploadedBy,
                RelatedDocumentId = doc.Id.ToString()
            });
        }

        // Sort by date
        return events.OrderBy(e => e.EventDate).ToList();
    }

    #endregion

    #region Timeline Operations

    /// <summary>
    /// Get timeline events for a specific case
    /// </summary>
    public List<CaseTimelineEvent> GetTimelineForCase(int caseId)
    {
        if (_timelineData.TryGetValue(caseId, out var events))
        {
            return events.OrderBy(e => e.EventDate).ToList();
        }

        // Generate timeline if not exists
        var caseItem = _caseService.GetCaseByIdAsync(caseId).GetAwaiter().GetResult();
        if (caseItem != null)
        {
            var newTimeline = GenerateInitialTimeline(caseItem);
            _timelineData[caseId] = newTimeline;
            SaveTimelineData();
            return newTimeline;
        }

        return new List<CaseTimelineEvent>();
    }

    /// <summary>
    /// Get only milestone events for a case
    /// </summary>
    public List<CaseTimelineEvent> GetMilestonesForCase(int caseId)
    {
        return GetTimelineForCase(caseId)
            .Where(e => e.IsMilestone)
            .OrderBy(e => e.EventDate)
            .ToList();
    }

    /// <summary>
    /// Add a new event to a case timeline
    /// </summary>
    public CaseTimelineEvent AddTimelineEvent(int caseId, TimelineEventType eventType, string title, 
        string description, string? performedBy = null, string? performedByRole = null,
        bool isMilestone = false, MilestoneType? milestoneType = null)
    {
        var caseItem = _caseService.GetCaseByIdAsync(caseId).GetAwaiter().GetResult();
        if (caseItem == null)
        {
            throw new ArgumentException($"Case {caseId} not found");
        }

        var newEvent = new CaseTimelineEvent
        {
            Id = _nextEventId++,
            CaseId = caseId,
            EventType = eventType,
            Title = title,
            Description = description,
            EventDate = DateTime.Now,
            PerformedBy = performedBy,
            PerformedByRole = performedByRole,
            IsMilestone = isMilestone,
            MilestoneType = milestoneType,
            StatusAtEvent = caseItem.Status
        };

        if (!_timelineData.ContainsKey(caseId))
        {
            _timelineData[caseId] = new List<CaseTimelineEvent>();
        }

        _timelineData[caseId].Add(newEvent);
        SaveTimelineData();

        _logger.LogInformation("Added timeline event {EventType} to case {CaseId}", eventType, caseId);
        return newEvent;
    }

    /// <summary>
    /// Add a legal notice sent event
    /// </summary>
    public CaseTimelineEvent AddNoticeSentEvent(int caseId, string noticeType, string recipient, 
        string noticeNumber, string performedBy)
    {
        return AddTimelineEvent(
            caseId,
            TimelineEventType.LegalNoticeSent,
            $"Legal Notice: {noticeType}",
            $"Notice {noticeNumber} sent to {recipient}",
            performedBy,
            "Police"
        );
    }

    /// <summary>
    /// Add a hearing event
    /// </summary>
    public CaseTimelineEvent AddHearingEvent(int caseId, DateTime hearingDate, string court, 
        string description, bool isCompleted = false)
    {
        return AddTimelineEvent(
            caseId,
            isCompleted ? TimelineEventType.HearingCompleted : TimelineEventType.HearingScheduled,
            isCompleted ? "Hearing Completed" : "Hearing Scheduled",
            $"{description} at {court}",
            null,
            "Court"
        );
    }

    /// <summary>
    /// Add a status change event
    /// </summary>
    public CaseTimelineEvent AddStatusChangeEvent(int caseId, CaseStatus oldStatus, CaseStatus newStatus, 
        string performedBy)
    {
        var isMilestone = newStatus == CaseStatus.ChargesheetFiled || 
                          newStatus == CaseStatus.TrialInProgress ||
                          newStatus == CaseStatus.Judgement ||
                          newStatus == CaseStatus.Closed;

        MilestoneType? milestoneType = newStatus switch
        {
            CaseStatus.ChargesheetFiled => Models.MilestoneType.ChargesheetSubmitted,
            CaseStatus.TrialInProgress => Models.MilestoneType.TrialBegan,
            CaseStatus.Judgement => Models.MilestoneType.JudgementDelivered,
            CaseStatus.Closed => Models.MilestoneType.CaseResolved,
            _ => null
        };

        return AddTimelineEvent(
            caseId,
            TimelineEventType.StatusChanged,
            $"Status: {newStatus}",
            $"Case status changed from {oldStatus} to {newStatus}",
            performedBy,
            null,
            isMilestone,
            milestoneType
        );
    }

    /// <summary>
    /// Add a note/comment to timeline
    /// </summary>
    public CaseTimelineEvent AddNote(int caseId, string note, string author, UserRole authorRole)
    {
        var eventType = authorRole switch
        {
            UserRole.Lawyer => TimelineEventType.LawyerComment,
            UserRole.Police => TimelineEventType.PoliceUpdate,
            _ => TimelineEventType.InternalNote
        };

        return AddTimelineEvent(
            caseId,
            eventType,
            "Note Added",
            note,
            author,
            authorRole.ToString()
        );
    }

    /// <summary>
    /// Get timeline statistics for a case
    /// </summary>
    public TimelineStatistics GetTimelineStatistics(int caseId)
    {
        var events = GetTimelineForCase(caseId);
        var caseItem = _caseService.GetCaseByIdAsync(caseId).GetAwaiter().GetResult();

        return new TimelineStatistics
        {
            TotalEvents = events.Count,
            MilestoneCount = events.Count(e => e.IsMilestone),
            DaysSinceFiling = caseItem != null ? (int)(DateTime.Now - caseItem.FiledDate).TotalDays : 0,
            LastActivityDate = events.Any() ? events.Max(e => e.EventDate) : DateTime.MinValue,
            NoticeSentCount = events.Count(e => e.EventType == TimelineEventType.LegalNoticeSent),
            HearingCount = events.Count(e => e.EventType == TimelineEventType.HearingCompleted),
            DocumentCount = events.Count(e => e.EventType == TimelineEventType.DocumentUploaded),
            UpcomingEvents = events.Where(e => e.EventDate > DateTime.Now).OrderBy(e => e.EventDate).ToList()
        };
    }

    #endregion
}

/// <summary>
/// Timeline statistics for a case
/// </summary>
public class TimelineStatistics
{
    public int TotalEvents { get; set; }
    public int MilestoneCount { get; set; }
    public int DaysSinceFiling { get; set; }
    public DateTime LastActivityDate { get; set; }
    public int NoticeSentCount { get; set; }
    public int HearingCount { get; set; }
    public int DocumentCount { get; set; }
    public List<CaseTimelineEvent> UpcomingEvents { get; set; } = new();
}

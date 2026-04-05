namespace AILegalAsst.Models;

/// <summary>
/// Call Detail Record - Individual call/SMS record from telecom provider
/// </summary>
public class CDRRecord
{
    public string Id { get; set; } = string.Empty;
    public string CallingNumber { get; set; } = string.Empty;
    public string CalledNumber { get; set; } = string.Empty;
    public DateTime DateTime { get; set; }
    public TimeSpan Duration { get; set; }
    public CDRType Type { get; set; }
    public string IMEI { get; set; } = string.Empty;
    public string IMSI { get; set; } = string.Empty;
    public string CellTowerId { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string TelecomProvider { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
    public string RoamingStatus { get; set; } = string.Empty;
}

public enum CDRType
{
    Voice,
    SMS,
    Data,
    MissedCall,
    USSD
}

/// <summary>
/// Complete CDR Analysis for a phone number
/// </summary>
public class CDRAnalysis
{
    public string Id { get; set; } = string.Empty;
    public string CaseId { get; set; } = string.Empty;
    public string PrimaryNumber { get; set; } = string.Empty;
    public DateTime AnalysisStartDate { get; set; }
    public DateTime AnalysisEndDate { get; set; }
    public string AnalyzedBy { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    
    // Statistics
    public int TotalRecords { get; set; }
    public int TotalCalls { get; set; }
    public int TotalSMS { get; set; }
    public int TotalDataSessions { get; set; }
    public int UniqueContacts { get; set; }
    public int UniqueTowers { get; set; }
    public int UniqueIMEIs { get; set; }
    
    // Analysis Results
    public List<FrequentContact> FrequentContacts { get; set; } = new();
    public List<CallPattern> DetectedPatterns { get; set; } = new();
    public List<BurstActivity> BurstActivities { get; set; } = new();
    public List<LocationCluster> LocationClusters { get; set; } = new();
    public List<HourlyActivity> HourlyDistribution { get; set; } = new();
    public List<DailyActivity> DailyDistribution { get; set; } = new();
    
    // Raw Records
    public List<CDRRecord> Records { get; set; } = new();
    
    // AI Analysis
    public List<string> AIInsights { get; set; } = new();
    public int RiskScore { get; set; }
}

/// <summary>
/// Detected call pattern (e.g., regular calls at specific times)
/// </summary>
public class CallPattern
{
    public string PatternType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public int OccurrenceCount { get; set; }
    public DateTime? FirstOccurrence { get; set; }
    public DateTime? LastOccurrence { get; set; }
    public List<string> RelatedNumbers { get; set; } = new();
    public int RiskScore { get; set; }
}

/// <summary>
/// Frequently contacted number
/// </summary>
public class FrequentContact
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public int TotalCalls { get; set; }
    public int TotalSMS { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public DateTime FirstContact { get; set; }
    public DateTime LastContact { get; set; }
    public int RiskScore { get; set; }
    public string Notes { get; set; } = string.Empty;
}

/// <summary>
/// Burst activity - sudden spike in communication
/// </summary>
public class BurstActivity
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int CallCount { get; set; }
    public int SMSCount { get; set; }
    public int UniqueNumbers { get; set; }
    public int PeakCallsPerMinute { get; set; }
    public string TriggerReason { get; set; } = string.Empty;
}

/// <summary>
/// Location cluster from cell tower data
/// </summary>
public class LocationCluster
{
    public string ClusterName { get; set; } = string.Empty;
    public List<string> CellTowerIds { get; set; } = new();
    public string Area { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public int TotalHits { get; set; }
    public DateTime FirstSeen { get; set; }
    public DateTime LastSeen { get; set; }
    public TimeSpan AverageTimeSpent { get; set; }
}

/// <summary>
/// Timeline event for visualization
/// </summary>
public class TimelineEvent
{
    public DateTime DateTime { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string RelatedNumber { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
}

/// <summary>
/// Hourly activity summary for heat map
/// </summary>
public class HourlyActivity
{
    public int Hour { get; set; }
    public int CallCount { get; set; }
    public int SMSCount { get; set; }
    public int DataSessions { get; set; }
}

/// <summary>
/// Daily activity summary
/// </summary>
public class DailyActivity
{
    public DateTime Date { get; set; }
    public int CallCount { get; set; }
    public int SMSCount { get; set; }
    public int DataSessions { get; set; }
    public int UniqueContacts { get; set; }
    public TimeSpan TotalDuration { get; set; }
}

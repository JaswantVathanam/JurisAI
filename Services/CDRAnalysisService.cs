using AILegalAsst.Models;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AILegalAsst.Services
{
    /// <summary>
    /// Service for analyzing Call Detail Records (CDR) to detect patterns,
    /// identify suspicious activity, and generate investigation insights.
    /// </summary>
    public class CDRAnalysisService
    {
        private readonly List<CDRRecord> _records = new();
        private readonly List<CDRAnalysis> _analyses = new();
        private readonly ILogger<CDRAnalysisService> _logger;
        private readonly AzureAgentService _agentService;

        public CDRAnalysisService(ILogger<CDRAnalysisService> logger, AzureAgentService agentService)
        {
            _logger = logger;
            _agentService = agentService;
            InitializeSampleData();
        }

        private void InitializeSampleData()
        {
            // Sample CDR analysis for demonstration
            var sampleAnalysis = new CDRAnalysis
            {
                Id = "CDR-2024-001",
                CaseId = "FIR-2024-1247",
                PrimaryNumber = "+91-9876543210",
                AnalysisStartDate = DateTime.Now.AddDays(-30),
                AnalysisEndDate = DateTime.Now,
                TotalRecords = 1247,
                TotalCalls = 892,
                TotalSMS = 312,
                TotalDataSessions = 43,
                UniqueContacts = 67,
                Status = "Completed",
                CreatedAt = DateTime.Now.AddDays(-1),
                AnalyzedBy = "Inspector Rajesh Kumar",
                FrequentContacts = new List<FrequentContact>
                {
                    new() { PhoneNumber = "+91-9988776655", ContactName = "Unknown", TotalCalls = 156, TotalSMS = 45, TotalDuration = TimeSpan.FromHours(12.5), FirstContact = DateTime.Now.AddDays(-28), LastContact = DateTime.Now.AddDays(-1), RiskScore = 85, Notes = "High frequency contact during incident period" },
                    new() { PhoneNumber = "+91-8877665544", ContactName = "Suspect B", TotalCalls = 89, TotalSMS = 23, TotalDuration = TimeSpan.FromHours(6.2), FirstContact = DateTime.Now.AddDays(-25), LastContact = DateTime.Now.AddDays(-3), RiskScore = 72 },
                    new() { PhoneNumber = "+91-7766554433", ContactName = "Unknown", TotalCalls = 67, TotalSMS = 12, TotalDuration = TimeSpan.FromHours(4.1), FirstContact = DateTime.Now.AddDays(-20), LastContact = DateTime.Now.AddDays(-5), RiskScore = 45 }
                },
                DetectedPatterns = new List<CallPattern>
                {
                    new() { PatternType = "Late Night Cluster", Description = "Significant call activity between 11 PM - 3 AM on multiple days", Severity = "High", OccurrenceCount = 23, FirstOccurrence = DateTime.Now.AddDays(-15), LastOccurrence = DateTime.Now.AddDays(-2), RelatedNumbers = new List<string> { "+91-9988776655", "+91-8877665544" }, RiskScore = 78 },
                    new() { PatternType = "Burst Activity", Description = "12 calls within 30 minutes on incident date", Severity = "Critical", OccurrenceCount = 1, FirstOccurrence = DateTime.Now.AddDays(-7), LastOccurrence = DateTime.Now.AddDays(-7), RelatedNumbers = new List<string> { "+91-9988776655" }, RiskScore = 92 },
                    new() { PatternType = "Tower Hopping", Description = "Rapid movement between 5 cell towers in 2 hours", Severity = "Medium", OccurrenceCount = 3, FirstOccurrence = DateTime.Now.AddDays(-10), LastOccurrence = DateTime.Now.AddDays(-7), RiskScore = 65 }
                },
                BurstActivities = new List<BurstActivity>
                {
                    new() { StartTime = DateTime.Now.AddDays(-7).AddHours(14), EndTime = DateTime.Now.AddDays(-7).AddHours(14.5), CallCount = 12, SMSCount = 3, UniqueNumbers = 2, PeakCallsPerMinute = 4, TriggerReason = "Pre-incident communication spike" }
                },
                LocationClusters = new List<LocationCluster>
                {
                    new() { ClusterName = "Primary Location", CellTowerIds = new List<string> { "MH-MUM-4521", "MH-MUM-4522", "MH-MUM-4523" }, Area = "Andheri East, Mumbai", Latitude = 19.1136, Longitude = 72.8697, TotalHits = 456, FirstSeen = DateTime.Now.AddDays(-30), LastSeen = DateTime.Now.AddDays(-1), AverageTimeSpent = TimeSpan.FromHours(8) },
                    new() { ClusterName = "Secondary Location", CellTowerIds = new List<string> { "MH-MUM-3215", "MH-MUM-3216" }, Area = "Bandra West, Mumbai", Latitude = 19.0596, Longitude = 72.8295, TotalHits = 189, FirstSeen = DateTime.Now.AddDays(-25), LastSeen = DateTime.Now.AddDays(-3), AverageTimeSpent = TimeSpan.FromHours(3) }
                },
                HourlyDistribution = GenerateSampleHourlyDistribution(),
                DailyDistribution = GenerateSampleDailyDistribution(),
                AIInsights = new List<string>
                {
                    "🔴 Critical: Burst activity detected 2 hours before reported incident time",
                    "🟠 High Risk: Primary contact (+91-9988776655) shows 85% risk score based on communication patterns",
                    "🟡 Notable: Late night communication cluster suggests coordination outside normal hours",
                    "🔵 Pattern: Subject frequently visited Andheri East area (456 cell tower hits)",
                    "🟢 Recommendation: Request CDR for +91-9988776655 to establish complete communication chain"
                },
                RiskScore = 82
            };

            _analyses.Add(sampleAnalysis);
        }

        private static List<HourlyActivity> GenerateSampleHourlyDistribution()
        {
            var distribution = new List<HourlyActivity>();
            var random = new Random(42);
            for (int hour = 0; hour < 24; hour++)
            {
                int baseCount = hour switch
                {
                    >= 9 and <= 11 => random.Next(30, 50),
                    >= 14 and <= 17 => random.Next(35, 55),
                    >= 20 and <= 23 => random.Next(25, 45),
                    >= 0 and <= 3 => random.Next(15, 30), // Suspicious late night
                    _ => random.Next(5, 20)
                };
                distribution.Add(new HourlyActivity { Hour = hour, CallCount = baseCount, SMSCount = random.Next(5, 15), DataSessions = random.Next(1, 5) });
            }
            return distribution;
        }

        private static List<DailyActivity> GenerateSampleDailyDistribution()
        {
            var distribution = new List<DailyActivity>();
            var random = new Random(42);
            for (int i = 30; i >= 0; i--)
            {
                var date = DateTime.Now.AddDays(-i).Date;
                distribution.Add(new DailyActivity
                {
                    Date = date,
                    CallCount = random.Next(20, 60),
                    SMSCount = random.Next(5, 20),
                    DataSessions = random.Next(1, 10),
                    UniqueContacts = random.Next(5, 15),
                    TotalDuration = TimeSpan.FromMinutes(random.Next(30, 180))
                });
            }
            return distribution;
        }

        /// <summary>
        /// Parse CDR data from CSV content
        /// </summary>
        public async Task<(List<CDRRecord> Records, string Error)> ParseCDRFromCSV(string csvContent, string phoneNumber)
        {
            var records = new List<CDRRecord>();
            try
            {
                var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length < 2)
                    return (records, "CSV file must contain header and at least one data row");

                // Parse header to identify columns
                var headers = lines[0].Split(',').Select(h => h.Trim().ToLower()).ToArray();
                var columnMap = MapColumns(headers);

                for (int i = 1; i < lines.Length; i++)
                {
                    var values = ParseCSVLine(lines[i]);
                    if (values.Length < 3) continue;

                    var record = new CDRRecord
                    {
                        Id = $"CDR-REC-{Guid.NewGuid():N}",
                        CallingNumber = GetColumnValue(values, columnMap, "calling", phoneNumber),
                        CalledNumber = GetColumnValue(values, columnMap, "called", ""),
                        DateTime = ParseDateTime(GetColumnValue(values, columnMap, "datetime", "")),
                        Duration = ParseDuration(GetColumnValue(values, columnMap, "duration", "0")),
                        Type = ParseCDRType(GetColumnValue(values, columnMap, "type", "Voice")),
                        CellTowerId = GetColumnValue(values, columnMap, "tower", ""),
                        IMEI = GetColumnValue(values, columnMap, "imei", ""),
                        IMSI = GetColumnValue(values, columnMap, "imsi", ""),
                        Location = GetColumnValue(values, columnMap, "location", ""),
                        Latitude = ParseDouble(GetColumnValue(values, columnMap, "latitude", "")),
                        Longitude = ParseDouble(GetColumnValue(values, columnMap, "longitude", "")),
                        Direction = DetermineDirection(phoneNumber, GetColumnValue(values, columnMap, "calling", ""), GetColumnValue(values, columnMap, "called", "")),
                        RoamingStatus = GetColumnValue(values, columnMap, "roaming", "Local")
                    };
                    records.Add(record);
                }

                _records.AddRange(records);
                _logger.LogInformation("Parsed {Count} CDR records for {Phone}", records.Count, phoneNumber);
                return (records, "");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing CDR CSV");
                return (records, $"Error parsing CSV: {ex.Message}");
            }
        }

        private Dictionary<string, int> MapColumns(string[] headers)
        {
            var map = new Dictionary<string, int>();
            for (int i = 0; i < headers.Length; i++)
            {
                var h = headers[i].ToLower();
                if (h.Contains("calling") || h.Contains("from") || h.Contains("a_number")) map["calling"] = i;
                else if (h.Contains("called") || h.Contains("to") || h.Contains("b_number")) map["called"] = i;
                else if (h.Contains("date") || h.Contains("time") || h.Contains("timestamp")) map["datetime"] = i;
                else if (h.Contains("duration") || h.Contains("length")) map["duration"] = i;
                else if (h.Contains("type") || h.Contains("call_type")) map["type"] = i;
                else if (h.Contains("tower") || h.Contains("cell") || h.Contains("lac")) map["tower"] = i;
                else if (h.Contains("imei")) map["imei"] = i;
                else if (h.Contains("imsi")) map["imsi"] = i;
                else if (h.Contains("location") || h.Contains("address")) map["location"] = i;
                else if (h.Contains("lat")) map["latitude"] = i;
                else if (h.Contains("lon") || h.Contains("lng")) map["longitude"] = i;
                else if (h.Contains("roam")) map["roaming"] = i;
            }
            return map;
        }

        private string[] ParseCSVLine(string line)
        {
            var values = new List<string>();
            bool inQuotes = false;
            var current = new StringBuilder();

            foreach (char c in line)
            {
                if (c == '"') inQuotes = !inQuotes;
                else if (c == ',' && !inQuotes)
                {
                    values.Add(current.ToString().Trim());
                    current.Clear();
                }
                else current.Append(c);
            }
            values.Add(current.ToString().Trim());
            return values.ToArray();
        }

        private string GetColumnValue(string[] values, Dictionary<string, int> map, string key, string defaultValue)
        {
            if (map.TryGetValue(key, out int index) && index < values.Length)
                return values[index];
            return defaultValue;
        }

        private DateTime ParseDateTime(string value)
        {
            if (DateTime.TryParse(value, out var dt)) return dt;
            string[] formats = { "dd/MM/yyyy HH:mm:ss", "MM/dd/yyyy HH:mm:ss", "yyyy-MM-dd HH:mm:ss", "dd-MM-yyyy HH:mm:ss" };
            if (DateTime.TryParseExact(value, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt)) return dt;
            return DateTime.Now;
        }

        private TimeSpan ParseDuration(string value)
        {
            if (int.TryParse(value, out int seconds)) return TimeSpan.FromSeconds(seconds);
            if (TimeSpan.TryParse(value, out var ts)) return ts;
            return TimeSpan.Zero;
        }

        private CDRType ParseCDRType(string value)
        {
            return value.ToLower() switch
            {
                "sms" or "message" => CDRType.SMS,
                "data" or "gprs" => CDRType.Data,
                "missed" or "missed call" => CDRType.MissedCall,
                "ussd" => CDRType.USSD,
                _ => CDRType.Voice
            };
        }

        private double? ParseDouble(string value)
        {
            if (double.TryParse(value, out double d)) return d;
            return null;
        }

        private string DetermineDirection(string primaryNumber, string calling, string called)
        {
            if (calling.Contains(primaryNumber.Replace("+91-", "").Replace("-", ""))) return "Outgoing";
            return "Incoming";
        }

        /// <summary>
        /// Perform comprehensive CDR analysis
        /// </summary>
        public async Task<CDRAnalysis> AnalyzeCDRData(string caseId, string phoneNumber, List<CDRRecord> records, string analyzedBy)
        {
            var analysis = new CDRAnalysis
            {
                Id = $"CDR-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}",
                CaseId = caseId,
                PrimaryNumber = phoneNumber,
                TotalRecords = records.Count,
                TotalCalls = records.Count(r => r.Type == CDRType.Voice || r.Type == CDRType.MissedCall),
                TotalSMS = records.Count(r => r.Type == CDRType.SMS),
                TotalDataSessions = records.Count(r => r.Type == CDRType.Data),
                AnalysisStartDate = records.Min(r => r.DateTime),
                AnalysisEndDate = records.Max(r => r.DateTime),
                Status = "Processing",
                CreatedAt = DateTime.Now,
                AnalyzedBy = analyzedBy
            };

            // Analyze frequent contacts
            analysis.FrequentContacts = AnalyzeFrequentContacts(records, phoneNumber);
            analysis.UniqueContacts = analysis.FrequentContacts.Count;

            // Detect patterns
            analysis.DetectedPatterns = DetectCallPatterns(records);

            // Detect burst activities
            analysis.BurstActivities = DetectBurstActivities(records);

            // Analyze location clusters
            analysis.LocationClusters = AnalyzeLocationClusters(records);

            // Generate hourly and daily distributions
            analysis.HourlyDistribution = GenerateHourlyDistribution(records);
            analysis.DailyDistribution = GenerateDailyDistribution(records);

            // Generate AI insights
            analysis.AIInsights = GenerateAIInsights(analysis);

            // Calculate overall risk score
            analysis.RiskScore = CalculateRiskScore(analysis);

            analysis.Status = "Completed";
            analysis.CompletedAt = DateTime.Now;

            _analyses.Add(analysis);
            _logger.LogInformation("Completed CDR analysis {Id} for case {CaseId}", analysis.Id, caseId);

            return analysis;
        }

        private List<FrequentContact> AnalyzeFrequentContacts(List<CDRRecord> records, string primaryNumber)
        {
            var contactStats = records
                .Where(r => r.Type == CDRType.Voice || r.Type == CDRType.SMS)
                .GroupBy(r => r.Direction == "Outgoing" ? r.CalledNumber : r.CallingNumber)
                .Where(g => !string.IsNullOrEmpty(g.Key) && !g.Key.Contains(primaryNumber.Replace("+91-", "").Replace("-", "")))
                .Select(g => new FrequentContact
                {
                    PhoneNumber = g.Key,
                    ContactName = "Unknown",
                    TotalCalls = g.Count(r => r.Type == CDRType.Voice),
                    TotalSMS = g.Count(r => r.Type == CDRType.SMS),
                    TotalDuration = TimeSpan.FromSeconds(g.Where(r => r.Type == CDRType.Voice).Sum(r => r.Duration.TotalSeconds)),
                    FirstContact = g.Min(r => r.DateTime),
                    LastContact = g.Max(r => r.DateTime),
                    RiskScore = CalculateContactRiskScore(g.ToList())
                })
                .OrderByDescending(c => c.TotalCalls + c.TotalSMS)
                .Take(20)
                .ToList();

            return contactStats;
        }

        private int CalculateContactRiskScore(List<CDRRecord> contactRecords)
        {
            int score = 0;

            // High frequency
            if (contactRecords.Count > 50) score += 20;
            else if (contactRecords.Count > 20) score += 10;

            // Late night calls
            int lateNightCalls = contactRecords.Count(r => r.DateTime.Hour >= 23 || r.DateTime.Hour <= 4);
            if (lateNightCalls > 10) score += 25;
            else if (lateNightCalls > 5) score += 15;

            // Short duration calls (potential coordination)
            int shortCalls = contactRecords.Count(r => r.Duration.TotalSeconds > 0 && r.Duration.TotalSeconds < 30);
            if (shortCalls > 20) score += 15;

            // Burst patterns
            var sortedRecords = contactRecords.OrderBy(r => r.DateTime).ToList();
            for (int i = 1; i < sortedRecords.Count; i++)
            {
                if ((sortedRecords[i].DateTime - sortedRecords[i - 1].DateTime).TotalMinutes < 5)
                    score += 2;
            }

            return Math.Min(100, score);
        }

        private List<CallPattern> DetectCallPatterns(List<CDRRecord> records)
        {
            var patterns = new List<CallPattern>();

            // Late Night Pattern
            var lateNightCalls = records.Where(r => r.DateTime.Hour >= 23 || r.DateTime.Hour <= 4).ToList();
            if (lateNightCalls.Count >= 10)
            {
                patterns.Add(new CallPattern
                {
                    PatternType = "Late Night Cluster",
                    Description = $"Significant activity ({lateNightCalls.Count} records) between 11 PM - 4 AM",
                    Severity = lateNightCalls.Count > 30 ? "High" : "Medium",
                    OccurrenceCount = lateNightCalls.Count,
                    FirstOccurrence = lateNightCalls.Min(r => r.DateTime),
                    LastOccurrence = lateNightCalls.Max(r => r.DateTime),
                    RelatedNumbers = lateNightCalls.Select(r => r.Direction == "Outgoing" ? r.CalledNumber : r.CallingNumber).Distinct().Take(5).ToList(),
                    RiskScore = Math.Min(100, 50 + lateNightCalls.Count)
                });
            }

            // Weekend Activity Pattern
            var weekendCalls = records.Where(r => r.DateTime.DayOfWeek == DayOfWeek.Saturday || r.DateTime.DayOfWeek == DayOfWeek.Sunday).ToList();
            if (weekendCalls.Count >= 20)
            {
                patterns.Add(new CallPattern
                {
                    PatternType = "Weekend Activity",
                    Description = $"High weekend communication ({weekendCalls.Count} records)",
                    Severity = "Low",
                    OccurrenceCount = weekendCalls.Count,
                    FirstOccurrence = weekendCalls.Min(r => r.DateTime),
                    LastOccurrence = weekendCalls.Max(r => r.DateTime),
                    RiskScore = 30
                });
            }

            // Regular Schedule Pattern
            var regularHours = records.Where(r => r.DateTime.Hour >= 9 && r.DateTime.Hour <= 18).ToList();
            if (regularHours.Count > records.Count * 0.7)
            {
                patterns.Add(new CallPattern
                {
                    PatternType = "Regular Schedule",
                    Description = "Majority of activity during regular business hours (9 AM - 6 PM)",
                    Severity = "Info",
                    OccurrenceCount = regularHours.Count,
                    RiskScore = 10
                });
            }

            // Short Call Pattern (possible coordination)
            var shortCalls = records.Where(r => r.Duration.TotalSeconds > 0 && r.Duration.TotalSeconds < 15).ToList();
            if (shortCalls.Count >= 15)
            {
                patterns.Add(new CallPattern
                {
                    PatternType = "Short Call Pattern",
                    Description = $"{shortCalls.Count} calls under 15 seconds - potential coordination signals",
                    Severity = "Medium",
                    OccurrenceCount = shortCalls.Count,
                    FirstOccurrence = shortCalls.Min(r => r.DateTime),
                    LastOccurrence = shortCalls.Max(r => r.DateTime),
                    RiskScore = 55
                });
            }

            return patterns;
        }

        private List<BurstActivity> DetectBurstActivities(List<CDRRecord> records)
        {
            var bursts = new List<BurstActivity>();
            var sortedRecords = records.OrderBy(r => r.DateTime).ToList();

            for (int i = 0; i < sortedRecords.Count; i++)
            {
                var windowEnd = sortedRecords[i].DateTime.AddMinutes(30);
                var windowRecords = sortedRecords.Where(r => r.DateTime >= sortedRecords[i].DateTime && r.DateTime <= windowEnd).ToList();

                if (windowRecords.Count >= 10)
                {
                    // Check if we already captured this burst
                    if (!bursts.Any(b => b.StartTime <= sortedRecords[i].DateTime && b.EndTime >= sortedRecords[i].DateTime))
                    {
                        var peakMinute = windowRecords
                            .GroupBy(r => new DateTime(r.DateTime.Year, r.DateTime.Month, r.DateTime.Day, r.DateTime.Hour, r.DateTime.Minute, 0))
                            .Max(g => g.Count());

                        bursts.Add(new BurstActivity
                        {
                            StartTime = windowRecords.Min(r => r.DateTime),
                            EndTime = windowRecords.Max(r => r.DateTime),
                            CallCount = windowRecords.Count(r => r.Type == CDRType.Voice),
                            SMSCount = windowRecords.Count(r => r.Type == CDRType.SMS),
                            UniqueNumbers = windowRecords.Select(r => r.Direction == "Outgoing" ? r.CalledNumber : r.CallingNumber).Distinct().Count(),
                            PeakCallsPerMinute = peakMinute,
                            TriggerReason = "High activity concentration detected"
                        });
                    }
                }
            }

            return bursts.OrderByDescending(b => b.CallCount + b.SMSCount).Take(10).ToList();
        }

        private List<LocationCluster> AnalyzeLocationClusters(List<CDRRecord> records)
        {
            var clusters = records
                .Where(r => !string.IsNullOrEmpty(r.CellTowerId))
                .GroupBy(r => r.CellTowerId.Substring(0, Math.Min(r.CellTowerId.Length, 10)))
                .Where(g => g.Count() >= 5)
                .Select(g => new LocationCluster
                {
                    ClusterName = $"Location Cluster {g.Key}",
                    CellTowerIds = g.Select(r => r.CellTowerId).Distinct().ToList(),
                    Area = g.FirstOrDefault(r => !string.IsNullOrEmpty(r.Location))?.Location ?? "Unknown Area",
                    Latitude = g.Where(r => r.Latitude.HasValue).Select(r => r.Latitude!.Value).FirstOrDefault(),
                    Longitude = g.Where(r => r.Longitude.HasValue).Select(r => r.Longitude!.Value).FirstOrDefault(),
                    TotalHits = g.Count(),
                    FirstSeen = g.Min(r => r.DateTime),
                    LastSeen = g.Max(r => r.DateTime),
                    AverageTimeSpent = TimeSpan.FromMinutes(g.Count() * 5) // Estimate
                })
                .OrderByDescending(c => c.TotalHits)
                .Take(10)
                .ToList();

            // Rename clusters
            for (int i = 0; i < clusters.Count; i++)
            {
                clusters[i].ClusterName = i == 0 ? "Primary Location" : i == 1 ? "Secondary Location" : $"Location #{i + 1}";
            }

            return clusters;
        }

        private List<HourlyActivity> GenerateHourlyDistribution(List<CDRRecord> records)
        {
            return Enumerable.Range(0, 24)
                .Select(hour => new HourlyActivity
                {
                    Hour = hour,
                    CallCount = records.Count(r => r.DateTime.Hour == hour && r.Type == CDRType.Voice),
                    SMSCount = records.Count(r => r.DateTime.Hour == hour && r.Type == CDRType.SMS),
                    DataSessions = records.Count(r => r.DateTime.Hour == hour && r.Type == CDRType.Data)
                })
                .ToList();
        }

        private List<DailyActivity> GenerateDailyDistribution(List<CDRRecord> records)
        {
            return records
                .GroupBy(r => r.DateTime.Date)
                .Select(g => new DailyActivity
                {
                    Date = g.Key,
                    CallCount = g.Count(r => r.Type == CDRType.Voice),
                    SMSCount = g.Count(r => r.Type == CDRType.SMS),
                    DataSessions = g.Count(r => r.Type == CDRType.Data),
                    UniqueContacts = g.Select(r => r.Direction == "Outgoing" ? r.CalledNumber : r.CallingNumber).Distinct().Count(),
                    TotalDuration = TimeSpan.FromSeconds(g.Where(r => r.Type == CDRType.Voice).Sum(r => r.Duration.TotalSeconds))
                })
                .OrderBy(d => d.Date)
                .ToList();
        }

        private List<string> GenerateAIInsights(CDRAnalysis analysis)
        {
            // Try AI Agent for intelligent narrative insights
            if (_agentService.IsReady)
            {
                try
                {
                    var dataSummary = $"CDR Analysis Summary for case {analysis.CaseId}:\n" +
                        $"- Primary Number: {analysis.PrimaryNumber}\n" +
                        $"- Period: {analysis.AnalysisStartDate:dd MMM yyyy} to {analysis.AnalysisEndDate:dd MMM yyyy}\n" +
                        $"- Total Records: {analysis.TotalRecords} (Calls: {analysis.TotalCalls}, SMS: {analysis.TotalSMS})\n" +
                        $"- Unique Contacts: {analysis.UniqueContacts}\n" +
                        $"- High Risk Contacts: {analysis.FrequentContacts.Count(c => c.RiskScore >= 70)}\n" +
                        $"- Burst Activities: {analysis.BurstActivities.Count}\n" +
                        $"- Detected Patterns: {string.Join(", ", analysis.DetectedPatterns.Select(p => $"{p.PatternType} ({p.Severity})"))}\n" +
                        $"- Location Clusters: {string.Join(", ", analysis.LocationClusters.Select(l => $"{l.Area} ({l.TotalHits} hits)"))}\n" +
                        $"- Risk Score: {analysis.RiskScore}/100";

                    var context = "You are a CDR (Call Detail Records) analysis expert for Indian law enforcement. " +
                        "Generate 5-7 concise investigation insights from this CDR analysis data. " +
                        "Each insight should start with an emoji indicator: 🔴 Critical, 🟠 High Risk, 🟡 Notable, 🔵 Pattern, 🟢 Recommendation, 📊 Statistics. " +
                        "Focus on actionable intelligence for the investigating officer.";

                    var response = _agentService.SendMessageAsync(dataSummary, context).GetAwaiter().GetResult();
                    if (response.Success && !string.IsNullOrWhiteSpace(response.Message))
                    {
                        var aiInsights = response.Message
                            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                            .Where(line => line.Trim().Length > 10)
                            .Select(line => line.Trim().TrimStart('-', '*', ' '))
                            .Where(line => !string.IsNullOrWhiteSpace(line))
                            .ToList();

                        if (aiInsights.Count >= 3)
                            return aiInsights;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "AI Agent CDR insights generation failed, using template-based insights");
                }
            }

            // Fallback: Template-based insights
            var insights = new List<string>();

            // Burst activity insight
            if (analysis.BurstActivities.Any())
            {
                var maxBurst = analysis.BurstActivities.OrderByDescending(b => b.CallCount + b.SMSCount).First();
                insights.Add($"🔴 Critical: Burst activity detected - {maxBurst.CallCount + maxBurst.SMSCount} communications in 30 minutes on {maxBurst.StartTime:dd MMM yyyy HH:mm}");
            }

            // High risk contact insight
            var highRiskContacts = analysis.FrequentContacts.Where(c => c.RiskScore >= 70).ToList();
            if (highRiskContacts.Any())
            {
                insights.Add($"🟠 High Risk: {highRiskContacts.Count} contact(s) with risk score above 70% identified");
            }

            // Late night pattern insight
            var lateNightPattern = analysis.DetectedPatterns.FirstOrDefault(p => p.PatternType == "Late Night Cluster");
            if (lateNightPattern != null)
            {
                insights.Add($"🟡 Notable: {lateNightPattern.OccurrenceCount} late night communications detected - possible covert coordination");
            }

            // Location insight
            if (analysis.LocationClusters.Any())
            {
                var primaryLocation = analysis.LocationClusters.First();
                insights.Add($"🔵 Location: Subject primarily located at {primaryLocation.Area} ({primaryLocation.TotalHits} cell tower hits)");
            }

            // Communication span insight
            var span = analysis.AnalysisEndDate - analysis.AnalysisStartDate;
            var avgDaily = analysis.TotalRecords / Math.Max(1, span.Days);
            insights.Add($"📊 Statistics: Average {avgDaily:F0} communications/day over {span.Days} days");

            // Recommendation
            if (analysis.FrequentContacts.Any(c => c.RiskScore >= 50))
            {
                var topRiskContact = analysis.FrequentContacts.OrderByDescending(c => c.RiskScore).First();
                insights.Add($"🟢 Recommendation: Request CDR for {topRiskContact.PhoneNumber} to establish complete communication chain");
            }

            return insights;
        }

        private int CalculateRiskScore(CDRAnalysis analysis)
        {
            int score = 0;

            // Burst activities contribute heavily
            score += Math.Min(30, analysis.BurstActivities.Count * 10);

            // High risk contacts
            score += Math.Min(25, analysis.FrequentContacts.Count(c => c.RiskScore >= 70) * 5);

            // Pattern severity
            score += analysis.DetectedPatterns.Count(p => p.Severity == "Critical") * 15;
            score += analysis.DetectedPatterns.Count(p => p.Severity == "High") * 10;
            score += analysis.DetectedPatterns.Count(p => p.Severity == "Medium") * 5;

            // Late night activity ratio
            var lateNightRatio = analysis.HourlyDistribution
                .Where(h => h.Hour >= 23 || h.Hour <= 4)
                .Sum(h => h.CallCount + h.SMSCount) / (double)Math.Max(1, analysis.TotalRecords);
            if (lateNightRatio > 0.2) score += 15;
            else if (lateNightRatio > 0.1) score += 10;

            return Math.Min(100, score);
        }

        // Public methods for retrieving data
        public Task<List<CDRAnalysis>> GetAllAnalysesAsync() => Task.FromResult(_analyses.OrderByDescending(a => a.CreatedAt).ToList());
        public Task<CDRAnalysis?> GetAnalysisByIdAsync(string id) => Task.FromResult(_analyses.FirstOrDefault(a => a.Id == id));
        public Task<List<CDRAnalysis>> GetAnalysesByCaseIdAsync(string caseId) => Task.FromResult(_analyses.Where(a => a.CaseId == caseId).ToList());

        public Task<List<TimelineEvent>> GetTimelineEventsAsync(string analysisId)
        {
            var analysis = _analyses.FirstOrDefault(a => a.Id == analysisId);
            if (analysis == null) return Task.FromResult(new List<TimelineEvent>());

            var events = new List<TimelineEvent>();

            // Add burst events
            foreach (var burst in analysis.BurstActivities)
            {
                events.Add(new TimelineEvent
                {
                    DateTime = burst.StartTime,
                    EventType = "Burst Activity",
                    Description = $"{burst.CallCount} calls, {burst.SMSCount} SMS in 30 min",
                    Severity = "Critical",
                    RelatedNumber = "",
                    Location = ""
                });
            }

            // Add pattern detection events
            foreach (var pattern in analysis.DetectedPatterns.Where(p => p.FirstOccurrence.HasValue))
            {
                events.Add(new TimelineEvent
                {
                    DateTime = pattern.FirstOccurrence!.Value,
                    EventType = pattern.PatternType,
                    Description = pattern.Description,
                    Severity = pattern.Severity,
                    RelatedNumber = pattern.RelatedNumbers?.FirstOrDefault() ?? ""
                });
            }

            return Task.FromResult(events.OrderBy(e => e.DateTime).ToList());
        }

        public Task DeleteAnalysisAsync(string id)
        {
            var analysis = _analyses.FirstOrDefault(a => a.Id == id);
            if (analysis != null) _analyses.Remove(analysis);
            return Task.CompletedTask;
        }
    }
}

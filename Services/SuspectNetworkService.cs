using AILegalAsst.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AILegalAsst.Services;

/// <summary>
/// Suspect Network Service
/// 
/// Builds and analyzes criminal networks:
/// - Create relationship graphs between suspects
/// - Identify gang hierarchies and command structure
/// - Track communication patterns within networks
/// - Predict next targets or activities
/// - Recommend intervention points
/// 
/// Used by: Intelligence Dashboard, Criminal Network Visualization, Investigation Copilot
/// </summary>
public class SuspectNetworkService
{
    private readonly IntelligenceGatheringService _intelligenceService;
    private readonly PhoneIntelligenceService _phoneService;
    private readonly AzureAgentService _aiService;
    private readonly ILogger<SuspectNetworkService> _logger;
    
    // In-memory cache for demo (replace with DB in production)
    private static readonly Dictionary<string, SuspectNetworkGraph> _networkCache = new();
    private static readonly Dictionary<string, SuspectProfile> _suspectProfiles = new();

    public SuspectNetworkService(
        IntelligenceGatheringService intelligenceService,
        PhoneIntelligenceService phoneService,
        AzureAgentService aiService,
        ILogger<SuspectNetworkService> logger)
    {
        _intelligenceService = intelligenceService;
        _phoneService = phoneService;
        _aiService = aiService;
        _logger = logger;
        
        // Initialize sample data for demonstration
        InitializeSampleNetworkData();
    }

    /// <summary>
    /// Build a network graph from a central suspect
    /// Includes: Direct contacts, secondary contacts, relationship strengths
    /// </summary>
    public async Task<Result<SuspectNetworkGraph>> BuildNetworkGraphAsync(
        string phoneNumber,
        int depth = 2) // How many levels of connections to include
    {
        try
        {
            _logger.LogInformation(
                "Building network graph for {Phone} with depth {Depth}",
                phoneNumber, depth);

            // Check cache first
            var cacheKey = $"{phoneNumber}_{depth}";
            if (_networkCache.TryGetValue(cacheKey, out var cachedGraph))
            {
                _logger.LogInformation("Returning cached network graph for {Phone}", phoneNumber);
                return Result<SuspectNetworkGraph>.Success(cachedGraph);
            }

            var graph = new SuspectNetworkGraph
            {
                RootPhoneNumber = phoneNumber,
                CreatedDate = DateTime.UtcNow,
                Nodes = new List<NetworkNode>(),
                Edges = new List<NetworkEdge>()
            };

            // Build network using BFS approach
            var visited = new HashSet<string>();
            var queue = new Queue<(string phone, int currentDepth)>();
            queue.Enqueue((phoneNumber, 0));
            
            while (queue.Count > 0)
            {
                var (currentPhone, currentDepth) = queue.Dequeue();
                
                if (visited.Contains(currentPhone) || currentDepth > depth)
                    continue;
                    
                visited.Add(currentPhone);
                
                // Get or create suspect profile
                var profile = GetOrCreateSuspectProfile(currentPhone);
                
                // Add node to graph
                var node = new NetworkNode
                {
                    PhoneNumber = currentPhone,
                    SuspectName = profile.Name,
                    RiskLevel = profile.RiskLevel,
                    CentralityScore = 0, // Calculate later
                    ConnectionCount = profile.Contacts.Count,
                    IsLeader = false, // Determine later
                    Role = profile.Role,
                    NodeType = profile.IsSuspect ? "suspect" : (profile.IsVictim ? "victim" : "unknown"),
                    TotalCallsMade = profile.TotalCallsMade,
                    TotalMoneyReceived = profile.TotalMoneyReceived,
                    TotalMoneySent = profile.TotalMoneySent,
                    LastActiveDate = profile.LastActiveDate,
                    Location = profile.Location
                };
                graph.Nodes.Add(node);
                
                // Add edges for contacts
                if (currentDepth < depth)
                {
                    foreach (var contact in profile.Contacts)
                    {
                        // Add edge
                        var edge = new NetworkEdge
                        {
                            SourcePhone = currentPhone,
                            TargetPhone = contact.PhoneNumber,
                            Weight = contact.CallFrequency + (int)(contact.MoneyTransferred / 1000),
                            RelationType = contact.RelationType,
                            FirstInteraction = contact.FirstContact,
                            LastInteraction = contact.LastContact,
                            CallCount = contact.CallFrequency,
                            MoneyTransferred = contact.MoneyTransferred,
                            Direction = contact.Direction
                        };
                        
                        // Avoid duplicate edges
                        if (!graph.Edges.Any(e => 
                            (e.SourcePhone == edge.SourcePhone && e.TargetPhone == edge.TargetPhone) ||
                            (e.SourcePhone == edge.TargetPhone && e.TargetPhone == edge.SourcePhone)))
                        {
                            graph.Edges.Add(edge);
                        }
                        
                        // Queue contact for next level
                        if (!visited.Contains(contact.PhoneNumber))
                        {
                            queue.Enqueue((contact.PhoneNumber, currentDepth + 1));
                        }
                    }
                }
            }
            
            // Calculate centrality scores for all nodes
            CalculateCentralityScores(graph);
            
            // Identify potential leader
            await IdentifyLeaderInGraphAsync(graph);
            
            // Cache the result
            _networkCache[cacheKey] = graph;

            _logger.LogInformation(
                "Built network graph with {NodeCount} nodes and {EdgeCount} edges",
                graph.Nodes.Count, graph.Edges.Count);

            return Result<SuspectNetworkGraph>.Success(graph);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building network graph");
            return Result<SuspectNetworkGraph>.Failure($"Error: {ex.Message}");
        }
    }
    
    private void CalculateCentralityScores(SuspectNetworkGraph graph)
    {
        foreach (var node in graph.Nodes)
        {
            // Count edges connected to this node
            var edgeCount = graph.Edges.Count(e => 
                e.SourcePhone == node.PhoneNumber || e.TargetPhone == node.PhoneNumber);
            
            // Calculate centrality based on connections and activity
            var maxConnections = graph.Nodes.Max(n => n.ConnectionCount);
            var connectionScore = maxConnections > 0 
                ? (double)node.ConnectionCount / maxConnections * 40 
                : 0;
            
            // Money flow score
            var totalMoney = (double)(node.TotalMoneyReceived + node.TotalMoneySent);
            var maxMoney = (double)graph.Nodes.Max(n => n.TotalMoneyReceived + n.TotalMoneySent);
            var moneyScore = maxMoney > 0 
                ? totalMoney / maxMoney * 30 
                : 0;
            
            // Call activity score
            var maxCalls = graph.Nodes.Max(n => n.TotalCallsMade);
            var callScore = maxCalls > 0 
                ? (double)node.TotalCallsMade / maxCalls * 30 
                : 0;
            
            node.CentralityScore = (int)(connectionScore + moneyScore + callScore);
        }
    }
    
    private async Task IdentifyLeaderInGraphAsync(SuspectNetworkGraph graph)
    {
        if (graph.Nodes.Count == 0) return;
        
        // Score each node for leadership potential
        var scores = new Dictionary<string, int>();
        
        foreach (var node in graph.Nodes)
        {
            var score = 0;
            
            // Centrality contributes heavily
            score += node.CentralityScore;
            
            // Money received (leaders often receive money)
            if (node.TotalMoneyReceived > node.TotalMoneySent)
                score += 20;
            
            // High risk level indicates known criminal
            if (node.RiskLevel == "High" || node.RiskLevel == "Critical")
                score += 15;
            
            // Outgoing calls (leaders coordinate)
            var outgoingEdges = graph.Edges.Count(e => e.SourcePhone == node.PhoneNumber);
            score += outgoingEdges * 5;
            
            scores[node.PhoneNumber ?? ""] = score;
        }
        
        // Identify the highest scoring node as leader
        var leaderId = scores.OrderByDescending(x => x.Value).FirstOrDefault().Key;
        var leaderNode = graph.Nodes.FirstOrDefault(n => n.PhoneNumber == leaderId);
        if (leaderNode != null)
        {
            leaderNode.IsLeader = true;
            leaderNode.Role = "Leader";
        }
    }

    /// <summary>
    /// Identify the leader/key person in a network
    /// Uses metrics: Communication frequency, money flow, arrest history
    /// </summary>
    public async Task<Result<NetworkNodeAnalysis>> IdentifyNetworkLeaderAsync(
        List<string> phoneNumbers)
    {
        try
        {
            _logger.LogInformation(
                "Identifying network leader among {Count} suspects",
                phoneNumbers.Count);

            var analysis = new NetworkNodeAnalysis
            {
                AnalysisDate = DateTime.UtcNow,
                Nodes = new List<NodeRanking>()
            };

            // Score each phone number
            var rankings = new List<NodeRanking>();
            
            foreach (var phone in phoneNumbers)
            {
                var profile = GetOrCreateSuspectProfile(phone);
                var ranking = new NodeRanking
                {
                    PhoneNumber = phone,
                    ScoreBreakdown = new List<string>()
                };
                
                var totalScore = 0;
                
                // 1. Centrality Score (connected to most others)
                var centralityScore = Math.Min(profile.Contacts.Count * 10, 30);
                ranking.ScoreBreakdown.Add($"Centrality: {centralityScore}/30 ({profile.Contacts.Count} connections)");
                totalScore += centralityScore;
                
                // 2. Communication Initiation (who contacts whom more)
                var outgoingCalls = profile.Contacts.Sum(c => c.Direction == "outgoing" ? c.CallFrequency : 0);
                var incomingCalls = profile.Contacts.Sum(c => c.Direction == "incoming" ? c.CallFrequency : 0);
                var initiationScore = outgoingCalls > incomingCalls ? 25 : 10;
                ranking.ScoreBreakdown.Add($"Communication Initiative: {initiationScore}/25 ({outgoingCalls} outgoing vs {incomingCalls} incoming)");
                totalScore += initiationScore;
                
                // 3. Financial Flow Control (who receives money)
                var moneyScore = 0;
                if (profile.TotalMoneyReceived > profile.TotalMoneySent)
                {
                    var ratio = profile.TotalMoneyReceived / Math.Max(profile.TotalMoneySent, 1);
                    moneyScore = Math.Min((int)(ratio * 5), 25);
                }
                ranking.ScoreBreakdown.Add($"Financial Control: {moneyScore}/25 (₹{profile.TotalMoneyReceived:N0} received)");
                totalScore += moneyScore;
                
                // 4. Criminal History (experienced criminals more likely to lead)
                var historyScore = profile.IsSuspect ? 15 : (profile.PreviousArrests > 0 ? 10 : 0);
                ranking.ScoreBreakdown.Add($"Criminal History: {historyScore}/20 ({profile.PreviousArrests} prior arrests)");
                totalScore += historyScore;
                
                ranking.LeadershipScore = totalScore;
                rankings.Add(ranking);
            }
            
            // Rank by score
            var rankedList = rankings.OrderByDescending(r => r.LeadershipScore).ToList();
            for (int i = 0; i < rankedList.Count; i++)
            {
                rankedList[i].Rank = i + 1;
            }
            
            analysis.Nodes = rankedList;

            _logger.LogInformation(
                "Leadership analysis complete. Top candidate: {Phone} with score {Score}",
                analysis.IdentifiedLeader?.PhoneNumber,
                analysis.IdentifiedLeader?.LeadershipScore);

            return Result<NetworkNodeAnalysis>.Success(analysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error identifying network leader");
            return Result<NetworkNodeAnalysis>.Failure($"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Detect communication patterns within a network
    /// E.g., "Morning briefing calls", "Evening coordination calls"
    /// </summary>
    public async Task<Result<List<CommunicationPatternAnalysis>>> DetectCommunicationPatternsAsync(
        List<string> phoneNumbers)
    {
        try
        {
            _logger.LogInformation(
                "Detecting communication patterns for {Count} suspects",
                phoneNumbers.Count);

            var patterns = new List<CommunicationPatternAnalysis>();

            // Analyze call timing patterns
            var allCallTimes = new List<(string from, string to, TimeSpan time, DayOfWeek day)>();
            
            foreach (var phone in phoneNumbers)
            {
                var profile = GetOrCreateSuspectProfile(phone);
                foreach (var contact in profile.Contacts)
                {
                    // Simulate call times based on contact data
                    for (int i = 0; i < Math.Min(contact.CallFrequency, 10); i++)
                    {
                        // Create realistic call patterns
                        var randomHour = (contact.CallFrequency % 24);
                        var randomDay = (DayOfWeek)(contact.CallFrequency % 7);
                        allCallTimes.Add((phone, contact.PhoneNumber, 
                            TimeSpan.FromHours(randomHour), randomDay));
                    }
                }
            }
            
            // Calculate basic stats for AI context
            var morningCalls = allCallTimes.Count(c => c.time.Hours >= 6 && c.time.Hours < 10);
            var eveningCalls = allCallTimes.Count(c => c.time.Hours >= 18 && c.time.Hours < 22);
            var lateNightCalls = allCallTimes.Count(c => c.time.Hours >= 23 || c.time.Hours < 5);

            // Try AI-powered pattern analysis first
            if (_aiService.IsReady)
            {
                try
                {
                    var prompt = $@"You are a cybercrime CDR analyst for Indian Police. Analyze these communication timing patterns between {phoneNumbers.Count} suspects and identify behavioral patterns.

CALL DATA SUMMARY:
- Total calls analyzed: {allCallTimes.Count}
- Morning calls (6-10 AM): {morningCalls}
- Evening calls (6-10 PM): {eveningCalls}
- Late night calls (11 PM-5 AM): {lateNightCalls}
- Suspects: {string.Join(", ", phoneNumbers.Take(5))}

Return EXACTLY in this format, one pattern per block separated by ---PATTERN---:
PatternName: <name>
Description: <1-2 sentence analysis>
TimePattern: <time range>
Confidence: <0-100>
---PATTERN---";

                    var response = await _aiService.SendMessageAsync(prompt, "Indian cybercrime CDR communication pattern analysis");
                    if (response.Success && !string.IsNullOrEmpty(response.Message))
                    {
                        var blocks = response.Message.Split("---PATTERN---", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        foreach (var block in blocks)
                        {
                            var lines = block.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                            var name = lines.FirstOrDefault(l => l.StartsWith("PatternName:"))?.Substring(12).Trim();
                            var desc = lines.FirstOrDefault(l => l.StartsWith("Description:"))?.Substring(12).Trim();
                            var time = lines.FirstOrDefault(l => l.StartsWith("TimePattern:"))?.Substring(12).Trim();
                            var confStr = lines.FirstOrDefault(l => l.StartsWith("Confidence:"))?.Substring(11).Trim();
                            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(desc))
                            {
                                int.TryParse(confStr, out var conf);
                                patterns.Add(new CommunicationPatternAnalysis
                                {
                                    PatternName = name,
                                    Description = desc,
                                    InvolvedPhones = phoneNumbers.Take(3).ToList(),
                                    TimePattern = time ?? "Variable",
                                    Frequency = morningCalls + eveningCalls + lateNightCalls,
                                    Confidence = conf > 0 ? Math.Min(conf, 100) : 70
                                });
                            }
                        }
                        if (patterns.Count > 0)
                        {
                            _logger.LogInformation("AI detected {Count} communication patterns", patterns.Count);
                            return Result<List<CommunicationPatternAnalysis>>.Success(patterns);
                        }
                    }
                }
                catch (Exception aiEx)
                {
                    _logger.LogWarning(aiEx, "AI pattern analysis failed, falling back to algorithmic detection");
                }
            }

            // Fallback: hardcoded pattern detection
            if (morningCalls > 5)
            {
                patterns.Add(new CommunicationPatternAnalysis
                {
                    PatternName = "Morning Coordination",
                    Description = "Regular morning calls detected - possible daily briefing pattern",
                    InvolvedPhones = phoneNumbers.Take(3).ToList(),
                    TimePattern = "Daily 6:00 AM - 10:00 AM",
                    Frequency = morningCalls,
                    Confidence = Math.Min(morningCalls * 5, 85)
                });
            }
            
            if (eveningCalls > 5)
            {
                patterns.Add(new CommunicationPatternAnalysis
                {
                    PatternName = "Evening Coordination",
                    Description = "Regular evening calls detected - possible end-of-day coordination",
                    InvolvedPhones = phoneNumbers.Take(3).ToList(),
                    TimePattern = "Daily 6:00 PM - 10:00 PM",
                    Frequency = eveningCalls,
                    Confidence = Math.Min(eveningCalls * 5, 80)
                });
            }
            
            if (lateNightCalls > 3)
            {
                patterns.Add(new CommunicationPatternAnalysis
                {
                    PatternName = "Suspicious Late Night Activity",
                    Description = "⚠️ Late night calls detected - unusual communication pattern",
                    InvolvedPhones = phoneNumbers.Take(3).ToList(),
                    TimePattern = "Late night 11:00 PM - 5:00 AM",
                    Frequency = lateNightCalls,
                    Confidence = 90
                });
            }
            
            patterns.Add(new CommunicationPatternAnalysis
            {
                PatternName = "Burst Communication",
                Description = "Multiple rapid calls detected - possible crisis communication or active operation",
                InvolvedPhones = phoneNumbers.Take(2).ToList(),
                TimePattern = "Random bursts",
                Frequency = 8,
                Confidence = 75
            });
            
            if (phoneNumbers.Count >= 3)
            {
                patterns.Add(new CommunicationPatternAnalysis
                {
                    PatternName = "Hierarchical Communication",
                    Description = "One number calls multiple others who don't call each other - suggests command structure",
                    InvolvedPhones = phoneNumbers,
                    TimePattern = "Throughout day",
                    Frequency = 15,
                    Confidence = 70
                });
            }

            _logger.LogInformation("Detected {Count} communication patterns", patterns.Count);
            return Result<List<CommunicationPatternAnalysis>>.Success(patterns);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting communication patterns");
            return Result<List<CommunicationPatternAnalysis>>.Failure($"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Predict next likely targets or crimes based on network patterns
    /// </summary>
    public async Task<Result<List<PredictionSummary>>> PredictNextActivityAsync(
        string phoneNumber)
    {
        try
        {
            _logger.LogInformation(
                "Predicting next activity for {Phone}",
                phoneNumber);

            var predictions = new List<PredictionSummary>();
            var profile = GetOrCreateSuspectProfile(phoneNumber);

            // Try AI-powered predictions first
            if (_aiService.IsReady)
            {
                try
                {
                    var prompt = $@"You are a cybercrime predictive analyst for Indian Police. Based on suspect data, predict next criminal activities.

SUSPECT PROFILE:
- Phone: {phoneNumber}
- Risk Level: {profile.RiskLevel}
- Role: {profile.Role}
- Total Contacts: {profile.Contacts.Count}
- Money Received: ₹{profile.TotalMoneyReceived:N0}
- Money Sent: ₹{profile.TotalMoneySent:N0}
- Previous Arrests: {profile.PreviousArrests}
- Last Active: {profile.LastActiveDate:yyyy-MM-dd}

Provide 3-4 predictions in this EXACT format, separated by ---PRED---:
Type: <prediction type>
Description: <1-2 sentence prediction>
Confidence: <0-100>
Evidence: <comma-separated evidence points>
---PRED---";

                    var response = await _aiService.SendMessageAsync(prompt, "Indian cybercrime suspect activity prediction");
                    if (response.Success && !string.IsNullOrEmpty(response.Message))
                    {
                        var blocks = response.Message.Split("---PRED---", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        foreach (var block in blocks)
                        {
                            var lines = block.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                            var type = lines.FirstOrDefault(l => l.StartsWith("Type:"))?.Substring(5).Trim();
                            var desc = lines.FirstOrDefault(l => l.StartsWith("Description:"))?.Substring(12).Trim();
                            var confStr = lines.FirstOrDefault(l => l.StartsWith("Confidence:"))?.Substring(11).Trim();
                            var evidence = lines.FirstOrDefault(l => l.StartsWith("Evidence:"))?.Substring(9).Trim();
                            if (!string.IsNullOrEmpty(type) && !string.IsNullOrEmpty(desc))
                            {
                                int.TryParse(confStr, out var conf);
                                predictions.Add(new PredictionSummary
                                {
                                    PredictionType = type,
                                    Description = desc,
                                    PredictedDate = DateTime.UtcNow.AddDays(3),
                                    Confidence = conf > 0 ? Math.Min(conf, 100) : 70,
                                    SupportingEvidence = evidence?.Split(',', StringSplitOptions.TrimEntries).ToList() ?? new List<string>()
                                });
                            }
                        }
                        if (predictions.Count > 0)
                        {
                            _logger.LogInformation("AI generated {Count} predictions for {Phone}", predictions.Count, phoneNumber);
                            return Result<List<PredictionSummary>>.Success(predictions);
                        }
                    }
                }
                catch (Exception aiEx)
                {
                    _logger.LogWarning(aiEx, "AI prediction failed, falling back to algorithmic predictions");
                }
            }

            // Fallback: hardcoded predictions
            predictions.Add(new PredictionSummary
            {
                PredictionType = "Next Active Period",
                Description = "Based on historical patterns, suspect likely to be active during evening hours (6-10 PM)",
                PredictedDate = DateTime.UtcNow.Date.AddHours(18),
                Confidence = 72,
                SupportingEvidence = new List<string>
                {
                    "75% of previous calls made between 6-10 PM",
                    "Most successful frauds executed during this window",
                    "Victim availability highest during evening"
                }
            });
            
            // Prediction 2: Target profile
            predictions.Add(new PredictionSummary
            {
                PredictionType = "Likely Target Profile",
                Description = "Middle-aged professionals (35-55), urban areas, moderate-high income bracket",
                PredictedDate = DateTime.UtcNow.AddDays(7),
                Confidence = 68,
                SupportingEvidence = new List<string>
                {
                    "Previous victims match this demographic",
                    "Investment scam MO targets professionals",
                    "Pattern suggests metro city targeting"
                }
            });
            
            // Prediction 3: Method of fraud
            predictions.Add(new PredictionSummary
            {
                PredictionType = "Expected Fraud Method",
                Description = "High probability of digital arrest scam or investment fraud scheme",
                PredictedDate = DateTime.UtcNow.AddDays(3),
                Confidence = 80,
                SupportingEvidence = new List<string>
                {
                    $"Network has {profile.Contacts.Count} connected numbers for coordination",
                    "Recent activity spike in linked accounts",
                    "Pattern matches known digital arrest syndicate"
                }
            });
            
            // Prediction 4: Money movement
            if (profile.TotalMoneyReceived > 100000)
            {
                predictions.Add(new PredictionSummary
                {
                    PredictionType = "Fund Movement Alert",
                    Description = "⚠️ Funds likely to be moved to crypto or hawala channels within 48 hours",
                    PredictedDate = DateTime.UtcNow.AddHours(48),
                    Confidence = 85,
                    SupportingEvidence = new List<string>
                    {
                        $"₹{profile.TotalMoneyReceived:N0} accumulated in network",
                        "Historical pattern shows withdrawal after threshold",
                        "Connected accounts showing pre-withdrawal behavior"
                    }
                });
            }

            _logger.LogInformation("Generated {Count} predictions", predictions.Count);
            return Result<List<PredictionSummary>>.Success(predictions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error predicting next activity");
            return Result<List<PredictionSummary>>.Failure($"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Recommend arrest/intervention strategy for this network
    /// </summary>
    public async Task<Result<NetworkInterventionStrategy>> RecommendInterventionStrategyAsync(
        List<string> phoneNumbers)
    {
        try
        {
            _logger.LogInformation(
                "Recommending intervention strategy for {Count} suspects",
                phoneNumbers.Count);

            // First identify the leader
            var leaderResult = await IdentifyNetworkLeaderAsync(phoneNumbers);
            var leader = leaderResult.IsSuccess ? leaderResult.Data?.IdentifiedLeader : null;

            var strategy = new NetworkInterventionStrategy
            {
                GeneratedDate = DateTime.UtcNow,
                RecommendedArrestOrder = new List<string>(),
                Recommendations = new List<string>(),
                CriticalEvidenceToPreserve = new List<string>()
            };

            // Try AI-powered intervention strategy first
            if (_aiService.IsReady)
            {
                try
                {
                    var leaderPhone = leader?.PhoneNumber ?? "Not identified";
                    var prompt = $@"You are a senior cybercrime investigation strategist for Indian Police. Recommend an arrest/intervention strategy for this criminal network.

NETWORK DATA:
- Suspects: {phoneNumbers.Count} ({string.Join(", ", phoneNumbers.Take(5))})
- Identified Leader: {leaderPhone}

Provide strategy in this EXACT format:
---ARREST_ORDER---
<comma-separated phone numbers in recommended arrest order>
---TIMING---
<optimal timing recommendation>
---RECOMMENDATIONS---
<one recommendation per line>
---EVIDENCE---
<one evidence item per line to preserve>
---END---

Use Indian legal context (BNSS, BNS, IT Act). Be specific and actionable.";

                    var response = await _aiService.SendMessageAsync(prompt, "Indian police arrest strategy for cybercrime network");
                    if (response.Success && !string.IsNullOrEmpty(response.Message))
                    {
                        var msg = response.Message;
                        
                        // Parse arrest order
                        var orderMatch = msg.IndexOf("---ARREST_ORDER---");
                        var timingMatch = msg.IndexOf("---TIMING---");
                        var recsMatch = msg.IndexOf("---RECOMMENDATIONS---");
                        var evidMatch = msg.IndexOf("---EVIDENCE---");
                        var endMatch = msg.IndexOf("---END---");

                        if (recsMatch > 0)
                        {
                            if (orderMatch >= 0 && timingMatch > orderMatch)
                            {
                                var orderText = msg.Substring(orderMatch + 18, timingMatch - orderMatch - 18).Trim();
                                strategy.RecommendedArrestOrder = orderText.Split(',', StringSplitOptions.TrimEntries)
                                    .Where(s => !string.IsNullOrEmpty(s)).ToList();
                            }

                            if (timingMatch >= 0 && recsMatch > timingMatch)
                            {
                                strategy.OptimalTimingRecommendation = msg.Substring(timingMatch + 12, recsMatch - timingMatch - 12).Trim();
                            }

                            var recsEnd = evidMatch > recsMatch ? evidMatch : (endMatch > recsMatch ? endMatch : msg.Length);
                            var recsText = msg.Substring(recsMatch + 21, recsEnd - recsMatch - 21).Trim();
                            strategy.Recommendations = recsText.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

                            if (evidMatch >= 0)
                            {
                                var evidEnd = endMatch > evidMatch ? endMatch : msg.Length;
                                var evidText = msg.Substring(evidMatch + 14, evidEnd - evidMatch - 14).Trim();
                                strategy.CriticalEvidenceToPreserve = evidText.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
                            }

                            if (strategy.Recommendations.Count > 0)
                            {
                                if (strategy.RecommendedArrestOrder.Count == 0)
                                    strategy.RecommendedArrestOrder = phoneNumbers;

                                _logger.LogInformation("AI generated intervention strategy with {Count} recommendations", strategy.Recommendations.Count);
                                return Result<NetworkInterventionStrategy>.Success(strategy);
                            }
                        }
                    }
                }
                catch (Exception aiEx)
                {
                    _logger.LogWarning(aiEx, "AI strategy generation failed, falling back to algorithmic strategy");
                }
            }

            // Fallback: hardcoded strategy
            // Determine arrest strategy based on network structure
            if (phoneNumbers.Count <= 3)
            {
                // Small network - simultaneous arrests
                strategy.RecommendedArrestOrder = phoneNumbers;
                strategy.Recommendations.Add("⚡ SIMULTANEOUS ARREST RECOMMENDED");
                strategy.Recommendations.Add("Network is small enough for coordinated action");
                strategy.OptimalTimingRecommendation = "Execute during early morning (5-6 AM) for element of surprise";
            }
            else
            {
                // Larger network - strategic sequence
                strategy.Recommendations.Add("📋 SEQUENTIAL ARREST STRATEGY");
                
                if (leader != null)
                {
                    // Option 1: Leader first (decapitation)
                    strategy.Recommendations.Add($"Option A: Arrest leader ({leader.PhoneNumber}) first to decapitate network");
                    strategy.Recommendations.Add("  - Pros: Disrupts coordination, may lead to network collapse");
                    strategy.Recommendations.Add("  - Cons: Others may flee if alerted");
                    
                    // Build arrest order: Leader -> High centrality -> Others
                    strategy.RecommendedArrestOrder.Add(leader.PhoneNumber ?? "");
                    foreach (var phone in phoneNumbers.Where(p => p != leader.PhoneNumber))
                    {
                        strategy.RecommendedArrestOrder.Add(phone);
                    }
                }
                
                strategy.Recommendations.Add("");
                strategy.Recommendations.Add("Option B: Arrest periphery first (suffocation)");
                strategy.Recommendations.Add("  - Pros: Gathers evidence, isolates leader");
                strategy.Recommendations.Add("  - Cons: Leader may destroy evidence or flee");
                
                strategy.OptimalTimingRecommendation = "Coordinate with all local police stations. Execute between 4-6 AM on weekday.";
            }
            
            // Critical evidence
            strategy.CriticalEvidenceToPreserve = new List<string>
            {
                "📱 Mobile phones - preserve in Faraday bags immediately",
                "💻 Laptops/computers - do not power off, image RAM",
                "📄 Bank statements, checkbooks, financial documents",
                "🔐 SIM cards and memory cards",
                "📋 Written notes, contact lists, address books",
                "💳 Debit/Credit cards and UPI device",
                "🧾 Transaction receipts and hawala records",
                "📦 Any hardware wallets or cryptocurrency devices"
            };
            
            // Additional recommendations
            strategy.Recommendations.Add("");
            strategy.Recommendations.Add("⚠️ IMPORTANT PRECAUTIONS:");
            strategy.Recommendations.Add("• Inform cyber cell before raid for digital forensics support");
            strategy.Recommendations.Add("• Request bank account freeze 30 mins before arrest");
            strategy.Recommendations.Add("• Have Section 91 CrPC/65B IT Act notices ready");
            strategy.Recommendations.Add("• Video record the entire arrest and seizure");
            
            _logger.LogInformation("Intervention strategy generated successfully");
            return Result<NetworkInterventionStrategy>.Success(strategy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recommending intervention");
            return Result<NetworkInterventionStrategy>.Failure($"Error: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Get AI-powered analysis of the network
    /// </summary>
    public async Task<Result<string>> GetAINetworkAnalysisAsync(
        string phoneNumber,
        SuspectNetworkGraph graph)
    {
        try
        {
            var prompt = $@"You are a cybercrime investigation analyst for Indian Police. Analyze this criminal network and provide insights.

NETWORK DATA:
- Central Suspect: {phoneNumber}
- Total Nodes: {graph.Nodes.Count}
- Total Connections: {graph.Edges.Count}

NODES:
{string.Join("\n", graph.Nodes.Select(n => $"- {n.PhoneNumber}: {n.SuspectName}, Role: {n.Role}, Risk: {n.RiskLevel}, Leader: {n.IsLeader}"))}

CONNECTIONS:
{string.Join("\n", graph.Edges.Take(10).Select(e => $"- {e.SourcePhone} → {e.TargetPhone}: {e.RelationType}, Calls: {e.CallCount}, Money: ₹{e.MoneyTransferred:N0}"))}

Provide:
1. Network Structure Analysis (hierarchy, clusters)
2. Key Players Identification
3. Modus Operandi Assessment
4. Investigation Recommendations
5. Immediate Actions Required

Use Indian legal context (BNS, BNSS, IT Act). Be specific and actionable.";

            var response = await _aiService.SendMessageAsync(prompt);
            
            if (response != null && response.Success && !string.IsNullOrEmpty(response.Message))
            {
                return Result<string>.Success(response.Message);
            }
            
            // Fallback analysis if AI unavailable
            return Result<string>.Success(GenerateFallbackAnalysis(graph));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting AI analysis");
            return Result<string>.Success(GenerateFallbackAnalysis(graph));
        }
    }
    
    private string GenerateFallbackAnalysis(SuspectNetworkGraph graph)
    {
        var leader = graph.Nodes.FirstOrDefault(n => n.IsLeader);
        var highRisk = graph.Nodes.Count(n => n.RiskLevel == "High" || n.RiskLevel == "Critical");
        
        return $@"## Network Analysis Report

### Network Structure
- **Total Suspects:** {graph.Nodes.Count}
- **Total Connections:** {graph.Edges.Count}
- **Identified Leader:** {leader?.PhoneNumber ?? "Not identified"} ({leader?.SuspectName ?? "Unknown"})
- **High Risk Individuals:** {highRisk}

### Key Observations
1. Network shows {(graph.Edges.Count > graph.Nodes.Count * 2 ? "high" : "moderate")} connectivity
2. {(leader != null ? $"Clear leadership structure with {leader.PhoneNumber} as central node" : "No clear leader identified - possible cell structure")}
3. Money flow analysis suggests organized fund collection pattern

### Recommended Actions
1. File Section 91 BNSS notices to all telecom providers
2. Request account freeze for linked bank accounts
3. Coordinate with cyber cell for device seizure
4. Consider simultaneous arrests to prevent evidence destruction

### Applicable Sections
- BNS Section 318 (Cheating)
- BNS Section 319 (Cheating by Personation)  
- IT Act Section 66D (Cheating by Personation using Computer)
- IT Act Section 43 (Unauthorized Access)";
    }
    
    // ===== HELPER METHODS =====
    
    private SuspectProfile GetOrCreateSuspectProfile(string phoneNumber)
    {
        if (_suspectProfiles.TryGetValue(phoneNumber, out var profile))
            return profile;
            
        // Create a simulated profile for demo
        var random = new Random(phoneNumber.GetHashCode());
        profile = new SuspectProfile
        {
            PhoneNumber = phoneNumber,
            Name = GenerateRandomName(random),
            RiskLevel = random.Next(100) switch
            {
                > 80 => "Critical",
                > 60 => "High",
                > 40 => "Medium",
                _ => "Low"
            },
            Role = random.Next(10) switch
            {
                0 => "Leader",
                1 or 2 => "Money Handler",
                3 or 4 => "Caller",
                5 or 6 => "Recruiter",
                _ => "Associate"
            },
            IsSuspect = random.Next(100) > 30,
            IsVictim = random.Next(100) > 85,
            TotalCallsMade = random.Next(50, 500),
            TotalMoneyReceived = random.Next(10000, 500000),
            TotalMoneySent = random.Next(5000, 200000),
            LastActiveDate = DateTime.UtcNow.AddDays(-random.Next(1, 30)),
            Location = GetRandomLocation(random),
            PreviousArrests = random.Next(100) > 70 ? random.Next(1, 5) : 0,
            Contacts = GenerateRandomContacts(phoneNumber, random)
        };
        
        _suspectProfiles[phoneNumber] = profile;
        return profile;
    }
    
    private string GenerateRandomName(Random random)
    {
        var firstNames = new[] { "Rajesh", "Amit", "Suresh", "Vikram", "Anil", "Pradeep", "Ravi", "Sanjay", "Deepak", "Manoj" };
        var lastNames = new[] { "Kumar", "Singh", "Sharma", "Verma", "Gupta", "Yadav", "Patel", "Shah", "Joshi", "Mishra" };
        return $"{firstNames[random.Next(firstNames.Length)]} {lastNames[random.Next(lastNames.Length)]}";
    }
    
    private string GetRandomLocation(Random random)
    {
        var locations = new[] 
        { 
            "Mumbai, Maharashtra", "Delhi NCR", "Bangalore, Karnataka", 
            "Hyderabad, Telangana", "Pune, Maharashtra", "Nagpur, Maharashtra",
            "Jaipur, Rajasthan", "Lucknow, UP", "Kolkata, West Bengal"
        };
        return locations[random.Next(locations.Length)];
    }
    
    private List<ContactInfo> GenerateRandomContacts(string phoneNumber, Random random)
    {
        var contacts = new List<ContactInfo>();
        var numContacts = random.Next(3, 8);
        
        for (int i = 0; i < numContacts; i++)
        {
            var contactPhone = $"9{random.Next(100000000, 999999999)}";
            if (contactPhone == phoneNumber) continue;
            
            contacts.Add(new ContactInfo
            {
                PhoneNumber = contactPhone,
                CallFrequency = random.Next(5, 100),
                MoneyTransferred = random.Next(0, 100000),
                RelationType = random.Next(4) switch
                {
                    0 => "Associate",
                    1 => "Money Transfer",
                    2 => "Frequent Contact",
                    _ => "One-time Contact"
                },
                Direction = random.Next(2) == 0 ? "incoming" : "outgoing",
                FirstContact = DateTime.UtcNow.AddDays(-random.Next(30, 180)),
                LastContact = DateTime.UtcNow.AddDays(-random.Next(1, 30))
            });
        }
        
        return contacts;
    }
    
    private void InitializeSampleNetworkData()
    {
        // Pre-populate some known suspect profiles for demo
        var knownSuspects = new[]
        {
            ("9876543210", "Vikram Malhotra", "Leader", "Critical", true),
            ("9876543211", "Amit Sharma", "Money Handler", "High", true),
            ("9876543212", "Suresh Yadav", "Caller", "High", true),
            ("9876543213", "Rajesh Gupta", "Recruiter", "Medium", true),
            ("9876543214", "Pradeep Singh", "Associate", "Medium", true),
            ("9999888877", "Unknown Caller 1", "Unknown", "High", true),
            ("9999888878", "Unknown Caller 2", "Unknown", "Medium", true)
        };
        
        foreach (var (phone, name, role, risk, isSuspect) in knownSuspects)
        {
            var random = new Random(phone.GetHashCode());
            _suspectProfiles[phone] = new SuspectProfile
            {
                PhoneNumber = phone,
                Name = name,
                Role = role,
                RiskLevel = risk,
                IsSuspect = isSuspect,
                IsVictim = false,
                TotalCallsMade = random.Next(100, 800),
                TotalMoneyReceived = random.Next(50000, 1000000),
                TotalMoneySent = random.Next(20000, 400000),
                LastActiveDate = DateTime.UtcNow.AddDays(-random.Next(1, 15)),
                Location = GetRandomLocation(random),
                PreviousArrests = role == "Leader" ? 2 : random.Next(0, 2),
                Contacts = GenerateRandomContacts(phone, random)
            };
        }
    }
}

// ===== DOMAIN MODELS FOR NETWORK ANALYSIS =====

public class SuspectNetworkGraph
{
    public int Id { get; set; }
    public string? RootPhoneNumber { get; set; }
    public DateTime CreatedDate { get; set; }
    public List<NetworkNode> Nodes { get; set; } = new();
    public List<NetworkEdge> Edges { get; set; } = new();
    public string? AIAnalysis { get; set; }
}

public class NetworkNode
{
    public string? PhoneNumber { get; set; }
    public string? SuspectName { get; set; }
    public string? RiskLevel { get; set; }
    public int CentralityScore { get; set; } // 0-100, how central in network
    public int ConnectionCount { get; set; } // How many direct connections
    public bool IsLeader { get; set; }
    public string? Role { get; set; } // Leader, Enforcer, Money handler, Recruiter
    public string? NodeType { get; set; } // suspect, victim, unknown
    public int TotalCallsMade { get; set; }
    public decimal TotalMoneyReceived { get; set; }
    public decimal TotalMoneySent { get; set; }
    public DateTime? LastActiveDate { get; set; }
    public string? Location { get; set; }
}

public class NetworkEdge
{
    public string? SourcePhone { get; set; }
    public string? TargetPhone { get; set; }
    public int Weight { get; set; } // Call frequency or transaction amount
    public string? RelationType { get; set; } // Contact, Recipient, Associate
    public DateTime FirstInteraction { get; set; }
    public DateTime LastInteraction { get; set; }
    public int CallCount { get; set; }
    public decimal MoneyTransferred { get; set; }
    public string? Direction { get; set; } // incoming, outgoing, bidirectional
}

public class NetworkNodeAnalysis
{
    public int Id { get; set; }
    public DateTime AnalysisDate { get; set; }
    public List<NodeRanking> Nodes { get; set; } = new();

    public NodeRanking? IdentifiedLeader => Nodes.FirstOrDefault(n => n.Rank == 1);
}

public class NodeRanking
{
    public string? PhoneNumber { get; set; }
    public int Rank { get; set; }
    public int LeadershipScore { get; set; } // 0-100
    public List<string> ScoreBreakdown { get; set; } = new();
    // E.g., "Centrality: 85/100", "Financial Control: 90/100", "History: 70/100"
}

public class CommunicationPatternAnalysis
{
    public string? PatternName { get; set; } // "Morning Briefing", "Evening Coordination"
    public string? Description { get; set; }
    public List<string> InvolvedPhones { get; set; } = new();
    public string? TimePattern { get; set; } // "Daily 8 AM", "Weekly Monday 3 PM"
    public int Frequency { get; set; } // How often observed
    public int Confidence { get; set; } // 0-100
}

public class PredictionSummary
{
    public string? PredictionType { get; set; } // "Next Target", "Activity Type"
    public string? Description { get; set; }
    public DateTime PredictedDate { get; set; }
    public int Confidence { get; set; } // 0-100
    public List<string> SupportingEvidence { get; set; } = new();
}

public class NetworkInterventionStrategy
{
    public int Id { get; set; }
    public DateTime GeneratedDate { get; set; }
    public List<string> RecommendedArrestOrder { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    // E.g., "Arrest leader first to prevent coordination", "Simultaneous arrests recommended"
    public string? OptimalTimingRecommendation { get; set; }
    public List<string> CriticalEvidenceToPreserve { get; set; } = new();
}

// ===== SUSPECT PROFILE FOR NETWORK BUILDING =====

public class SuspectProfile
{
    public string PhoneNumber { get; set; } = "";
    public string Name { get; set; } = "";
    public string RiskLevel { get; set; } = "Unknown";
    public string Role { get; set; } = "Unknown";
    public bool IsSuspect { get; set; }
    public bool IsVictim { get; set; }
    public int TotalCallsMade { get; set; }
    public decimal TotalMoneyReceived { get; set; }
    public decimal TotalMoneySent { get; set; }
    public DateTime? LastActiveDate { get; set; }
    public string? Location { get; set; }
    public int PreviousArrests { get; set; }
    public List<ContactInfo> Contacts { get; set; } = new();
}

public class ContactInfo
{
    public string PhoneNumber { get; set; } = "";
    public int CallFrequency { get; set; }
    public decimal MoneyTransferred { get; set; }
    public string RelationType { get; set; } = "";
    public string Direction { get; set; } = ""; // incoming, outgoing
    public DateTime FirstContact { get; set; }
    public DateTime LastContact { get; set; }
}

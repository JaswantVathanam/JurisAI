using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AILegalAsst.Models;
using Microsoft.Extensions.Logging;

namespace AILegalAsst.Services
{
    /// <summary>
    /// Agent Orchestration Service - enables inter-agent communication.
    /// Instead of each handler calling exactly one service, the orchestrator
    /// detects ALL relevant sub-agents for a query, gathers data from them
    /// in parallel, and combines the context for the main AI agent to synthesize.
    /// </summary>
    public class AgentOrchestrationService
    {
        private readonly AzureAgentService _agentService;
        private readonly CaseService _caseService;
        private readonly LawService _lawService;
        private readonly LegalDatabaseService _legalDbService;
        private readonly PrecedentService _precedentService;
        private readonly LegalWebSearchService _webSearchService;
        private readonly CybercrimeService _cybercrimeService;
        private readonly ScamPatternService _scamPatternService;
        private readonly FIRDraftService _firDraftService;
        private readonly DeadlineTrackerService _deadlineService;
        private readonly EvidenceCustodyService _evidenceService;
        private readonly LegalNoticeService _legalNoticeService;
        private readonly CDRAnalysisService _cdrService;
        private readonly CaseTimelineService _timelineService;
        private readonly ILogger<AgentOrchestrationService> _logger;

        // Sub-agent domain definitions with keyword triggers
        private static readonly SubAgentDomain[] SubAgentDomains = new[]
        {
            new SubAgentDomain("CaseData", new[] {
                "case", "fir", "complaint", "accused", "complainant", "victim",
                "police station", "io ", "investigation officer", "case number",
                "case id", "case details", "case status", "case history"
            }),
            new SubAgentDomain("LawSearch", new[] {
                "law", "section", "act", "bns", "bnss", "bsa", "ipc", "crpc",
                "it act", "cyber law", "provision", "legal provision", "amendment",
                "statute", "legislation", "article", "clause"
            }),
            new SubAgentDomain("Precedent", new[] {
                "precedent", "judgment", "ruling", "court order", "supreme court",
                "high court", "landmark case", "case law", "judicial", "verdict",
                "citation", "appeal"
            }),
            new SubAgentDomain("Cybercrime", new[] {
                "cybercrime", "cyber crime", "online fraud", "phishing", "hacking",
                "ransomware", "identity theft", "cyber attack", "data breach",
                "cyber security", "digital crime", "upi fraud", "banking fraud"
            }),
            new SubAgentDomain("ScamAnalysis", new[] {
                "scam", "fraud", "con", "scheme", "ponzi", "investment fraud",
                "lottery scam", "romance scam", "tech support scam", "fraud pattern",
                "modus operandi", "cheating"
            }),
            new SubAgentDomain("Deadline", new[] {
                "deadline", "time limit", "limitation", "due date", "statutory period",
                "filing deadline", "chargesheet", "bail", "remand", "custody period",
                "hearing date"
            }),
            new SubAgentDomain("Evidence", new[] {
                "evidence", "proof", "exhibit", "chain of custody", "forensic",
                "digital evidence", "document", "physical evidence", "seized",
                "tamper", "hash", "integrity"
            }),
            new SubAgentDomain("CDR", new[] {
                "cdr", "call detail", "call record", "phone record", "tower location",
                "cell tower", "imei", "imsi", "call log", "sms record",
                "communication record"
            }),
            new SubAgentDomain("Timeline", new[] {
                "timeline", "chronology", "sequence of events", "history",
                "case progress", "investigation stages", "milestones"
            }),
            new SubAgentDomain("FIR", new[] {
                "fir draft", "first information report", "register fir", "file fir",
                "fir format", "fir template", "zero fir"
            }),
            new SubAgentDomain("LegalNotice", new[] {
                "legal notice", "notice", "show cause", "demand notice",
                "cease and desist", "warning notice"
            }),
        };

        public AgentOrchestrationService(
            AzureAgentService agentService,
            CaseService caseService,
            LawService lawService,
            LegalDatabaseService legalDbService,
            PrecedentService precedentService,
            LegalWebSearchService webSearchService,
            CybercrimeService cybercrimeService,
            ScamPatternService scamPatternService,
            FIRDraftService firDraftService,
            DeadlineTrackerService deadlineService,
            EvidenceCustodyService evidenceService,
            LegalNoticeService legalNoticeService,
            CDRAnalysisService cdrService,
            CaseTimelineService timelineService,
            ILogger<AgentOrchestrationService> logger)
        {
            _agentService = agentService;
            _caseService = caseService;
            _lawService = lawService;
            _legalDbService = legalDbService;
            _precedentService = precedentService;
            _webSearchService = webSearchService;
            _cybercrimeService = cybercrimeService;
            _scamPatternService = scamPatternService;
            _firDraftService = firDraftService;
            _deadlineService = deadlineService;
            _evidenceService = evidenceService;
            _legalNoticeService = legalNoticeService;
            _cdrService = cdrService;
            _timelineService = timelineService;
            _logger = logger;
        }

        /// <summary>
        /// Detect which sub-agent domains are relevant for a given query.
        /// Returns all matching domains (not just one).
        /// </summary>
        public List<string> DetectRelevantDomains(string query)
        {
            var lowerQuery = query.ToLowerInvariant();
            var matched = new List<string>();

            foreach (var domain in SubAgentDomains)
            {
                if (domain.Keywords.Any(kw => lowerQuery.Contains(kw)))
                {
                    matched.Add(domain.Name);
                }
            }

            return matched;
        }

        /// <summary>
        /// Orchestrate multiple sub-agents: gather data from all relevant domains
        /// in parallel, combine context, and send to the AI agent for synthesis.
        /// </summary>
        public async Task<OrchestrationResult> OrchestrateAsync(
            string query,
            List<string> domains,
            Action<string>? onStatusUpdate = null,
            CancellationToken cancellationToken = default)
        {
            var result = new OrchestrationResult();
            var contextSections = new System.Collections.Concurrent.ConcurrentDictionary<string, string>();

            _logger.LogInformation("Orchestrating {Count} sub-agents for query: {Query}",
                domains.Count, query.Length > 100 ? query[..100] + "..." : query);

            // Initialize activity tracking for each domain
            foreach (var domain in domains)
            {
                result.Activities.Add(new ChatStateService.AgentActivity
                {
                    AgentName = domain,
                    Status = ChatStateService.AgentStatus.Pending
                });
            }

            // Extract case numbers from query for case-related lookups
            var caseNumbers = ExtractCaseNumbers(query);

            // Launch all sub-agent data gathering in parallel
            var tasks = new List<Task>();

            foreach (var domain in domains)
            {
                tasks.Add(GatherDomainDataWithTrackingAsync(domain, query, caseNumbers, contextSections, result.Activities, onStatusUpdate, cancellationToken));
            }

            await Task.WhenAll(tasks);
            cancellationToken.ThrowIfCancellationRequested();

            // Build combined context
            var combinedContext = new StringBuilder();
            combinedContext.AppendLine("=== MULTI-AGENT INTELLIGENCE REPORT ===\n");
            combinedContext.AppendLine($"Sub-agents consulted: {string.Join(", ", domains)}\n");

            foreach (var domain in domains)
            {
                if (contextSections.TryGetValue(domain, out var sectionData) && !string.IsNullOrWhiteSpace(sectionData))
                {
                    combinedContext.AppendLine($"--- {domain} Agent Report ---");
                    combinedContext.AppendLine(sectionData);
                    combinedContext.AppendLine();
                    result.SuccessfulAgents.Add(domain);
                }
                else
                {
                    result.FailedAgents.Add(domain);
                }
            }

            result.CombinedContext = combinedContext.ToString();
            result.DomainsQueried = domains;

            // Only attempt AI synthesis if we actually gathered meaningful data
            if (result.SuccessfulAgents.Count > 0)
            {
                onStatusUpdate?.Invoke("Synthesizing multi-agent intelligence...");

                var synthesisPrompt = $@"You are a senior AI Legal Investigation Assistant for India.
Multiple specialized sub-agents have been consulted to answer the user's query.

User query: ""{query}""

{result.CombinedContext}

Based on ALL the intelligence gathered from multiple sub-agents above:
1. Synthesize findings from all sources into a coherent, comprehensive response
2. Highlight connections and correlations across different data sources
3. Flag any contradictions or gaps in the intelligence
4. Provide actionable recommendations based on the combined analysis
5. Cite specific sources (laws, case data, precedents) where applicable

Format with proper markdown. Present the multi-source analysis clearly.";

                var aiResult = await _agentService.SendMessageAsync(
                    synthesisPrompt,
                    $"Multi-agent orchestration: {string.Join(", ", domains)}",
                    cancellationToken);

                if (aiResult?.Success == true && !IsAgentRefusal(aiResult.Message))
                {
                    result.SynthesizedResponse = aiResult.Message;
                    result.Success = true;
                }
                else
                {
                    // AI failed or refused — use locally gathered data as fallback
                    result.SynthesizedResponse = FormatFallbackResponse(result);
                    result.Success = result.SuccessfulAgents.Count > 0;
                }
            }
            else
            {
                // No agents gathered data — use fallback directly, don't send empty context to AI
                _logger.LogWarning("No sub-agents gathered data for query: {Query}", query.Length > 100 ? query[..100] + "..." : query);
                result.SynthesizedResponse = FormatFallbackResponse(result);
                result.Success = false;
            }

            _logger.LogInformation("Orchestration complete. Success: {Success}, Agents: {Successful}/{Total}",
                result.Success, result.SuccessfulAgents.Count, domains.Count);

            return result;
        }

        /// <summary>
        /// Gather data from a single sub-agent domain.
        /// </summary>
        private async Task GatherDomainDataAsync(
            string domain,
            string query,
            List<int> caseNumbers,
            System.Collections.Concurrent.ConcurrentDictionary<string, string> contextSections,
            Action<string>? onStatusUpdate,
            CancellationToken cancellationToken)
        {
            try
            {
                string data = domain switch
                {
                    "CaseData" => await GatherCaseDataAsync(query, caseNumbers, cancellationToken),
                    "LawSearch" => await GatherLawDataAsync(query, cancellationToken),
                    "Precedent" => await GatherPrecedentDataAsync(query, cancellationToken),
                    "Cybercrime" => await GatherCybercrimeDataAsync(query),
                    "ScamAnalysis" => await GatherScamDataAsync(query),
                    "Deadline" => await GatherDeadlineDataAsync(query, caseNumbers),
                    "Evidence" => await GatherEvidenceDataAsync(query, caseNumbers),
                    "CDR" => await GatherCDRDataAsync(query),
                    "Timeline" => await GatherTimelineDataAsync(query, caseNumbers),
                    "FIR" => await GatherFIRDataAsync(query, caseNumbers, cancellationToken),
                    "LegalNotice" => await GatherLegalNoticeDataAsync(query),
                    _ => string.Empty
                };

                if (!string.IsNullOrWhiteSpace(data))
                {
                    contextSections[domain] = data;
                    onStatusUpdate?.Invoke($"Received intel from {domain} agent...");
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Sub-agent {Domain} failed: {Error}", domain, ex.Message);
                contextSections[domain] = $"[{domain} agent encountered an error: {ex.Message}]";
            }
        }

        /// <summary>
        /// Wrapper that adds activity tracking (timing, status, summary) around domain gathering.
        /// </summary>
        private async Task GatherDomainDataWithTrackingAsync(
            string domain,
            string query,
            List<int> caseNumbers,
            System.Collections.Concurrent.ConcurrentDictionary<string, string> contextSections,
            List<ChatStateService.AgentActivity> activities,
            Action<string>? onStatusUpdate,
            CancellationToken cancellationToken)
        {
            var activity = activities.FirstOrDefault(a => a.AgentName == domain);
            if (activity != null)
            {
                activity.Status = ChatStateService.AgentStatus.Running;
                activity.StartTime = DateTime.Now;
                onStatusUpdate?.Invoke($"{domain} agent started...");
            }

            try
            {
                await GatherDomainDataAsync(domain, query, caseNumbers, contextSections, onStatusUpdate, cancellationToken);

                if (activity != null)
                {
                    activity.EndTime = DateTime.Now;
                    if (contextSections.TryGetValue(domain, out var data) && !string.IsNullOrWhiteSpace(data))
                    {
                        activity.Status = ChatStateService.AgentStatus.Completed;
                        // Create a brief summary (first line or truncated)
                        var lines = data.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                        activity.DataSummary = lines.Length > 0
                            ? (lines[0].Length > 120 ? lines[0][..120] + "..." : lines[0])
                            : "Data gathered successfully";
                    }
                    else
                    {
                        activity.Status = ChatStateService.AgentStatus.Skipped;
                        activity.DataSummary = "No relevant data found";
                    }
                }
            }
            catch (OperationCanceledException)
            {
                if (activity != null)
                {
                    activity.EndTime = DateTime.Now;
                    activity.Status = ChatStateService.AgentStatus.Failed;
                    activity.ErrorMessage = "Cancelled";
                }
                throw;
            }
            catch (Exception ex)
            {
                if (activity != null)
                {
                    activity.EndTime = DateTime.Now;
                    activity.Status = ChatStateService.AgentStatus.Failed;
                    activity.ErrorMessage = ex.Message;
                }
            }
        }

        // ── Sub-Agent Data Gatherers ──────────────────────────────────────

        private async Task<string> GatherCaseDataAsync(string query, List<int> caseNumbers, CancellationToken ct)
        {
            var sb = new StringBuilder();
            var lowerQuery = query.ToLowerInvariant();

            // Search by case numbers if found
            if (caseNumbers.Count > 0)
            {
                foreach (var caseNum in caseNumbers.Take(3))
                {
                    var caseData = await _caseService.GetCaseByIdAsync(caseNum);
                    if (caseData != null)
                    {
                        AppendCaseDetails(sb, caseData);
                    }
                }
            }

            // Check for status-based queries (pending, active, closed, etc.)
            var statusKeywords = new Dictionary<string, string[]>
            {
                { "pending", new[] { "pending", "filed", "waiting" } },
                { "active", new[] { "active", "ongoing", "in progress", "underinvestigation", "under investigation" } },
                { "closed", new[] { "closed", "resolved", "completed", "disposed" } },
                { "all", new[] { "all case", "my case", "show case", "list case", "view case" } }
            };

            string? matchedStatusGroup = null;
            foreach (var kvp in statusKeywords)
            {
                if (kvp.Value.Any(kw => lowerQuery.Contains(kw)))
                {
                    matchedStatusGroup = kvp.Key;
                    break;
                }
            }

            if (matchedStatusGroup != null)
            {
                var allCases = await _caseService.GetAllCasesAsync();
                List<Case> filtered;

                if (matchedStatusGroup == "all")
                {
                    filtered = allCases.Take(10).ToList();
                    sb.AppendLine($"All cases ({allCases.Count} total):");
                }
                else
                {
                    var statusMatches = statusKeywords[matchedStatusGroup];
                    filtered = allCases.Where(c =>
                        statusMatches.Any(s => c.Status.ToString().Contains(s, StringComparison.OrdinalIgnoreCase)))
                        .Take(10).ToList();
                    sb.AppendLine($"Cases with status '{matchedStatusGroup}' ({filtered.Count} found):");
                }

                foreach (var c in filtered)
                {
                    AppendCaseDetails(sb, c);
                }

                if (!filtered.Any())
                    sb.AppendLine($"  No cases found with status '{matchedStatusGroup}'.");
            }
            else if (caseNumbers.Count == 0)
            {
                // Text search as fallback
                var searchResults = await _caseService.SearchCasesAsync(query);
                if (searchResults?.Any() == true)
                {
                    sb.AppendLine("Related cases from search:");
                    foreach (var c in searchResults.Take(5))
                    {
                        AppendCaseDetails(sb, c);
                    }
                }
                else
                {
                    // If nothing matched, show recent cases as context
                    var allCases = await _caseService.GetAllCasesAsync();
                    if (allCases.Any())
                    {
                        sb.AppendLine($"No exact match found. Showing recent cases ({allCases.Count} total):");
                        foreach (var c in allCases.OrderByDescending(c => c.FiledDate).Take(5))
                        {
                            sb.AppendLine($"  Case #{c.Id}: {c.Title} ({c.Status}) - {c.Type}");
                        }
                    }
                }
            }

            return sb.ToString();
        }

        private static void AppendCaseDetails(StringBuilder sb, Case caseData)
        {
            sb.AppendLine($"Case #{caseData.Id}: {caseData.Title}");
            sb.AppendLine($"  Status: {caseData.Status}, Type: {caseData.Type}");
            sb.AppendLine($"  Complainant: {caseData.Complainant}");
            if (!string.IsNullOrEmpty(caseData.Accused))
                sb.AppendLine($"  Accused: {caseData.Accused}");
            if (!string.IsNullOrEmpty(caseData.PoliceStation))
                sb.AppendLine($"  Police Station: {caseData.PoliceStation}");
            if (!string.IsNullOrEmpty(caseData.InvestigatingOfficer))
                sb.AppendLine($"  IO: {caseData.InvestigatingOfficer}");
            if (caseData.ApplicableLaws?.Any() == true)
                sb.AppendLine($"  Applicable Laws: {string.Join(", ", caseData.ApplicableLaws)}");
            if (!string.IsNullOrEmpty(caseData.Description))
                sb.AppendLine($"  Description: {caseData.Description}");
            sb.AppendLine();
        }

        private async Task<string> GatherLawDataAsync(string query, CancellationToken ct)
        {
            var sb = new StringBuilder();

            var keywords = query.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 2).Take(5).ToArray();
            var searchTerm = string.Join(" ", keywords);

            var laws = _lawService.SearchLaws(searchTerm);
            if (laws?.Any() == true)
            {
                sb.AppendLine("Laws from local database:");
                foreach (var law in laws.Take(5))
                {
                    sb.AppendLine($"  {law.Title} ({law.Type}, {law.ActNumber})");
                    if (law.Sections?.Any() == true)
                        foreach (var sec in law.Sections.Take(3))
                            sb.AppendLine($"    Section {sec.SectionNumber}: {sec.Title}");
                }
            }

            // Also try legal database sections
            var sections = await _legalDbService.SearchSectionsAsync(searchTerm);
            if (sections?.Any() == true)
            {
                sb.AppendLine("Legal sections found:");
                foreach (var sec in sections.Take(5))
                    sb.AppendLine($"  {sec.SectionNumber} - {sec.Title} ({sec.Category})");
            }

            // Online search with timeout to avoid long waits
            try
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(8));
                var onlineResults = await _webSearchService.SearchIndianKanoonAsync(query, 3, timeoutCts.Token);
                if (onlineResults?.Any() == true)
                {
                    sb.AppendLine("Online legal results (Indian Kanoon):");
                    foreach (var r in onlineResults.Take(3))
                    {
                        sb.AppendLine($"  {r.Title}");
                        if (!string.IsNullOrWhiteSpace(r.Snippet))
                            sb.AppendLine($"    {r.Snippet}");
                        sb.AppendLine($"    Source: {r.SourceUrl}");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Law search online lookup timed out after 8s for query: {Query}", searchTerm);
                sb.AppendLine("(Online search timed out — results based on local database only)");
            }

            // Instruct synthesis to supplement with AI's own legal knowledge
            if (sb.Length == 0)
                sb.AppendLine("No local law data found. Please use your own knowledge of Indian law to answer the query.");
            else
                sb.AppendLine("\nIMPORTANT: Also supplement the above with your own knowledge of Indian laws, sections, amendments, and legal principles relevant to the query.");

            return sb.ToString();
        }

        private async Task<string> GatherPrecedentDataAsync(string query, CancellationToken ct)
        {
            var sb = new StringBuilder();

            var precedents = await _precedentService.SearchPrecedentsAsync(query);
            if (precedents?.Any() == true)
            {
                sb.AppendLine("Relevant precedents:");
                foreach (var p in precedents.Take(5))
                {
                    sb.AppendLine($"  {p.CaseTitle} ({p.JudgementDate:yyyy})");
                    sb.AppendLine($"    Court: {p.Court}");
                    if (!string.IsNullOrEmpty(p.Summary))
                        sb.AppendLine($"    Summary: {p.Summary}");
                }
            }

            // Online search
            var onlineResults = await _webSearchService.SearchIndianKanoonAsync(query, 3, ct);
            if (onlineResults?.Any() == true)
            {
                sb.AppendLine("Online precedent results (Indian Kanoon):");
                foreach (var r in onlineResults.Take(3))
                {
                    sb.AppendLine($"  {r.Title}");
                    if (!string.IsNullOrWhiteSpace(r.Snippet))
                        sb.AppendLine($"    {r.Snippet}");
                    sb.AppendLine($"    Source: {r.SourceUrl}");
                }
            }

            return sb.ToString();
        }

        private async Task<string> GatherCybercrimeDataAsync(string query)
        {
            var sb = new StringBuilder();
            var reports = _cybercrimeService.SearchReports(query);
            if (reports?.Any() == true)
            {
                sb.AppendLine("Matching cybercrime reports:");
                foreach (var r in reports.Take(5))
                    sb.AppendLine($"  #{r.Id} {r.IncidentType}: {r.Description?.Substring(0, Math.Min(100, r.Description?.Length ?? 0))} ({r.Category})");
            }

            var resources = _cybercrimeService.GetResources();
            if (resources?.Any() == true)
            {
                var keywords = query.ToLowerInvariant().Split(' ');
                var relevant = resources.Where(r =>
                    keywords.Any(k => r.Title?.Contains(k, StringComparison.OrdinalIgnoreCase) == true ||
                                      r.Description?.Contains(k, StringComparison.OrdinalIgnoreCase) == true))
                    .Take(3).ToList();
                if (relevant.Any())
                {
                    sb.AppendLine("Relevant cybercrime resources:");
                    foreach (var res in relevant)
                        sb.AppendLine($"  {res.Title}: {res.Description}");
                }
            }
            return await Task.FromResult(sb.ToString());
        }

        private async Task<string> GatherScamDataAsync(string query)
        {
            var sb = new StringBuilder();
            var patterns = _scamPatternService.GetPatterns();
            if (patterns?.Any() == true)
            {
                var keywords = query.ToLowerInvariant().Split(' ');
                var relevant = patterns.Where(p =>
                    keywords.Any(k => p.PatternName?.Contains(k, StringComparison.OrdinalIgnoreCase) == true ||
                                      p.Description?.Contains(k, StringComparison.OrdinalIgnoreCase) == true))
                    .Take(5).ToList();

                if (relevant.Any())
                {
                    sb.AppendLine("Matching scam patterns:");
                    foreach (var p in relevant)
                        sb.AppendLine($"  {p.PatternName}: {p.Description}");
                }
            }
            return await Task.FromResult(sb.ToString());
        }

        private async Task<string> GatherDeadlineDataAsync(string query, List<int> caseNumbers)
        {
            var sb = new StringBuilder();
            var deadlines = await _deadlineService.GetAllDeadlinesAsync();
            if (deadlines?.Any() == true)
            {
                // Filter for relevant case deadlines
                var relevant = caseNumbers.Count > 0
                    ? deadlines.Where(d => caseNumbers.Any(cn => d.CaseId == cn.ToString())).ToList()
                    : deadlines.Take(5).ToList();

                if (relevant.Any())
                {
                    sb.AppendLine("Active deadlines:");
                    foreach (var d in relevant.Take(5))
                    {
                        sb.AppendLine($"  {d.Title} - Due: {d.DueDate:dd-MMM-yyyy} ({d.Status})");
                        sb.AppendLine($"    Type: {d.DeadlineType}, Priority: {d.Priority}");
                    }
                }

                // Also get upcoming alerts
                var alerts = await _deadlineService.GetActiveAlertsAsync();
                if (alerts?.Any() == true)
                {
                    sb.AppendLine("Active alerts:");
                    foreach (var a in alerts.Take(3))
                        sb.AppendLine($"  [{a.Severity}] {a.Message}");
                }
            }
            return sb.ToString();
        }

        private async Task<string> GatherEvidenceDataAsync(string query, List<int> caseNumbers)
        {
            var sb = new StringBuilder();

            if (caseNumbers.Count > 0)
            {
                foreach (var caseNum in caseNumbers.Take(2))
                {
                    var caseId = caseNum.ToString();
                    var evidence = await _evidenceService.GetEvidenceByCaseIdAsync(caseId);
                    if (evidence?.Any() == true)
                    {
                        sb.AppendLine($"Evidence for Case #{caseNum}:");
                        foreach (var e in evidence.Take(5))
                        {
                            sb.AppendLine($"  {e.Title} ({e.Type}) - Status: {e.Status}");
                            sb.AppendLine($"    Collected: {e.CollectedAt:dd-MMM-yyyy}, Location: {e.CollectionLocation}");
                        }
                    }
                }
            }

            var stats = await _evidenceService.GetStatisticsAsync();
            sb.AppendLine($"Evidence statistics: Total={stats.TotalEvidence}, Verified={stats.VerifiedCount}, Pending={stats.PendingVerification}");

            return sb.ToString();
        }

        private async Task<string> GatherCDRDataAsync(string query)
        {
            var sb = new StringBuilder();
            // Extract phone numbers from query
            var phonePattern = new Regex(@"\b\d{10,12}\b");
            var phones = phonePattern.Matches(query).Select(m => m.Value).ToList();

            if (phones.Any())
            {
                sb.AppendLine($"Phone numbers detected for CDR analysis: {string.Join(", ", phones)}");
                sb.AppendLine("CDR analysis can be performed on these numbers.");
            }

            // Get any existing CDR analyses
            var analyses = await _cdrService.GetAllAnalysesAsync();
            if (analyses?.Any() == true)
            {
                sb.AppendLine("Existing CDR analyses:");
                foreach (var a in analyses.Take(3))
                    sb.AppendLine($"  Analysis {a.Id}: Case {a.CaseId}, Phone {a.PrimaryNumber} ({a.TotalRecords} records)");
            }

            return await Task.FromResult(sb.ToString());
        }

        private Task<string> GatherTimelineDataAsync(string query, List<int> caseNumbers)
        {
            var sb = new StringBuilder();

            foreach (var caseNum in caseNumbers.Take(2))
            {
                var events = _timelineService.GetTimelineForCase(caseNum);
                if (events?.Any() == true)
                {
                    sb.AppendLine($"Timeline for Case #{caseNum}:");
                    foreach (var e in events.OrderBy(ev => ev.EventDate).Take(10))
                    {
                        sb.AppendLine($"  {e.EventDate:dd-MMM-yyyy}: {e.Title} - {e.Description}");
                    }
                }
            }

            return Task.FromResult(sb.ToString());
        }

        private async Task<string> GatherFIRDataAsync(string query, List<int> caseNumbers, CancellationToken ct)
        {
            var sb = new StringBuilder();

            if (caseNumbers.Count > 0)
            {
                foreach (var caseNum in caseNumbers.Take(2))
                {
                    var caseData = await _caseService.GetCaseByIdAsync(caseNum);
                    if (caseData != null)
                    {
                        sb.AppendLine($"FIR-relevant data for Case #{caseNum}:");
                        sb.AppendLine($"  Title: {caseData.Title}");
                        sb.AppendLine($"  Complainant: {caseData.Complainant}");
                        sb.AppendLine($"  Accused: {caseData.Accused}");
                        sb.AppendLine($"  Police Station: {caseData.PoliceStation}");
                        sb.AppendLine($"  Applicable Laws: {string.Join(", ", caseData.ApplicableLaws ?? new())}");
                        sb.AppendLine($"  Description: {caseData.Description}");
                    }
                }
            }
            else
            {
                // No case numbers specified - show recent cases that have FIR-relevant data
                var allCases = await _caseService.GetAllCasesAsync();
                var firCases = allCases
                    .Where(c => !string.IsNullOrEmpty(c.PoliceStation) || !string.IsNullOrEmpty(c.Accused))
                    .Take(5).ToList();

                if (firCases.Any())
                {
                    sb.AppendLine("Cases with FIR-relevant data:");
                    foreach (var c in firCases)
                    {
                        sb.AppendLine($"  Case #{c.Id}: {c.Title} - Station: {c.PoliceStation}, Status: {c.Status}");
                    }
                }
                else if (allCases.Any())
                {
                    sb.AppendLine("Recent cases available for FIR drafting:");
                    foreach (var c in allCases.Take(5))
                    {
                        sb.AppendLine($"  Case #{c.Id}: {c.Title} ({c.Status})");
                    }
                }
            }

            return sb.ToString();
        }

        private async Task<string> GatherLegalNoticeDataAsync(string query)
        {
            var sb = new StringBuilder();
            var notices = await _legalNoticeService.GetAllNoticesAsync();
            if (notices?.Any() == true)
            {
                var keywords = query.ToLowerInvariant().Split(' ');
                var relevant = notices.Where(n =>
                    keywords.Any(k => n.Subject?.Contains(k, StringComparison.OrdinalIgnoreCase) == true ||
                                      n.Type.ToString().Contains(k, StringComparison.OrdinalIgnoreCase) == true))
                    .Take(5).ToList();

                if (relevant.Any())
                {
                    sb.AppendLine("Related legal notices:");
                    foreach (var n in relevant)
                        sb.AppendLine($"  {n.Type}: {n.Subject} (Case #{n.CaseId})");
                }
            }
            return sb.ToString();
        }

        // ── Helpers ──────────────────────────────────────────────────────

        private static bool IsAgentRefusal(string? message)
        {
            if (string.IsNullOrWhiteSpace(message)) return true;
            var lower = message.ToLowerInvariant();
            return lower.Contains("i'm sorry, but i cannot") ||
                   lower.Contains("i cannot assist with that") ||
                   lower.Contains("i can't assist with that") ||
                   lower.Contains("i'm not able to help with") ||
                   lower.Contains("i'm unable to") ||
                   lower.Contains("as an ai, i cannot");
        }

        private List<int> ExtractCaseNumbers(string query)
        {
            var pattern = new Regex(@"(?:case\s*(?:#|no\.?|number)?\s*|#)(\d{1,6})\b", RegexOptions.IgnoreCase);
            var matches = pattern.Matches(query);
            var numbers = new List<int>();

            foreach (Match m in matches)
            {
                if (int.TryParse(m.Groups[1].Value, out int num))
                    numbers.Add(num);
            }

            // Also catch standalone numbers that could be case IDs
            if (numbers.Count == 0)
            {
                var standalonePattern = new Regex(@"\b(\d{3,6})\b");
                foreach (Match m in standalonePattern.Matches(query))
                {
                    if (int.TryParse(m.Groups[1].Value, out int num))
                        numbers.Add(num);
                }
            }

            return numbers.Distinct().ToList();
        }

        public string FormatFallbackResponse(OrchestrationResult result)
        {
            var sb = new StringBuilder();
            sb.AppendLine("## Multi-Agent Intelligence Report\n");

            if (result.SuccessfulAgents.Count > 0)
            {
                sb.AppendLine($"**Sub-agents consulted:** {string.Join(", ", result.SuccessfulAgents)}\n");
                sb.AppendLine(result.CombinedContext);
            }

            if (result.FailedAgents.Count > 0)
                sb.AppendLine($"\n_Note: Some agents ({string.Join(", ", result.FailedAgents)}) returned no data._");

            return sb.ToString();
        }

        // ── Models ──────────────────────────────────────────────────────

        private record SubAgentDomain(string Name, string[] Keywords);
    }

    /// <summary>
    /// Result from multi-agent orchestration
    /// </summary>
    public class OrchestrationResult
    {
        public bool Success { get; set; }
        public string CombinedContext { get; set; } = string.Empty;
        public string SynthesizedResponse { get; set; } = string.Empty;
        public List<string> DomainsQueried { get; set; } = new();
        public List<string> SuccessfulAgents { get; set; } = new();
        public List<string> FailedAgents { get; set; } = new();
        public List<ChatStateService.AgentActivity> Activities { get; set; } = new();
    }
}

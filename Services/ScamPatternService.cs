using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AILegalAsst.Models;

namespace AILegalAsst.Services
{
    /// <summary>
    /// Service for detecting scam patterns and linking similar fraud cases
    /// </summary>
    public class ScamPatternService
    {
        private readonly List<ScamReport> _reports = new();
        private readonly List<ScamPattern> _patterns = new();
        private readonly AzureAgentService _agentService;
        private readonly ILogger<ScamPatternService> _logger;
        private int _nextReportId = 1;
        private int _nextPatternId = 1;

        public ScamPatternService(AzureAgentService agentService, ILogger<ScamPatternService> logger)
        {
            _agentService = agentService;
            _logger = logger;
            InitializeSampleData();
        }

        /// <summary>
        /// Analyze a complaint text and find matching patterns
        /// </summary>
        public async Task<ScamAnalysisResult> AnalyzeComplaintAsync(string complaintText, ScamReport report)
        {
            var result = new ScamAnalysisResult
            {
                Matches = new List<ScamMatch>(),
                MatchesByState = new Dictionary<string, int>(),
                Warnings = new List<string>(),
                RecommendedActions = new List<string>()
            };

            // Extract identifiers from complaint
            var extractedPhones = ExtractPhoneNumbers(complaintText);
            var extractedUpiIds = ExtractUpiIds(complaintText);
            var extractedEmails = ExtractEmails(complaintText);
            var extractedUrls = ExtractUrls(complaintText);
            var keywords = ExtractKeywords(complaintText);

            // Add extracted data to report
            report.PhoneNumbers.AddRange(extractedPhones);
            report.UpiIds.AddRange(extractedUpiIds);
            report.Emails.AddRange(extractedEmails);
            report.Websites.AddRange(extractedUrls);

            // Search for matches in existing reports
            foreach (var existingReport in _reports)
            {
                // Phone number match
                foreach (var phone in report.PhoneNumbers.Where(p => existingReport.PhoneNumbers.Contains(p)))
                {
                    result.Matches.Add(new ScamMatch
                    {
                        ReportId = existingReport.Id,
                        MatchType = "phone",
                        MatchedValue = phone,
                        Confidence = 0.95,
                        State = existingReport.State,
                        ReportedDate = existingReport.ReportedDate,
                        AmountLost = existingReport.AmountLost
                    });
                }

                // UPI ID match
                foreach (var upi in report.UpiIds.Where(u => existingReport.UpiIds.Contains(u)))
                {
                    result.Matches.Add(new ScamMatch
                    {
                        ReportId = existingReport.Id,
                        MatchType = "upi",
                        MatchedValue = upi,
                        Confidence = 0.98,
                        State = existingReport.State,
                        ReportedDate = existingReport.ReportedDate,
                        AmountLost = existingReport.AmountLost
                    });
                }

                // Bank account match
                foreach (var acc in report.BankAccounts.Where(a => existingReport.BankAccounts.Contains(a)))
                {
                    result.Matches.Add(new ScamMatch
                    {
                        ReportId = existingReport.Id,
                        MatchType = "account",
                        MatchedValue = acc,
                        Confidence = 0.99,
                        State = existingReport.State,
                        ReportedDate = existingReport.ReportedDate,
                        AmountLost = existingReport.AmountLost
                    });
                }

                // Email match
                foreach (var email in report.Emails.Where(e => existingReport.Emails.Contains(e)))
                {
                    result.Matches.Add(new ScamMatch
                    {
                        ReportId = existingReport.Id,
                        MatchType = "email",
                        MatchedValue = email,
                        Confidence = 0.90,
                        State = existingReport.State,
                        ReportedDate = existingReport.ReportedDate,
                        AmountLost = existingReport.AmountLost
                    });
                }
            }

            // Check pattern matches
            foreach (var pattern in _patterns)
            {
                var patternMatch = CheckPatternMatch(report, pattern, keywords);
                if (patternMatch > 0.5)
                {
                    result.IdentifiedPattern = pattern;
                    break;
                }
            }

            // Calculate statistics
            result.HasMatches = result.Matches.Any();
            result.TotalMatches = result.Matches.Count;
            result.TotalFraudAmount = result.Matches.Sum(m => m.AmountLost);
            result.AffectedVictims = result.Matches.Select(m => m.ReportId).Distinct().Count();

            if (result.Matches.Any())
            {
                result.OldestReport = result.Matches.Min(m => m.ReportedDate);
                result.OverallConfidence = result.Matches.Average(m => m.Confidence);
            }

            // Group by state
            foreach (var match in result.Matches)
            {
                if (!result.MatchesByState.ContainsKey(match.State))
                    result.MatchesByState[match.State] = 0;
                result.MatchesByState[match.State]++;
            }

            // Determine risk level
            result.RiskLevel = DetermineRiskLevel(result);

            // Generate warnings and recommendations
            GenerateWarningsAndRecommendations(result, report);

            // Add AI-powered analysis
            result.AIAnalysis = await GenerateAIAnalysisAsync(complaintText, report, result);

            await Task.CompletedTask;
            return result;
        }

        /// <summary>
        /// Generate AI-powered detailed analysis of the scam complaint
        /// </summary>
        private async Task<AIScamAnalysis> GenerateAIAnalysisAsync(string complaintText, ScamReport report, ScamAnalysisResult patternResult)
        {
            var analysis = new AIScamAnalysis
            {
                AnalyzedAt = DateTime.Now,
                ExtractedEvidence = new List<string>(),
                ImmediateSteps = new List<string>(),
                LegalAdvice = new List<string>(),
                ApplicableLaws = new List<string>(),
                PreventionTips = new List<string>()
            };

            // Extract evidence from the complaint
            if (report.PhoneNumbers.Any())
                analysis.ExtractedEvidence.Add($"Phone Numbers: {string.Join(", ", report.PhoneNumbers)}");
            if (report.UpiIds.Any())
                analysis.ExtractedEvidence.Add($"UPI IDs: {string.Join(", ", report.UpiIds)}");
            if (report.Emails.Any())
                analysis.ExtractedEvidence.Add($"Email Addresses: {string.Join(", ", report.Emails)}");
            if (report.Websites.Any())
                analysis.ExtractedEvidence.Add($"Websites/URLs: {string.Join(", ", report.Websites)}");

            // Try AI Agent for comprehensive analysis
            if (_agentService.IsReady)
            {
                try
                {
                    var prompt = $"Analyze this cybercrime/scam complaint under Indian law:\n" +
                        $"Category: {report.Category}\n" +
                        $"Amount Lost: ₹{report.AmountLost:N0}\n" +
                        $"Complaint: {complaintText}\n" +
                        $"Similar Reports Found: {patternResult.TotalMatches}\n" +
                        $"Total Fraud Amount Linked: ₹{patternResult.TotalFraudAmount:N0}\n" +
                        $"Risk Level: {patternResult.RiskLevel}\n\n" +
                        "Respond with sections separated by '---SECTION---' in this order:\n" +
                        "1. ScamType (one line)\n2. Scenario (2-3 sentences)\n3. ComplexityScore (1-10)\n" +
                        "4. NeedsUrgentAction (true/false)\n5. Summary (2-3 sentences)\n" +
                        "6. ImmediateSteps (numbered list)\n7. LegalAdvice (numbered list)\n" +
                        "8. ApplicableLaws (numbered list with sections and penalties)\n" +
                        "9. RecoveryProspect (one paragraph)\n10. PreventionTips (numbered list)\n" +
                        "11. VictimSupportMessage (empathetic message)";

                    var context = "You are an Indian cybercrime legal expert. Provide practical, actionable analysis " +
                        "under Indian IT Act 2000, IPC/BNS, and relevant state laws. Include specific section numbers and penalties.";

                    var response = await _agentService.SendMessageAsync(prompt, context);
                    if (response.Success && !string.IsNullOrWhiteSpace(response.Message))
                    {
                        var sections = response.Message.Split("---SECTION---", StringSplitOptions.RemoveEmptyEntries);
                        if (sections.Length >= 8)
                        {
                            analysis.ScamType = sections[0].Trim();
                            analysis.MostLikelyScenario = sections[1].Trim();
                            if (int.TryParse(sections[2].Trim(), out var complexity))
                                analysis.ScamComplexityScore = complexity;
                            analysis.NeedsUrgentAction = sections[3].Trim().Contains("true", StringComparison.OrdinalIgnoreCase) || report.AmountLost > 100000;
                            analysis.Summary = sections[4].Trim();
                            analysis.ImmediateSteps = ParseNumberedList(sections[5]);
                            analysis.LegalAdvice = ParseNumberedList(sections[6]);
                            analysis.ApplicableLaws = ParseNumberedList(sections[7]);
                            if (sections.Length > 8) analysis.RecoveryProspect = sections[8].Trim();
                            if (sections.Length > 9) analysis.PreventionTips = ParseNumberedList(sections[9]);
                            if (sections.Length > 10) analysis.VictimSupportMessage = sections[10].Trim();

                            return analysis;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "AI Agent scam analysis failed, using template-based analysis");
                }
            }

            // Fallback: Template-based analysis
            var (scamType, scenario, complexity2, urgent) = AnalyzeScamCategory(report.Category, complaintText, report.AmountLost);
            analysis.ScamType = scamType;
            analysis.MostLikelyScenario = scenario;
            analysis.ScamComplexityScore = complexity2;
            analysis.NeedsUrgentAction = urgent || report.AmountLost > 100000;
            analysis.Summary = GenerateAISummary(report, patternResult);
            analysis.ImmediateSteps = GetImmediateSteps(report.Category, report.AmountLost);
            analysis.LegalAdvice = GetLegalAdvice(report.Category);
            analysis.ApplicableLaws = GetApplicableLaws(report.Category);
            analysis.RecoveryProspect = AssessRecoveryProspect(report, patternResult);
            analysis.PreventionTips = GetPreventionTips(report.Category);
            analysis.VictimSupportMessage = GenerateSupportMessage(report.AmountLost, patternResult.HasMatches);

            return analysis;
        }

        private List<string> ParseNumberedList(string text)
        {
            return text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim().TrimStart('0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '.', '-', '*', ' '))
                .Where(line => line.Length > 5)
                .ToList();
        }

        private (string scamType, string scenario, int complexity, bool urgent) AnalyzeScamCategory(ScamCategory category, string text, decimal amount)
        {
            return category switch
            {
                ScamCategory.UPIFraud => (
                    "UPI Payment Fraud",
                    "Scammer likely used social engineering to trick the victim into making UPI payments or sharing OTP. Common tactics include fake customer care numbers, refund scams, or payment request manipulation.",
                    amount > 50000 ? 7 : 5,
                    amount > 100000
                ),
                ScamCategory.LotteryScam => (
                    "Lottery/Prize Fraud",
                    "Victim was promised a lottery win or prize but asked to pay 'processing fees', 'taxes', or 'customs charges'. This is a classic advance-fee fraud with zero chance of receiving any actual prize.",
                    4,
                    false
                ),
                ScamCategory.JobFraud => (
                    "Employment/Job Fraud",
                    "Fraudulent job offer requiring upfront payment for training, registration, or equipment. May also involve work-from-home scams with fake task completion payments.",
                    6,
                    amount > 50000
                ),
                ScamCategory.InvestmentScam => (
                    "Investment/Trading Fraud",
                    "Ponzi scheme or fake trading platform promising unrealistic returns. Scammers often show fake profits initially to encourage larger investments before disappearing.",
                    8,
                    true
                ),
                ScamCategory.ImpersonationScam => (
                    "Authority Impersonation Fraud",
                    "Scammer impersonated police, bank official, or government authority to create fear and urgency. May have claimed arrest warrant, account freeze, or legal action to extort money.",
                    7,
                    true
                ),
                ScamCategory.OTPFraud => (
                    "OTP/Verification Fraud",
                    "Victim was tricked into sharing OTP or clicking on phishing links, allowing unauthorized access to bank accounts or UPI apps. Transaction likely occurred within minutes.",
                    6,
                    true
                ),
                ScamCategory.SextortionScam => (
                    "Sextortion/Blackmail",
                    "Scammer obtained or created compromising material and is threatening to release it unless payment is made. This is serious extortion and requires immediate police intervention.",
                    9,
                    true
                ),
                ScamCategory.RomanceScam => (
                    "Romance/Dating Fraud",
                    "Long-term manipulation through fake romantic relationship. Scammer built emotional connection before requesting money for emergencies, travel, or investments.",
                    8,
                    false
                ),
                ScamCategory.CryptoScam => (
                    "Cryptocurrency Fraud",
                    "Fake crypto exchange, investment scheme, or wallet scam. Funds moved to crypto are extremely difficult to trace or recover.",
                    9,
                    amount > 100000
                ),
                _ => (
                    "Online Fraud",
                    "This appears to be an online fraud case. Analysis suggests potential financial loss through deceptive practices.",
                    5,
                    amount > 100000
                )
            };
        }

        private string GenerateAISummary(ScamReport report, ScamAnalysisResult result)
        {
            var summary = $"This is a {GetCategoryDisplayName(report.Category)} case ";
            
            if (report.AmountLost > 0)
                summary += $"involving ₹{report.AmountLost:N0}. ";
            else
                summary += "with unspecified financial loss. ";

            if (result.HasMatches)
            {
                summary += $"Our AI has identified {result.TotalMatches} similar reports across {result.MatchesByState.Count} states, ";
                summary += $"with a combined fraud amount of ₹{result.TotalFraudAmount:N0}. ";
                
                if (result.IdentifiedPattern != null)
                    summary += $"This matches the known '{result.IdentifiedPattern.PatternName}' scam pattern under active investigation.";
            }
            else
            {
                summary += "This appears to be a new or isolated case with no matching patterns in our database yet.";
            }

            return summary;
        }

        private List<string> GetImmediateSteps(ScamCategory category, decimal amount)
        {
            var steps = new List<string>
            {
                "Stop all communication with the scammer immediately",
                "Do not make any more payments or transfers",
                "Preserve all evidence - screenshots, messages, call recordings"
            };

            if (amount > 0)
            {
                steps.Add("Contact your bank immediately to report the fraud transaction");
                steps.Add("Request bank to initiate chargeback or hold if possible");
            }

            steps.Add("File complaint at cybercrime.gov.in with all evidence");
            steps.Add("Report to your local cyber police station for FIR");

            if (category == ScamCategory.SextortionScam)
            {
                steps.Insert(0, "DO NOT pay any amount - it will only lead to more demands");
                steps.Add("Contact trusted family member or counselor for support");
            }

            if (category == ScamCategory.ImpersonationScam)
            {
                steps.Add("Report the fake authority number/email to respective department");
            }

            return steps;
        }

        private List<string> GetLegalAdvice(ScamCategory category)
        {
            var advice = new List<string>
            {
                "File an FIR at your nearest police station - this is your legal right under Section 154 CrPC",
                "If police refuse to register FIR, you can approach the Superintendent of Police or Court",
                "Keep copies of all complaints and acknowledgment receipts"
            };

            if (category == ScamCategory.InvestmentScam || category == ScamCategory.CryptoScam)
            {
                advice.Add("Report to SEBI if it involves securities or investment schemes");
                advice.Add("Consider filing complaint with Economic Offenses Wing (EOW)");
            }

            if (category == ScamCategory.BankingFraud || category == ScamCategory.OTPFraud)
            {
                advice.Add("Report to RBI Banking Ombudsman within 30 days if bank doesn't respond");
                advice.Add("Under RBI guidelines, banks must credit unauthorized transactions within 10 days");
            }

            advice.Add("You can track your complaint status on cybercrime.gov.in portal");
            
            return advice;
        }

        private List<string> GetApplicableLaws(ScamCategory category)
        {
            var laws = new List<string>
            {
                "Section 66D IT Act, 2000 - Cheating by personation using computer resource (3 years imprisonment + fine)",
                "Section 420 IPC - Cheating and dishonestly inducing delivery of property (7 years imprisonment)"
            };

            switch (category)
            {
                case ScamCategory.OTPFraud:
                case ScamCategory.PhishingScam:
                    laws.Add("Section 66C IT Act - Identity theft (3 years imprisonment + ₹1 lakh fine)");
                    break;
                case ScamCategory.SextortionScam:
                    laws.Add("Section 67/67A IT Act - Obscene material in electronic form (5 years + ₹10 lakh fine)");
                    laws.Add("Section 384 IPC - Extortion (3 years imprisonment)");
                    laws.Add("Section 354D IPC - Stalking (3 years imprisonment for first offense)");
                    break;
                case ScamCategory.ImpersonationScam:
                    laws.Add("Section 170 IPC - Personating a public servant (2 years imprisonment)");
                    break;
                case ScamCategory.InvestmentScam:
                    laws.Add("Maharashtra Protection of Interest of Depositors Act, 1999");
                    laws.Add("SEBI Act for unauthorized investment schemes");
                    break;
            }

            laws.Add("Section 43A IT Act - Compensation for failure to protect data");
            
            return laws;
        }

        private string AssessRecoveryProspect(ScamReport report, ScamAnalysisResult result)
        {
            if (report.ReportedDate.Date == DateTime.Today)
            {
                return "HIGH CHANCE - Reported same day. Contact bank immediately for transaction reversal. Banks can freeze beneficiary accounts within 24 hours.";
            }
            
            var daysSince = (DateTime.Now - report.ReportedDate).Days;
            
            if (daysSince <= 3)
            {
                return "GOOD CHANCE - Reported within 3 days. Banks may still be able to freeze funds. File complaint immediately and share acknowledgment with bank.";
            }
            
            if (daysSince <= 7)
            {
                return "MODERATE CHANCE - Funds may have been withdrawn, but frozen assets from linked scam accounts might provide partial recovery.";
            }

            if (result.HasMatches && result.IdentifiedPattern != null)
            {
                return "POSSIBLE - This is part of a known scam pattern. If arrests are made, victims may receive compensation from seized assets through court orders.";
            }

            return "CHALLENGING - Significant time has passed. Focus on filing formal complaints and preserving evidence. Recovery may happen through investigation but will take time.";
        }

        private List<string> GetPreventionTips(ScamCategory category)
        {
            var tips = new List<string>
            {
                "Never share OTP, PIN, or passwords with anyone, even if they claim to be from bank",
                "Verify caller identity by calling back on official numbers from bank website"
            };

            switch (category)
            {
                case ScamCategory.UPIFraud:
                    tips.Add("To receive money, you never need to enter PIN - if asked, it's a scam");
                    tips.Add("Use only official bank apps downloaded from Play Store/App Store");
                    break;
                case ScamCategory.JobFraud:
                    tips.Add("Legitimate employers never ask for money for jobs");
                    tips.Add("Verify company on MCA portal and check reviews before applying");
                    break;
                case ScamCategory.InvestmentScam:
                    tips.Add("No legitimate investment guarantees high fixed returns");
                    tips.Add("Check if investment scheme is registered with SEBI/RBI");
                    break;
                case ScamCategory.LotteryScam:
                    tips.Add("You cannot win a lottery you never entered");
                    tips.Add("Genuine prizes never require advance payment");
                    break;
            }

            tips.Add("When in doubt, wait 24 hours before making any payment");
            tips.Add("Enable transaction alerts and monitor your bank statements regularly");
            
            return tips;
        }

        private string GenerateSupportMessage(decimal amountLost, bool hasMatches)
        {
            var message = "We understand this is a difficult situation. Being a victim of fraud is not your fault - scammers are sophisticated criminals who manipulate people. ";
            
            if (amountLost > 100000)
            {
                message += "Given the significant amount involved, we strongly recommend immediate action. ";
            }

            if (hasMatches)
            {
                message += "The good news is that similar cases have been identified, which increases the chances of collective investigation and recovery. ";
            }

            message += "Take this one step at a time: first secure your accounts, then file the complaint, and preserve all evidence. You're not alone in this fight.";

            return message;
        }

        /// <summary>
        /// Submit a new scam report
        /// </summary>
        public async Task<ScamReport> SubmitReportAsync(ScamReport report)
        {
            report.Id = _nextReportId++;
            report.ReportedDate = DateTime.Now;
            
            // Analyze and link to patterns
            var analysis = await AnalyzeComplaintAsync(report.Description, report);
            if (analysis.IdentifiedPattern != null)
            {
                report.MatchedPatternId = analysis.IdentifiedPattern.Id;
            }
            report.SimilarReports = analysis.Matches;
            report.MatchConfidence = analysis.OverallConfidence;

            _reports.Add(report);

            // Update pattern statistics
            UpdatePatternStatistics(report);

            return report;
        }

        /// <summary>
        /// Get all reports matching specific criteria
        /// </summary>
        public List<ScamReport> GetReports(ScamCategory? category = null, string? state = null, int? limit = null)
        {
            var query = _reports.AsEnumerable();

            if (category.HasValue)
                query = query.Where(r => r.Category == category.Value);

            if (!string.IsNullOrEmpty(state))
                query = query.Where(r => r.State == state);

            query = query.OrderByDescending(r => r.ReportedDate);

            if (limit.HasValue)
                query = query.Take(limit.Value);

            return query.ToList();
        }

        /// <summary>
        /// Get identified scam patterns
        /// </summary>
        public List<ScamPattern> GetPatterns(PatternSeverity? minSeverity = null)
        {
            var query = _patterns.AsEnumerable();

            if (minSeverity.HasValue)
                query = query.Where(p => p.Severity >= minSeverity.Value);

            return query.OrderByDescending(p => p.TotalComplaints).ToList();
        }

        /// <summary>
        /// Get dashboard statistics
        /// </summary>
        public ScamStatistics GetStatistics()
        {
            var today = DateTime.Today;
            var monthStart = new DateTime(today.Year, today.Month, 1);

            return new ScamStatistics
            {
                TotalReportsToday = _reports.Count(r => r.ReportedDate.Date == today),
                TotalReportsThisMonth = _reports.Count(r => r.ReportedDate >= monthStart),
                TotalPatternsIdentified = _patterns.Count,
                TotalAmountReported = _reports.Sum(r => r.AmountLost),
                TotalAmountRecovered = _patterns.Sum(p => p.RecoveredAmount),
                ReportsByCategory = _reports.GroupBy(r => r.Category)
                    .ToDictionary(g => g.Key, g => g.Count()),
                ReportsByState = _reports.GroupBy(r => r.State)
                    .ToDictionary(g => g.Key, g => g.Count()),
                TrendingPatterns = _patterns.OrderByDescending(p => p.TotalComplaints).Take(5).ToList(),
                RecentAlerts = _patterns.Where(p => p.Severity >= PatternSeverity.High)
                    .OrderByDescending(p => p.LastReportedDate)
                    .Take(5)
                    .Select(p => $"{p.PatternName}: {p.TotalComplaints} victims, ₹{p.TotalFraudAmount:N0} lost")
                    .ToList()
            };
        }

        /// <summary>
        /// Search reports by phone, UPI, or other identifier
        /// </summary>
        public List<ScamReport> SearchByIdentifier(string identifier)
        {
            var normalized = identifier.Trim().ToLower();
            
            return _reports.Where(r =>
                r.PhoneNumbers.Any(p => p.Contains(normalized)) ||
                r.UpiIds.Any(u => u.ToLower().Contains(normalized)) ||
                r.BankAccounts.Any(a => a.ToLower().Contains(normalized)) ||
                r.Emails.Any(e => e.ToLower().Contains(normalized))
            ).ToList();
        }

        /// <summary>
        /// Get category display name
        /// </summary>
        public string GetCategoryDisplayName(ScamCategory category)
        {
            return category switch
            {
                ScamCategory.UPIFraud => "UPI/Payment Fraud",
                ScamCategory.LotteryScam => "Lottery/Prize Scam",
                ScamCategory.JobFraud => "Job/Employment Fraud",
                ScamCategory.LoanFraud => "Loan/Credit Fraud",
                ScamCategory.InvestmentScam => "Investment/Trading Scam",
                ScamCategory.RomanceScam => "Romance/Dating Scam",
                ScamCategory.TechSupportScam => "Tech Support Scam",
                ScamCategory.ImpersonationScam => "Impersonation (Police/Bank)",
                ScamCategory.PhishingScam => "Phishing/Fake Links",
                ScamCategory.OTPFraud => "OTP/Verification Fraud",
                ScamCategory.SocialMediaScam => "Social Media Scam",
                ScamCategory.EcommerceFraud => "E-commerce/Online Shopping",
                ScamCategory.InsuranceFraud => "Insurance Fraud",
                ScamCategory.CryptoScam => "Cryptocurrency Scam",
                ScamCategory.SextortionScam => "Sextortion/Blackmail",
                ScamCategory.CustomsScam => "Customs/Parcel Scam",
                ScamCategory.CourierScam => "Courier/Delivery Scam",
                ScamCategory.BankingFraud => "Banking/Card Fraud",
                _ => "Other Fraud"
            };
        }

        /// <summary>
        /// Get category icon
        /// </summary>
        public string GetCategoryIcon(ScamCategory category)
        {
            return category switch
            {
                ScamCategory.UPIFraud => "bi-phone",
                ScamCategory.LotteryScam => "bi-gift",
                ScamCategory.JobFraud => "bi-briefcase",
                ScamCategory.LoanFraud => "bi-cash-stack",
                ScamCategory.InvestmentScam => "bi-graph-up-arrow",
                ScamCategory.RomanceScam => "bi-heart",
                ScamCategory.TechSupportScam => "bi-headset",
                ScamCategory.ImpersonationScam => "bi-person-badge",
                ScamCategory.PhishingScam => "bi-link-45deg",
                ScamCategory.OTPFraud => "bi-key",
                ScamCategory.SocialMediaScam => "bi-facebook",
                ScamCategory.EcommerceFraud => "bi-cart",
                ScamCategory.InsuranceFraud => "bi-shield",
                ScamCategory.CryptoScam => "bi-currency-bitcoin",
                ScamCategory.SextortionScam => "bi-exclamation-triangle",
                ScamCategory.CustomsScam => "bi-box-seam",
                ScamCategory.CourierScam => "bi-truck",
                ScamCategory.BankingFraud => "bi-bank",
                _ => "bi-question-circle"
            };
        }

        #region Private Helper Methods

        private List<string> ExtractPhoneNumbers(string text)
        {
            var pattern = @"\b(?:\+91[-\s]?)?[6-9]\d{9}\b";
            return Regex.Matches(text, pattern)
                .Select(m => Regex.Replace(m.Value, @"[\s-]", ""))
                .Distinct()
                .ToList();
        }

        private List<string> ExtractUpiIds(string text)
        {
            var pattern = @"\b[\w.-]+@[\w]+\b";
            return Regex.Matches(text, pattern, RegexOptions.IgnoreCase)
                .Select(m => m.Value.ToLower())
                .Where(u => u.Contains("@") && (u.EndsWith("@upi") || u.EndsWith("@paytm") || 
                    u.EndsWith("@ybl") || u.EndsWith("@ibl") || u.EndsWith("@axl") || 
                    u.EndsWith("@okaxis") || u.EndsWith("@oksbi") || u.EndsWith("@okhdfcbank")))
                .Distinct()
                .ToList();
        }

        private List<string> ExtractEmails(string text)
        {
            var pattern = @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b";
            return Regex.Matches(text, pattern, RegexOptions.IgnoreCase)
                .Select(m => m.Value.ToLower())
                .Where(e => !e.EndsWith("@upi") && !e.EndsWith("@paytm")) // Exclude UPI IDs
                .Distinct()
                .ToList();
        }

        private List<string> ExtractUrls(string text)
        {
            var pattern = @"https?://[^\s<>""]+|www\.[^\s<>""]+";
            return Regex.Matches(text, pattern, RegexOptions.IgnoreCase)
                .Select(m => m.Value.ToLower())
                .Distinct()
                .ToList();
        }

        private List<string> ExtractKeywords(string text)
        {
            var scamKeywords = new[]
            {
                "lottery", "winner", "prize", "lucky", "congratulations",
                "processing fee", "advance payment", "verification fee",
                "customs", "parcel", "stuck", "release",
                "job", "work from home", "salary", "earn",
                "investment", "trading", "profit", "returns",
                "loan", "credit", "interest", "emi",
                "otp", "verification", "bank", "account blocked",
                "kyc", "update", "link expire", "urgent"
            };

            return scamKeywords.Where(k => text.ToLower().Contains(k)).ToList();
        }

        private double CheckPatternMatch(ScamReport report, ScamPattern pattern, List<string> keywords)
        {
            double score = 0;
            int checks = 0;

            // Phone match
            if (report.PhoneNumbers.Any(p => pattern.PhoneNumbers.Contains(p)))
            {
                score += 0.4;
            }
            checks++;

            // UPI match
            if (report.UpiIds.Any(u => pattern.UpiIds.Contains(u)))
            {
                score += 0.3;
            }
            checks++;

            // Keyword match
            var keywordMatches = keywords.Count(k => pattern.Keywords.Any(pk => pk.ToLower().Contains(k)));
            if (keywordMatches > 0)
            {
                score += Math.Min(0.3, keywordMatches * 0.1);
            }
            checks++;

            // Category match
            if (report.Category == pattern.Category)
            {
                score += 0.2;
            }
            checks++;

            return score;
        }

        private RiskLevel DetermineRiskLevel(ScamAnalysisResult result)
        {
            if (result.TotalMatches >= 20 || result.TotalFraudAmount >= 1000000)
                return RiskLevel.Critical;
            if (result.TotalMatches >= 10 || result.TotalFraudAmount >= 500000)
                return RiskLevel.High;
            if (result.TotalMatches >= 5 || result.TotalFraudAmount >= 100000)
                return RiskLevel.Medium;
            if (result.TotalMatches >= 1)
                return RiskLevel.Low;
            return RiskLevel.NoRisk;
        }

        private void GenerateWarningsAndRecommendations(ScamAnalysisResult result, ScamReport report)
        {
            if (result.TotalMatches > 0)
            {
                result.Warnings.Add($"This phone/UPI is linked to {result.TotalMatches} other complaints!");
                result.Warnings.Add($"Total fraud amount: ₹{result.TotalFraudAmount:N0}");
                result.Warnings.Add($"Affected victims: {result.AffectedVictims} across {result.MatchesByState.Count} states");
            }

            if (result.IdentifiedPattern != null)
            {
                result.Warnings.Add($"Matches known scam pattern: {result.IdentifiedPattern.PatternName}");
                if (result.IdentifiedPattern.IsActiveInvestigation)
                {
                    result.Warnings.Add("Active police investigation in progress!");
                }
            }

            // Recommendations
            result.RecommendedActions.Add("File an FIR at your nearest police station");
            result.RecommendedActions.Add("Report on National Cyber Crime Portal (cybercrime.gov.in)");
            
            if (report.AmountLost > 0)
            {
                result.RecommendedActions.Add("Contact your bank immediately to freeze transaction");
                result.RecommendedActions.Add("Request bank statement for the fraud transaction");
            }

            if (result.TotalMatches >= 5)
            {
                result.RecommendedActions.Add("Consider joining class action with other victims");
            }
        }

        private void UpdatePatternStatistics(ScamReport report)
        {
            if (report.MatchedPatternId.HasValue)
            {
                var pattern = _patterns.FirstOrDefault(p => p.Id == report.MatchedPatternId);
                if (pattern != null)
                {
                    pattern.TotalComplaints++;
                    pattern.TotalFraudAmount += report.AmountLost;
                    pattern.LastReportedDate = DateTime.Now;
                    
                    if (!pattern.AffectedStates.Contains(report.State))
                        pattern.AffectedStates.Add(report.State);
                }
            }
        }

        private void InitializeSampleData()
        {
            // Sample patterns
            _patterns.AddRange(new[]
            {
                new ScamPattern
                {
                    Id = _nextPatternId++,
                    PatternName = "Lottery Winner Scam Ring",
                    Description = "Callers claim victim won lottery, demand processing fee via UPI",
                    Category = ScamCategory.LotteryScam,
                    Keywords = new List<string> { "lottery", "winner", "prize", "processing fee", "lucky draw" },
                    PhoneNumbers = new List<string> { "9876543210", "9876543211", "9876543212" },
                    UpiIds = new List<string> { "lottery.winner@paytm", "prizewin@ybl" },
                    AffectedStates = new List<string> { "Maharashtra", "Delhi", "Karnataka", "Tamil Nadu", "Gujarat" },
                    TotalComplaints = 156,
                    TotalFraudAmount = 2450000,
                    RecoveredAmount = 340000,
                    FirstReportedDate = DateTime.Now.AddMonths(-8),
                    LastReportedDate = DateTime.Now.AddDays(-2),
                    IsActiveInvestigation = true,
                    Severity = PatternSeverity.High,
                    ModusOperandi = new List<string>
                    {
                        "Random calls claiming lottery win",
                        "Demand 'processing fee' ₹2,000-₹10,000",
                        "Multiple calls requesting more payments",
                        "Use voice changers, fake accents"
                    },
                    PreventionTips = new List<string>
                    {
                        "No legitimate lottery requires advance payment",
                        "Never share OTP with anyone",
                        "Block suspicious numbers immediately"
                    }
                },
                new ScamPattern
                {
                    Id = _nextPatternId++,
                    PatternName = "KYC Update Banking Fraud",
                    Description = "Impersonators claim bank account will be blocked, demand KYC update via fake link",
                    Category = ScamCategory.PhishingScam,
                    Keywords = new List<string> { "kyc", "update", "blocked", "expire", "urgent", "bank" },
                    PhoneNumbers = new List<string> { "9988776655", "9988776656" },
                    Emails = new List<string> { "kyc-update@fakebank.com" },
                    Websites = new List<string> { "kyc-update-sbi.com", "hdfc-kyc-update.in" },
                    AffectedStates = new List<string> { "Maharashtra", "Delhi", "Uttar Pradesh", "West Bengal" },
                    TotalComplaints = 234,
                    TotalFraudAmount = 5670000,
                    RecoveredAmount = 890000,
                    FirstReportedDate = DateTime.Now.AddMonths(-12),
                    LastReportedDate = DateTime.Now.AddDays(-1),
                    IsActiveInvestigation = true,
                    Severity = PatternSeverity.Critical,
                    ModusOperandi = new List<string>
                    {
                        "SMS/Call claiming KYC expired",
                        "Send fake bank link",
                        "Capture credentials on phishing site",
                        "Drain account within minutes"
                    },
                    PreventionTips = new List<string>
                    {
                        "Banks never ask for credentials via SMS/call",
                        "Always use official bank app/website",
                        "Report suspicious messages to bank"
                    }
                },
                new ScamPattern
                {
                    Id = _nextPatternId++,
                    PatternName = "Job Fraud Network",
                    Description = "Fake job offers demanding registration/training fees",
                    Category = ScamCategory.JobFraud,
                    Keywords = new List<string> { "job", "work from home", "salary", "registration", "training fee" },
                    PhoneNumbers = new List<string> { "8877665544", "8877665545" },
                    UpiIds = new List<string> { "jobhire@paytm", "careers.hr@ybl" },
                    Websites = new List<string> { "amazon-jobs-india.com", "flipkart-careers.in" },
                    AffectedStates = new List<string> { "Bihar", "Jharkhand", "Odisha", "Uttar Pradesh" },
                    TotalComplaints = 89,
                    TotalFraudAmount = 1230000,
                    RecoveredAmount = 120000,
                    FirstReportedDate = DateTime.Now.AddMonths(-6),
                    LastReportedDate = DateTime.Now.AddDays(-5),
                    IsActiveInvestigation = false,
                    Severity = PatternSeverity.Medium,
                    ModusOperandi = new List<string>
                    {
                        "WhatsApp messages offering high-paying jobs",
                        "Fake company websites with job listings",
                        "Demand registration/training fee",
                        "Ghost after payment"
                    },
                    PreventionTips = new List<string>
                    {
                        "Legitimate companies never charge for jobs",
                        "Verify company on official platforms",
                        "Check email domain matches company"
                    }
                },
                new ScamPattern
                {
                    Id = _nextPatternId++,
                    PatternName = "Investment Trading Scam",
                    Description = "Fake trading apps promising guaranteed returns",
                    Category = ScamCategory.InvestmentScam,
                    Keywords = new List<string> { "investment", "trading", "profit", "guaranteed returns", "forex" },
                    PhoneNumbers = new List<string> { "7766554433", "7766554434" },
                    Websites = new List<string> { "forex-india-trade.com", "crypto-profit-now.in" },
                    AffectedStates = new List<string> { "Maharashtra", "Gujarat", "Kerala", "Telangana" },
                    TotalComplaints = 67,
                    TotalFraudAmount = 12500000,
                    RecoveredAmount = 450000,
                    FirstReportedDate = DateTime.Now.AddMonths(-10),
                    LastReportedDate = DateTime.Now.AddDays(-3),
                    IsActiveInvestigation = true,
                    Severity = PatternSeverity.Critical,
                    ModusOperandi = new List<string>
                    {
                        "Telegram/WhatsApp groups with 'tips'",
                        "Fake trading app with manipulated returns",
                        "Initial small profits to build trust",
                        "Block withdrawal after large deposit"
                    },
                    PreventionTips = new List<string>
                    {
                        "No legitimate investment guarantees returns",
                        "Only use SEBI-registered brokers",
                        "Check RBI's Sachet portal for warnings"
                    }
                }
            });

            // Sample reports
            var states = new[] { "Maharashtra", "Delhi", "Karnataka", "Tamil Nadu", "Gujarat", "Uttar Pradesh" };
            var cities = new Dictionary<string, string[]>
            {
                { "Maharashtra", new[] { "Mumbai", "Pune", "Nagpur" } },
                { "Delhi", new[] { "New Delhi", "South Delhi", "North Delhi" } },
                { "Karnataka", new[] { "Bangalore", "Mysore", "Mangalore" } },
                { "Tamil Nadu", new[] { "Chennai", "Coimbatore", "Madurai" } },
                { "Gujarat", new[] { "Ahmedabad", "Surat", "Vadodara" } },
                { "Uttar Pradesh", new[] { "Lucknow", "Noida", "Varanasi" } }
            };

            var random = new Random(42);
            for (int i = 0; i < 50; i++)
            {
                var state = states[random.Next(states.Length)];
                var city = cities[state][random.Next(cities[state].Length)];
                var category = (ScamCategory)random.Next(Enum.GetValues<ScamCategory>().Length);
                var pattern = _patterns[random.Next(_patterns.Count)];

                _reports.Add(new ScamReport
                {
                    Id = _nextReportId++,
                    ReporterId = $"user_{i}",
                    ReporterName = $"Victim {i + 1}",
                    Description = $"Received call/message about {GetCategoryDisplayName(category).ToLower()}. Lost money via UPI transfer.",
                    Category = category,
                    AmountLost = random.Next(1000, 100000),
                    IncidentDate = DateTime.Now.AddDays(-random.Next(1, 180)),
                    ReportedDate = DateTime.Now.AddDays(-random.Next(1, 30)),
                    State = state,
                    City = city,
                    PhoneNumbers = new List<string> { pattern.PhoneNumbers.FirstOrDefault() ?? $"98765{random.Next(10000, 99999)}" },
                    UpiIds = pattern.UpiIds.Take(1).ToList(),
                    MatchedPatternId = random.NextDouble() > 0.3 ? pattern.Id : null,
                    MatchConfidence = random.NextDouble() * 0.5 + 0.5
                });
            }
        }

        #endregion
    }
}

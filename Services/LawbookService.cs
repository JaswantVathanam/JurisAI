using System.Text.Json;
using AILegalAsst.Models;

namespace AILegalAsst.Services;

/// <summary>
/// Comprehensive pocket lawbook service
/// Manages constitution articles, acts, precedents, and legal procedures
/// Data sourced from official Government of India, Supreme Court, and legal databases
/// </summary>
public class LawbookService
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<LawbookService> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    // Cached data
    private List<ConstitutionArticle>? _constitutionData;
    private List<ComprehensiveAct>? _actsData;
    private List<LandmarkPrecedent>? _precedentsData;
    private CourseOfLawProcedures? _courseOfLawData;

    public LawbookService(IWebHostEnvironment env, ILogger<LawbookService> logger)
    {
        _env = env;
        _logger = logger;
    }

    /// <summary>
    /// Get all constitutional articles with full details
    /// Source: Government of India, Ministry of Law & Justice
    /// </summary>
    public async Task<List<ConstitutionArticle>> GetConstitutionArticlesAsync()
    {
        if (_constitutionData != null)
            return _constitutionData;

        try
        {
            var path = Path.Combine(_env.WebRootPath, "data", "constitution_articles.json");
            if (File.Exists(path))
            {
                var json = await File.ReadAllTextAsync(path);
                _constitutionData = JsonSerializer.Deserialize<List<ConstitutionArticle>>(json, _jsonOptions) ?? new();
                _logger.LogInformation($"Loaded {_constitutionData.Count} constitution articles from file");
            }
            else
            {
                _constitutionData = GetDefaultConstitutionData();
                _logger.LogWarning("Constitution data file not found, using default data");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading constitution data");
            _constitutionData = GetDefaultConstitutionData();
        }

        return _constitutionData;
    }

    /// <summary>
    /// Get comprehensive acts with all sections
    /// Includes: IPC, CrPC, IT Act 2000, BNS, BNSS, Evidence Act, etc.
    /// Source: Government of India legal databases
    /// </summary>
    public async Task<List<ComprehensiveAct>> GetComprehensiveActsAsync()
    {
        if (_actsData != null)
            return _actsData;

        try
        {
            var path = Path.Combine(_env.WebRootPath, "data", "comprehensive_acts.json");
            if (File.Exists(path))
            {
                var json = await File.ReadAllTextAsync(path);
                _actsData = JsonSerializer.Deserialize<List<ComprehensiveAct>>(json, _jsonOptions) ?? new();
                _logger.LogInformation($"Loaded {_actsData.Count} comprehensive acts from file");
            }
            else
            {
                _actsData = GetDefaultActsData();
                _logger.LogWarning("Acts data file not found, using default data");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading acts data");
            _actsData = GetDefaultActsData();
        }

        return _actsData;
    }

    /// <summary>
    /// Get landmark Supreme Court precedents
    /// Landmark cases with full judgment details and ratio decidendi
    /// Source: Supreme Court of India, Indian Kanoon (supremecourtofindia.gov.in)
    /// </summary>
    public async Task<List<LandmarkPrecedent>> GetLandmarkPrecedentsAsync()
    {
        if (_precedentsData != null)
            return _precedentsData;

        try
        {
            var path = Path.Combine(_env.WebRootPath, "data", "comprehensive_precedents.json");
            if (File.Exists(path))
            {
                var json = await File.ReadAllTextAsync(path);
                _precedentsData = JsonSerializer.Deserialize<List<LandmarkPrecedent>>(json, _jsonOptions) ?? new();
                _logger.LogInformation($"Loaded {_precedentsData.Count} landmark precedents from file");
            }
            else
            {
                _precedentsData = GetDefaultPrecedentsData();
                _logger.LogWarning("Precedents data file not found, using default data");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading precedents data");
            _precedentsData = GetDefaultPrecedentsData();
        }

        return _precedentsData;
    }

    /// <summary>
    /// Get complete legal procedures and course of law
    /// FIR → Investigation → Bail → Trial → Judgment → Appeal
    /// Includes timeframes and applicable sections
    /// </summary>
    public async Task<CourseOfLawProcedures> GetCourseOfLawAsync()
    {
        if (_courseOfLawData != null)
            return _courseOfLawData;

        try
        {
            var path = Path.Combine(_env.WebRootPath, "data", "course_of_law.json");
            if (File.Exists(path))
            {
                var json = await File.ReadAllTextAsync(path);
                _courseOfLawData = JsonSerializer.Deserialize<CourseOfLawProcedures>(json, _jsonOptions);
                _logger.LogInformation("Loaded course of law procedures from file");
            }
            else
            {
                _courseOfLawData = GetDefaultCourseOfLawa();
                _logger.LogWarning("Course of law data file not found, using default data");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading course of law data");
            _courseOfLawData = GetDefaultCourseOfLawa();
        }

        return _courseOfLawData ?? new();
    }

    /// <summary>
    /// Search across all legal content
    /// </summary>
    public async Task<SearchResults> SearchAllAsync(string query)
    {
        var results = new SearchResults { Query = query };

        // Search articles
        var articles = await GetConstitutionArticlesAsync();
        results.ArticleMatches = articles
            .Where(a => a.Title?.Contains(query, StringComparison.OrdinalIgnoreCase) == true ||
                        a.Content?.Contains(query, StringComparison.OrdinalIgnoreCase) == true)
            .ToList();

        // Search acts
        var acts = await GetComprehensiveActsAsync();
        results.ActMatches = acts
            .Where(a => a.Title?.Contains(query, StringComparison.OrdinalIgnoreCase) == true ||
                        a.Description?.Contains(query, StringComparison.OrdinalIgnoreCase) == true)
            .ToList();

        // Search precedents
        var precedents = await GetLandmarkPrecedentsAsync();
        results.PrecedentMatches = precedents
            .Where(p => p.CaseTitle?.Contains(query, StringComparison.OrdinalIgnoreCase) == true ||
                        p.HeadnoteText?.Contains(query, StringComparison.OrdinalIgnoreCase) == true)
            .ToList();

        _logger.LogInformation($"Search for '{query}' found {results.ArticleMatches.Count} articles, {results.ActMatches.Count} acts, {results.PrecedentMatches.Count} precedents");

        return results;
    }

    // ===================== DEFAULT DATA (Fallback) =====================

    private List<ConstitutionArticle> GetDefaultConstitutionData()
    {
        return new List<ConstitutionArticle>
        {
            new()
            {
                ArticleNumber = "14",
                Title = "Equality before law",
                ShortText = "State shall not deny to any person equality before law",
                Content = "The State shall not deny to any person equality before the law or the equal protection of the laws within the territory of India. This article ensures that no person shall be treated differently on the basis of caste, creed, colour, religion, sex, or place of birth.",
                Part = "III",
                Category = "Fundamental Rights",
                AppliesTo = "All persons",
                Notes = "Foundational principle of anti-discrimination",
                Source = "Constitution of India"
            },
            new()
            {
                ArticleNumber = "19(1)(a)",
                Title = "Freedom of speech and expression",
                ShortText = "All citizens shall have the right to freedom of speech and expression",
                Content = "Guarantees the right to free speech subject to reasonable restrictions under law. This includes freedom to hold opinions, print and publish, criticize government, and participate in public discourse.",
                Part = "III",
                Category = "Fundamental Rights",
                AppliesTo = "Citizens",
                Notes = "Can be restricted for national security, contempt, defamation, public order",
                Source = "Constitution of India"
            },
            new()
            {
                ArticleNumber = "21",
                Title = "Protection of life and personal liberty",
                ShortText = "No person shall be deprived of life or liberty except by law",
                Content = "This is one of the most important fundamental rights. It protects the right to live with dignity. Courts have expanded it to include privacy, freedom from torture, right to legal aid, freedom of movement, and freedom from arbitrary arrest.",
                Part = "III",
                Category = "Fundamental Rights",
                AppliesTo = "All persons",
                Notes = "Has been interpreted very broadly by courts to protect many derived rights",
                Source = "Constitution of India"
            },
            new()
            {
                ArticleNumber = "32",
                Title = "Right to constitutional remedies",
                ShortText = "Right to move Supreme Court for enforcement of fundamental rights",
                Content = "Guarantees the right to approach the Supreme Court for the enforcement of any of the rights conferred by Part III. Called the 'heart and soul of the Constitution' by Dr. B.R. Ambedkar.",
                Part = "III",
                Category = "Fundamental Rights",
                AppliesTo = "All persons",
                Notes = "The ultimate protector of constitutional rights",
                Source = "Constitution of India"
            }
        };
    }

    private List<ComprehensiveAct> GetDefaultActsData()
    {
        return new List<ComprehensiveAct>
        {
            new()
            {
                ActId = "IPC1860",
                Title = "Indian Penal Code, 1860",
                YearEnacted = 1860,
                Category = "Criminal",
                Description = "Main criminal law code defining crimes and punishments",
                TotalSections = 511,
                Source = "Government of India, Ministry of Law & Justice"
            },
            new()
            {
                ActId = "CRPC1973",
                Title = "Criminal Procedure Code, 1973",
                YearEnacted = 1973,
                Category = "Procedure",
                Description = "Provides procedure for investigation, prosecution and trial",
                TotalSections = 484,
                Source = "Government of India, Ministry of Law & Justice"
            },
            new()
            {
                ActId = "ITA2000",
                Title = "Information Technology Act, 2000",
                YearEnacted = 2000,
                Category = "Cyber",
                Description = "Laws for electronic records, cyber offences, digital commerce",
                TotalSections = 94,
                Source = "Government of India, Ministry of Communications"
            },
            new()
            {
                ActId = "EA1872",
                Title = "Indian Evidence Act, 1872",
                YearEnacted = 1872,
                Category = "Evidence",
                Description = "Governs rules of evidence, witnesses, documents, and proof",
                TotalSections = 167,
                Source = "Government of India, Ministry of Law & Justice"
            }
        };
    }

    private List<LandmarkPrecedent> GetDefaultPrecedentsData()
    {
        return new List<LandmarkPrecedent>
        {
            new()
            {
                CaseId = "SC_1950_001",
                CaseTitle = "A.K. Gopalan v. State of Madras",
                CaseYear = 1950,
                CourtName = "Supreme Court of India",
                JudgmentDate = new DateTime(1950, 1, 27),
                JudgeCName = "B.K. Mukherjea, Sastry, Kania JJ",
                HeadnoteText = "Interpretation of Article 21 - right to liberty",
                RatiDecidendi = "Liberty in Article 21 refers only to physical liberty and not to other forms of liberty",
                Category = "Constitutional Law",
                RelevantArticles = new List<string> { "21" },
                RelevantSections = new List<string>(),
                Source = "Supreme Court of India"
            },
            new()
            {
                CaseId = "SC_1978_001",
                CaseTitle = "Kesavananda Bharati v. State of Kerala",
                CaseYear = 1973,
                CourtName = "Supreme Court of India",
                JudgmentDate = new DateTime(1973, 4, 24),
                JudgeCName = "A.N. Ray, Y.V. Chandrachud JJ",
                HeadnoteText = "Basic structure doctrine of Constitution",
                RatiDecidendi = "Parliament's amending power under Article 368 is not unlimited. Certain basic features of Constitution are beyond the reach of amendment.",
                Category = "Constitutional Law",
                RelevantArticles = new List<string> { "368" },
                RelevantSections = new List<string>(),
                Source = "Supreme Court of India"
            }
        };
    }

    private CourseOfLawProcedures GetDefaultCourseOfLawa()
    {
        return new()
        {
            Title = "Course of Law - Criminal Procedure",
            Description = "Step-by-step procedure from FIR to final judgment",
            Phases = new List<LegalPhase>
            {
                new()
                {
                    PhaseNumber = 1,
                    PhaseName = "Information & Registration",
                    Description = "FIR registration and initial investigation",
                    TimeFrame = "24-48 hours",
                    Section = "CrPC 154"
                },
                new()
                {
                    PhaseNumber = 2,
                    PhaseName = "Investigation",
                    Description = "Police investigation and evidence collection",
                    TimeFrame = "90 days (extendable)",
                    Section = "CrPC 156-173"
                },
                new()
                {
                    PhaseNumber = 3,
                    PhaseName = "First Appearance",
                    Description = "Accused production before magistrate",
                    TimeFrame = "24 hours from arrest",
                    Section = "CrPC 36"
                },
                new()
                {
                    PhaseNumber = 4,
                    PhaseName = "Bail / Remand",
                    Description = "Bail hearing and custody decisions",
                    TimeFrame = "14 days initial remand",
                    Section = "CrPC 436-450"
                },
                new()
                {
                    PhaseNumber = 5,
                    PhaseName = "Chargesheet",
                    Description = "Police submits chargesheet with evidence",
                    TimeFrame = "60-90 days from arrest",
                    Section = "CrPC 173"
                },
                new()
                {
                    PhaseNumber = 6,
                    PhaseName = "Framing of Charges",
                    Description = "Court frames charges against accused",
                    TimeFrame = "90 days after chargesheet",
                    Section = "CrPC 227-229"
                },
                new()
                {
                    PhaseNumber = 7,
                    PhaseName = "Trial",
                    Description = "Evidence and arguments from both sides",
                    TimeFrame = "Several months to years",
                    Section = "CrPC 231-300"
                },
                new()
                {
                    PhaseNumber = 8,
                    PhaseName = "Judgment",
                    Description = "Court pronounces verdict and sentence",
                    TimeFrame = "Variable",
                    Section = "CrPC 354-357"
                },
                new()
                {
                    PhaseNumber = 9,
                    PhaseName = "Appeal",
                    Description = "Right to appeal to higher court",
                    TimeFrame = "90 days to file appeal",
                    Section = "CrPC 401-405"
                }
            },
            Source = "Criminal Procedure Code, 1973"
        };
    }
}

// ===================== MODEL CLASSES =====================

public class ConstitutionArticle
{
    public string? ArticleNumber { get; set; }
    public string? Title { get; set; }
    public string? ShortText { get; set; }
    public string? Content { get; set; }
    public string? Part { get; set; } // Part I, II, III, etc.
    public string? Category { get; set; } // Fundamental Rights, DPSP, Fundamental Duties
    public string? AppliesTo { get; set; }
    public string? Notes { get; set; }
    public string? Source { get; set; }
}

public class ComprehensiveAct
{
    public string? ActId { get; set; }
    public string? Title { get; set; }
    public int YearEnacted { get; set; }
    public string? Category { get; set; } // Criminal, Cyber, Procedure, Evidence, etc.
    public string? Description { get; set; }
    public int TotalSections { get; set; }
    public List<ActSection>? Sections { get; set; }
    public string? Source { get; set; }
}

public class ActSection
{
    public string? SectionNumber { get; set; }
    public string? Title { get; set; }
    public string? Content { get; set; }
    public string? Punishment { get; set; }
    public bool IsBailable { get; set; }
    public bool IsCognizable { get; set; }
}

public class LandmarkPrecedent
{
    public string? CaseId { get; set; }
    public string? CaseTitle { get; set; }
    public int CaseYear { get; set; }
    public string? CourtName { get; set; }
    public DateTime JudgmentDate { get; set; }
    public string? JudgeCName { get; set; }
    public string? HeadnoteText { get; set; }
    public string? RatiDecidendi { get; set; }
    public string? FullJudgmentText { get; set; }
    public string? Category { get; set; }
    public List<string>? RelevantArticles { get; set; } = new();
    public List<string>? RelevantSections { get; set; } = new();
    public string? Source { get; set; }
}

public class CourseOfLawProcedures
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public List<LegalPhase>? Phases { get; set; }
    public string? Source { get; set; }
}

public class LegalPhase
{
    public int PhaseNumber { get; set; }
    public string? PhaseName { get; set; }
    public string? Description { get; set; }
    public string? TimeFrame { get; set; }
    public string? Section { get; set; }
}

public class SearchResults
{
    public string? Query { get; set; }
    public List<ConstitutionArticle> ArticleMatches { get; set; } = new();
    public List<ComprehensiveAct> ActMatches { get; set; } = new();
    public List<LandmarkPrecedent> PrecedentMatches { get; set; } = new();
}

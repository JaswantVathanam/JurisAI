using AILegalAsst.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AILegalAsst.Services;

public class PrecedentService
{
    private readonly List<Precedent> _precedents = new();
    private readonly AzureAgentService _agentService;
    private readonly ILogger<PrecedentService> _logger;
    private readonly IWebHostEnvironment _env;
    private bool _initialized = false;

    public PrecedentService(AzureAgentService agentService, ILogger<PrecedentService> logger, IWebHostEnvironment env)
    {
        _agentService = agentService;
        _logger = logger;
        _env = env;
    }

    private async Task EnsureInitializedAsync()
    {
        if (_initialized) return;
        _initialized = true;
        await LoadPrecedentsFromJsonAsync();
        AddCybercrimePrecedents();
    }

    private async Task LoadPrecedentsFromJsonAsync()
    {
        try
        {
            var filePath = Path.Combine(_env.WebRootPath, "data", "comprehensive_precedents.json");
            if (File.Exists(filePath))
            {
                var json = await File.ReadAllTextAsync(filePath);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var landmarks = JsonSerializer.Deserialize<List<LandmarkPrecedentDto>>(json, options) ?? new();

                int id = 1;
                foreach (var lp in landmarks)
                {
                    _precedents.Add(new Precedent
                    {
                        Id = id++,
                        CaseTitle = lp.CaseTitle ?? "",
                        CaseCitation = lp.CaseId ?? "",
                        Court = lp.CourtName ?? "",
                        JudgementDate = DateTime.TryParse(lp.JudgmentDate, out var dt) ? dt : DateTime.MinValue,
                        Judge = lp.JudgeCName ?? "",
                        Summary = lp.HeadnoteText ?? "",
                        Facts = lp.FullJudgmentText ?? "",
                        LegalIssues = lp.Category ?? "",
                        Judgement = lp.RatiDecidendi ?? "",
                        Ratio = lp.RatiDecidendi ?? "",
                        ApplicableLaws = new List<string> { lp.Source ?? "Indian Law" },
                        Sections = (lp.RelevantSections ?? new()).Concat(
                            (lp.RelevantArticles ?? new()).Select(a => $"Article {a}")).ToList(),
                        Keywords = new List<string> { lp.Category ?? "" },
                        IsLandmarkCase = true,
                        CitationCount = 0
                    });
                }
                _logger.LogInformation("Loaded {Count} precedents from JSON", _precedents.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load precedents from JSON");
        }
    }

    private void AddCybercrimePrecedents()
    {
        // Add cybercrime-specific precedents if not already loaded from JSON
        var existingTitles = _precedents.Select(p => p.CaseTitle).ToHashSet(StringComparer.OrdinalIgnoreCase);
        int nextId = _precedents.Count > 0 ? _precedents.Max(p => p.Id) + 1 : 1;

        if (!existingTitles.Any(t => t.Contains("Suhas Katti", StringComparison.OrdinalIgnoreCase)))
        {
            _precedents.Add(new Precedent
            {
                Id = nextId++,
                CaseTitle = "State of Tamil Nadu v. Suhas Katti",
                CaseCitation = "2004 Cri LJ 4985",
                Court = "Sessions Court, Egmore, Chennai",
                JudgementDate = new DateTime(2004, 11, 5),
                Judge = "Judge M. Jeyapaul",
                Summary = "First conviction in India under IT Act 2000 for cyberstalking and obscene messages",
                Facts = "The accused sent obscene, defamatory, and annoying messages through e-mail and chatrooms",
                LegalIssues = "Application of IT Act 2000 Section 67 for publishing obscene content in electronic form",
                Judgement = "Convicted under Section 469, 509 IPC and Section 67 of IT Act 2000",
                Ratio = "Electronic communication can constitute stalking and harassment under IT Act",
                ApplicableLaws = new List<string> { "IT Act 2000", "IPC" },
                Sections = new List<string> { "Section 67 IT Act", "Section 469 IPC", "Section 509 IPC" },
                Keywords = new List<string> { "Cyberstalking", "Online Harassment", "Obscene Messages", "IT Act" },
                IsCybercrimeRelated = true, CybercrimeCategory = "Cyberstalking", CitationCount = 47
            });
        }

        if (!existingTitles.Any(t => t.Contains("Shreya Singhal", StringComparison.OrdinalIgnoreCase)))
        {
            _precedents.Add(new Precedent
            {
                Id = nextId++,
                CaseTitle = "Shreya Singhal v. Union of India",
                CaseCitation = "AIR 2015 SC 1523",
                Court = "Supreme Court of India",
                JudgementDate = new DateTime(2015, 3, 24),
                Judge = "J. Chelameswar and R.F. Nariman",
                Summary = "Section 66A of IT Act struck down as unconstitutional, violating Article 19(1)(a)",
                Facts = "Challenged constitutional validity of Section 66A IT Act on grounds of freedom of speech",
                LegalIssues = "Whether Section 66A violates fundamental right to freedom of speech and expression",
                Judgement = "Section 66A declared unconstitutional and struck down",
                Ratio = "Restrictions on speech must be reasonable and clearly defined, Section 66A was vague and arbitrary",
                ApplicableLaws = new List<string> { "IT Act 2000", "Constitution of India" },
                Sections = new List<string> { "Section 66A IT Act", "Article 19(1)(a)", "Article 19(2)" },
                Keywords = new List<string> { "Freedom of Speech", "IT Act", "Constitutional Law", "Section 66A" },
                IsCybercrimeRelated = true, CybercrimeCategory = "Constitutional Law", CitationCount = 156
            });
        }

        if (!existingTitles.Any(t => t.Contains("Avnish Bajaj", StringComparison.OrdinalIgnoreCase)))
        {
            _precedents.Add(new Precedent
            {
                Id = nextId++,
                CaseTitle = "Avnish Bajaj v. State",
                CaseCitation = "2005 CriLJ 4601",
                Court = "Delhi High Court",
                JudgementDate = new DateTime(2005, 12, 23),
                Judge = "Justice B.N. Chaturvedi",
                Summary = "Intermediary liability case - CEO of auction site arrested for obscene content posted by users",
                Facts = "Obscene MMS clip sold on Bazee.com (eBay India), CEO arrested under IT Act",
                LegalIssues = "Extent of intermediary liability for user-generated content",
                Judgement = "Granted bail, led to amendments clarifying intermediary liability under Section 79",
                Ratio = "Intermediaries should not be held liable if they act as mere conduits and comply with due diligence",
                ApplicableLaws = new List<string> { "IT Act 2000" },
                Sections = new List<string> { "Section 67 IT Act", "Section 79 IT Act", "Section 85 IT Act" },
                Keywords = new List<string> { "Intermediary Liability", "E-commerce", "Obscene Content", "Safe Harbor" },
                IsCybercrimeRelated = true, CybercrimeCategory = "Intermediary Liability", CitationCount = 89
            });
        }
    }

    private class LandmarkPrecedentDto
    {
        public string? CaseId { get; set; }
        public string? CaseTitle { get; set; }
        public int CaseYear { get; set; }
        public string? CourtName { get; set; }
        public string? JudgmentDate { get; set; }
        public string? JudgeCName { get; set; }
        public string? HeadnoteText { get; set; }
        public string? RatiDecidendi { get; set; }
        public string? FullJudgmentText { get; set; }
        public string? Category { get; set; }
        public List<string>? RelevantArticles { get; set; }
        public List<string>? RelevantSections { get; set; }
        public string? Source { get; set; }
    }

    public async Task<List<Precedent>> GetAllPrecedentsAsync()
    {
        await EnsureInitializedAsync();
        return _precedents.ToList();
    }

    public async Task<Precedent?> GetPrecedentByIdAsync(int id)
    {
        await EnsureInitializedAsync();
        return _precedents.FirstOrDefault(p => p.Id == id);
    }

    public async Task<List<Precedent>> SearchPrecedentsAsync(string query)
    {
        await EnsureInitializedAsync();
        // Try AI-powered semantic search first
        if (_agentService.IsReady && !string.IsNullOrWhiteSpace(query))
        {
            try
            {
                var precedentSummary = string.Join("\n", _precedents.Select(p => $"ID:{p.Id}|{p.CaseTitle}|{p.Summary}|Sections:{string.Join(",", p.Sections)}|Keywords:{string.Join(",", p.Keywords)}"));
                var prompt = $"Given these Indian legal precedents:\n{precedentSummary}\n\nFind precedents relevant to: \"{query}\"\nReturn ONLY the IDs (numbers) of relevant precedents, comma-separated.";
                var context = "You are an Indian legal precedent search expert. Match queries to relevant case law semantically.";

                var response = await _agentService.SendMessageAsync(prompt, context);
                if (response.Success && !string.IsNullOrWhiteSpace(response.Message))
                {
                    var ids = response.Message
                        .Split(new[] { ',', ' ', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                        .Where(s => int.TryParse(s.Trim(), out _))
                        .Select(s => int.Parse(s.Trim()))
                        .ToList();

                    if (ids.Count > 0)
                    {
                        var aiResults = _precedents.Where(p => ids.Contains(p.Id)).ToList();
                        if (aiResults.Count > 0) return aiResults;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AI-powered precedent search failed, falling back to keyword search");
            }
        }

        // Fallback: keyword-based search
        var results = _precedents.Where(p =>
            p.CaseTitle.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            p.Summary.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            p.Keywords.Any(k => k.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
            p.Sections.Any(s => s.Contains(query, StringComparison.OrdinalIgnoreCase))
        ).ToList();

        return results;
    }

    public async Task<List<Precedent>> GetCybercrimePrecedentsAsync()
    {
        await EnsureInitializedAsync();
        return _precedents.Where(p => p.IsCybercrimeRelated).ToList();
    }

    public async Task UpdatePrecedentAsync(Precedent updatedPrecedent)
    {
        await EnsureInitializedAsync();
        var precedent = _precedents.FirstOrDefault(p => p.Id == updatedPrecedent.Id);
        if (precedent != null)
        {
            // Update all editable fields
            precedent.CaseTitle = updatedPrecedent.CaseTitle;
            precedent.CaseCitation = updatedPrecedent.CaseCitation;
            precedent.Citation = updatedPrecedent.Citation;
            precedent.Court = updatedPrecedent.Court;
            precedent.CourtName = updatedPrecedent.CourtName;
            precedent.Judge = updatedPrecedent.Judge;
            precedent.JudgeName = updatedPrecedent.JudgeName;
            precedent.JudgementDate = updatedPrecedent.JudgementDate;
            precedent.Summary = updatedPrecedent.Summary;
            precedent.Judgement = updatedPrecedent.Judgement;
            precedent.JudgementText = updatedPrecedent.JudgementText;
        }
        return;
    }

    public async Task<List<Precedent>> GetSimilarPrecedentsAsync(Case currentCase)
    {
        await EnsureInitializedAsync();
        // Find precedents with matching sections or keywords
        var results = _precedents.Where(p =>
            p.Sections.Any(s => currentCase.Sections.Contains(s)) ||
            (p.IsCybercrimeRelated && currentCase.IsCybercrime &&
             p.CybercrimeCategory == currentCase.CybercrimeCategory)
        ).ToList();

        return results;
    }
}

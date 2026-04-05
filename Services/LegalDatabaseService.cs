using AILegalAsst.Models;

namespace AILegalAsst.Services;

public class LegalDatabaseService
{
    private readonly List<Law> _laws = new();
    private readonly AzureAgentService _agentService;
    private readonly ILogger<LegalDatabaseService> _logger;

    public LegalDatabaseService(AzureAgentService agentService, ILogger<LegalDatabaseService> logger)
    {
        _agentService = agentService;
        _logger = logger;
        InitializeLaws();
    }

    private void InitializeLaws()
    {
        // Information Technology Act, 2000
        var itAct = new Law
        {
            Id = 1,
            Title = "Information Technology Act",
            Type = LawType.Act,
            ActNumber = "21",
            Year = 2000,
            Description = "An Act to provide legal recognition for transactions carried out by means of electronic data interchange and other means of electronic communication",
            EnactedDate = new DateTime(2000, 6, 9),
            LastAmended = new DateTime(2008, 10, 27),
            IsCybercrimeRelated = true,
            Keywords = new List<string> { "Cybercrime", "Electronic Commerce", "Digital Signature", "Hacking", "Data Protection" },
            Sections = new List<LawSection>
            {
                new LawSection
                {
                    Id = 1,
                    SectionNumber = "43",
                    Title = "Penalty and compensation for damage to computer, computer system, etc.",
                    Content = "If any person without permission of the owner or any other person who is in charge of a computer, computer system or computer network, damages or causes to be damaged any computer, computer system or computer network",
                    Punishment = "Liable to pay damages by way of compensation to the person so affected",
                    IsBailable = true,
                    IsCognizable = false
                },
                new LawSection
                {
                    Id = 2,
                    SectionNumber = "66",
                    Title = "Computer related offences",
                    Content = "If any person, dishonestly or fraudulently, does any act referred to in section 43, he shall be punishable with imprisonment for a term which may extend to three years or with fine which may extend to five lakh rupees or with both",
                    Punishment = "Imprisonment up to 3 years or fine up to Rs. 5 lakhs or both",
                    IsBailable = true,
                    IsCognizable = true
                },
                new LawSection
                {
                    Id = 3,
                    SectionNumber = "66C",
                    Title = "Punishment for identity theft",
                    Content = "Whoever, fraudulently or dishonestly make use of the electronic signature, password or any other unique identification feature of any other person, shall be punished with imprisonment of either description for a term which may extend to three years and shall also be liable to fine which may extend to one lakh rupees",
                    Punishment = "Imprisonment up to 3 years and fine up to Rs. 1 lakh",
                    IsBailable = true,
                    IsCognizable = true
                },
                new LawSection
                {
                    Id = 4,
                    SectionNumber = "66D",
                    Title = "Punishment for cheating by personation by using computer resource",
                    Content = "Whoever, by means of any communication device or computer resource cheats by personation, shall be punished with imprisonment of either description for a term which may extend to three years and shall also be liable to fine which may extend to one lakh rupees",
                    Punishment = "Imprisonment up to 3 years and fine up to Rs. 1 lakh",
                    IsBailable = true,
                    IsCognizable = true
                },
                new LawSection
                {
                    Id = 5,
                    SectionNumber = "66E",
                    Title = "Punishment for violation of privacy",
                    Content = "Whoever, intentionally or knowingly captures, publishes or transmits the image of a private area of any person without his or her consent, under circumstances violating the privacy of that person, shall be punished with imprisonment which may extend to three years or with fine not exceeding two lakh rupees, or with both",
                    Punishment = "Imprisonment up to 3 years or fine up to Rs. 2 lakhs or both",
                    IsBailable = true,
                    IsCognizable = true
                },
                new LawSection
                {
                    Id = 6,
                    SectionNumber = "67",
                    Title = "Punishment for publishing or transmitting obscene material in electronic form",
                    Content = "Whoever publishes or transmits or causes to be published or transmitted in the electronic form, any material which is lascivious or appeals to the prurient interest or if its effect is such as to tend to deprave and corrupt persons",
                    Punishment = "First conviction: imprisonment up to 3 years and fine up to Rs. 5 lakhs. Subsequent conviction: imprisonment up to 5 years and fine up to Rs. 10 lakhs",
                    IsBailable = true,
                    IsCognizable = true
                }
            }
        };

        // Indian Penal Code (Cybercrime-related sections)
        var ipc = new Law
        {
            Id = 2,
            Title = "Indian Penal Code (Cybercrime Sections)",
            Type = LawType.Act,
            ActNumber = "45",
            Year = 1860,
            Description = "Selected sections of IPC applicable to cybercrimes",
            EnactedDate = new DateTime(1860, 10, 6),
            IsCybercrimeRelated = true,
            Keywords = new List<string> { "Criminal Law", "Fraud", "Cheating", "Defamation", "Stalking" },
            Sections = new List<LawSection>
            {
                new LawSection
                {
                    Id = 7,
                    SectionNumber = "354D",
                    Title = "Stalking",
                    Content = "Any man who follows a woman and contacts, or attempts to contact such woman to foster personal interaction repeatedly despite a clear indication of disinterest by such woman; or monitors the use by a woman of the internet, email or any other form of electronic communication",
                    Punishment = "First conviction: imprisonment up to 3 years and fine. Subsequent conviction: imprisonment up to 5 years and fine",
                    IsBailable = true,
                    IsCognizable = true
                },
                new LawSection
                {
                    Id = 8,
                    SectionNumber = "420",
                    Title = "Cheating and dishonestly inducing delivery of property",
                    Content = "Whoever cheats and thereby dishonestly induces the person deceived to deliver any property to any person, or to make, alter or destroy the whole or any part of a valuable security",
                    Punishment = "Imprisonment up to 7 years and fine",
                    IsBailable = false,
                    IsCognizable = true
                },
                new LawSection
                {
                    Id = 9,
                    SectionNumber = "463",
                    Title = "Forgery",
                    Content = "Whoever makes any false document or false electronic record or part of a document or electronic record, with intent to cause damage or injury, to the public or to any person",
                    Punishment = "Imprisonment up to 2 years or fine or both",
                    IsBailable = false,
                    IsCognizable = true
                },
                new LawSection
                {
                    Id = 10,
                    SectionNumber = "469",
                    Title = "Forgery for purpose of harming reputation",
                    Content = "Whoever commits forgery, intending that the document or electronic record forged shall harm the reputation of any party",
                    Punishment = "Imprisonment up to 3 years and fine",
                    IsBailable = false,
                    IsCognizable = true
                },
                new LawSection
                {
                    Id = 11,
                    SectionNumber = "499",
                    Title = "Defamation",
                    Content = "Whoever, by words either spoken or intended to be read, or by signs or by visible representations, makes or publishes any imputation concerning any person intending to harm reputation",
                    Punishment = "Simple imprisonment up to 2 years or fine or both",
                    IsBailable = true,
                    IsCognizable = false
                },
                new LawSection
                {
                    Id = 12,
                    SectionNumber = "506",
                    Title = "Punishment for criminal intimidation",
                    Content = "Whoever commits criminal intimidation shall be punished with imprisonment of either description for a term which may extend to two years, or with fine, or with both",
                    Punishment = "Imprisonment up to 2 years or fine or both. If threat be to cause death or grievous hurt: imprisonment up to 7 years or fine or both",
                    IsBailable = true,
                    IsCognizable = true
                }
            }
        };

        _laws.Add(itAct);
        _laws.Add(ipc);
    }

    public Task<List<Law>> GetAllLawsAsync()
    {
        return Task.FromResult(_laws.ToList());
    }

    public Task<Law?> GetLawByIdAsync(int id)
    {
        return Task.FromResult(_laws.FirstOrDefault(l => l.Id == id));
    }

    public Task<List<Law>> GetCybercrimeLawsAsync()
    {
        return Task.FromResult(_laws.Where(l => l.IsCybercrimeRelated).ToList());
    }

    public Task<LawSection?> GetSectionAsync(int lawId, string sectionNumber)
    {
        var law = _laws.FirstOrDefault(l => l.Id == lawId);
        var section = law?.Sections.FirstOrDefault(s => s.SectionNumber == sectionNumber);
        return Task.FromResult(section);
    }

    public async Task<List<LawSection>> SearchSectionsAsync(string query)
    {
        // Try AI semantic search first
        if (_agentService.IsReady)
        {
            try
            {
                var allSections = _laws.SelectMany(l => l.Sections).ToList();
                var sectionSummary = string.Join("\n", allSections.Select(s => $"ID:{s.Id} - Section {s.SectionNumber}: {s.Title}"));
                
                var prompt = $@"You are an Indian legal database search engine. Given this query, return the IDs of the most relevant legal sections.

QUERY: {query}

AVAILABLE SECTIONS:
{sectionSummary}

Return ONLY a comma-separated list of matching section IDs (numbers only). Example: 1,3,5
If no sections match, return: NONE";

                var response = await _agentService.SendMessageAsync(prompt, "Indian legal section semantic search");
                if (response.Success && !string.IsNullOrEmpty(response.Message) && response.Message.Trim() != "NONE")
                {
                    var ids = response.Message.Split(',', StringSplitOptions.TrimEntries)
                        .Select(s => { int.TryParse(s.Trim(), out var id); return id; })
                        .Where(id => id > 0)
                        .ToList();
                    
                    if (ids.Count > 0)
                    {
                        var results = allSections.Where(s => ids.Contains(s.Id)).ToList();
                        if (results.Count > 0)
                            return results;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AI section search failed, falling back to keyword search");
            }
        }

        // Fallback: keyword matching
        var fallbackResults = new List<LawSection>();
        
        foreach (var law in _laws)
        {
            var matchingSections = law.Sections.Where(s =>
                s.SectionNumber.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                s.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                s.Content.Contains(query, StringComparison.OrdinalIgnoreCase)
            );
            
            fallbackResults.AddRange(matchingSections);
        }

        return fallbackResults;
    }

    public async Task<List<Law>> SearchLawsAsync(string query)
    {
        // Try AI semantic search first
        if (_agentService.IsReady)
        {
            try
            {
                var lawSummary = string.Join("\n", _laws.Select(l => $"ID:{l.Id} - {l.Title} ({l.Year}): {l.Description}"));
                
                var prompt = $@"You are an Indian legal search engine. Given this query, return the IDs of matching laws.

QUERY: {query}

AVAILABLE LAWS:
{lawSummary}

Return ONLY a comma-separated list of matching law IDs. Example: 1,2
If no laws match, return: NONE";

                var response = await _agentService.SendMessageAsync(prompt, "Indian legal law semantic search");
                if (response.Success && !string.IsNullOrEmpty(response.Message) && response.Message.Trim() != "NONE")
                {
                    var ids = response.Message.Split(',', StringSplitOptions.TrimEntries)
                        .Select(s => { int.TryParse(s.Trim(), out var id); return id; })
                        .Where(id => id > 0)
                        .ToList();
                    
                    if (ids.Count > 0)
                    {
                        var results = _laws.Where(l => ids.Contains(l.Id)).ToList();
                        if (results.Count > 0)
                            return results;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AI law search failed, falling back to keyword search");
            }
        }

        // Fallback: keyword matching
        var fallbackResults = _laws.Where(l =>
            l.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            l.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            l.Keywords.Any(k => k.Contains(query, StringComparison.OrdinalIgnoreCase))
        ).ToList();

        return fallbackResults;
    }
}

namespace AILegalAsst.Models;

public class Law
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public LawType Type { get; set; }
    public string? ActNumber { get; set; }
    public int? Year { get; set; }
    
    public string Description { get; set; } = string.Empty;
    public List<LawSection> Sections { get; set; } = new();
    
    public DateTime? EnactedDate { get; set; }
    public DateTime? LastAmended { get; set; }
    public List<string> Amendments { get; set; } = new();
    
    public bool IsCybercrimeRelated { get; set; }
    public List<string> Keywords { get; set; } = new();
}

public enum LawType
{
    Constitution,
    Act,
    Ordinance,
    Rule,
    Regulation,
    Amendment
}

public class LawSection
{
    public int Id { get; set; }
    public string SectionNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Explanation { get; set; }
    public List<string> Exceptions { get; set; } = new();
    public string? Punishment { get; set; }
    public bool IsBailable { get; set; }
    public bool IsCognizable { get; set; }
    public bool IsCybercrime { get; set; }
    public string Category { get; set; } = string.Empty;
}

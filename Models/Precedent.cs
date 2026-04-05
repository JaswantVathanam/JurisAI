namespace AILegalAsst.Models;

public class Precedent
{
    public int Id { get; set; }
    public string CaseTitle { get; set; } = string.Empty;
    public string CaseCitation { get; set; } = string.Empty;
    public string Citation { get; set; } = string.Empty; // Alias for CaseCitation
    public string Court { get; set; } = string.Empty;
    public string CourtName { get; set; } = string.Empty; // Alias for Court
    public DateTime JudgementDate { get; set; }
    public string Judge { get; set; } = string.Empty;
    public string JudgeName { get; set; } = string.Empty; // Alias for Judge
    
    public string Summary { get; set; } = string.Empty;
    public string Facts { get; set; } = string.Empty;
    public string LegalIssues { get; set; } = string.Empty;
    public string Judgement { get; set; } = string.Empty;
    public string JudgementText { get; set; } = string.Empty; // Alias for Judgement
    public string Ratio { get; set; } = string.Empty;
    
    public List<string> ApplicableLaws { get; set; } = new();
    public List<string> Sections { get; set; } = new();
    public List<string> ApplicableSections { get; set; } = new(); // Alias for Sections
    public List<string> Keywords { get; set; } = new();
    public List<string> KeyPrinciples { get; set; } = new();
    
    public bool IsCybercrimeRelated { get; set; }
    public string? CybercrimeCategory { get; set; }
    
    public bool IsLandmarkCase { get; set; }
    
    public int CitationCount { get; set; }
    public List<string> CitedBy { get; set; } = new();
    
    public string? PDFPath { get; set; }
}

using System;

namespace AILegalAsst.Models
{
    /// <summary>
    /// Represents an AI-generated suggestion for investigation copilot
    /// </summary>
    public class CopilotSuggestion
    {
        public int Id { get; set; }
        
        /// <summary>
        /// Reference to the investigation session
        /// </summary>
        public int InvestigationSessionId { get; set; }
        public virtual InvestigationSession InvestigationSession { get; set; }
        
        /// <summary>
        /// Type of suggestion: NextStep, Warning, Evidence, Precedent, LegalAdvice, QuestionForSuspect, DocumentRequired
        /// </summary>
        public string Type { get; set; }
        
        /// <summary>
        /// The suggestion text/content from AI
        /// </summary>
        public string Content { get; set; }
        
        /// <summary>
        /// Priority level 1-5 (1=low, 5=critical)
        /// </summary>
        public int Priority { get; set; } = 3;
        
        /// <summary>
        /// Has the investigator accepted this suggestion?
        /// </summary>
        public bool IsAccepted { get; set; } = false;
        
        /// <summary>
        /// Has the investigator rejected this suggestion?
        /// </summary>
        public bool IsRejected { get; set; } = false;
        
        /// <summary>
        /// Reason if rejected
        /// </summary>
        public string RejectionReason { get; set; }
        
        /// <summary>
        /// Action item created from this suggestion (if accepted)
        /// </summary>
        public int? ActionItemId { get; set; }
        public virtual InvestigationAction ActionItem { get; set; }
        
        /// <summary>
        /// Confidence score from AI (0-100)
        /// </summary>
        public int ConfidenceScore { get; set; } = 75;
        
        /// <summary>
        /// Explanation why this suggestion is important
        /// </summary>
        public string Explanation { get; set; }
        
        /// <summary>
        /// Estimated effort required in hours
        /// </summary>
        public double EstimatedEffort { get; set; } = 2;
        
        /// <summary>
        /// Source of the suggestion: AzureAgent, SystemDefault
        /// </summary>
        public string Source { get; set; } = "SystemDefault";
        
        /// <summary>
        /// When suggestion was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// When suggestion was last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// When suggestion was accepted/rejected
        /// </summary>
        public DateTime? ActionTakenAt { get; set; }
    }
}

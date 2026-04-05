using System;
using System.Collections.Generic;

namespace AILegalAsst.Models
{
    /// <summary>
    /// Represents an investigation session for AI-powered copilot assistance
    /// </summary>
    public class InvestigationSession
    {
        public int Id { get; set; }
        
        /// <summary>
        /// Reference to the case being investigated
        /// </summary>
        public int CaseId { get; set; }
        public virtual Case Case { get; set; }
        
        /// <summary>
        /// Reference to the investigator/police officer
        /// </summary>
        public int InvestigatorId { get; set; }
        public virtual User Investigator { get; set; }
        
        /// <summary>
        /// Current investigation stage: Filing, Investigation, Evidence, Chargesheet
        /// </summary>
        public string CurrentStage { get; set; } = "Filing";
        
        /// <summary>
        /// Session status: Active, Paused, Completed
        /// </summary>
        public string Status { get; set; } = "Active";
        
        /// <summary>
        /// Number of actions completed
        /// </summary>
        public int CompletedActionsCount { get; set; } = 0;
        
        /// <summary>
        /// Total number of actions in this session
        /// </summary>
        public int TotalActionsCount { get; set; } = 0;
        
        /// <summary>
        /// Estimated completion date
        /// </summary>
        public DateTime? EstimatedCompletionDate { get; set; }
        
        /// <summary>
        /// Notes and observations from investigator
        /// </summary>
        public string Notes { get; set; }
        
        /// <summary>
        /// When the session started
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// When the session was last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// When the session was completed
        /// </summary>
        public DateTime? CompletedAt { get; set; }
        
        /// <summary>
        /// Related suggestions from the AI copilot
        /// </summary>
        public virtual ICollection<CopilotSuggestion> Suggestions { get; set; } = new List<CopilotSuggestion>();
        
        /// <summary>
        /// Action items assigned during this session
        /// </summary>
        public virtual ICollection<InvestigationAction> Actions { get; set; } = new List<InvestigationAction>();
        
        /// <summary>
        /// Calculate progress percentage
        /// </summary>
        public decimal GetProgressPercentage()
        {
            if (TotalActionsCount == 0) return 0;
            return (decimal)CompletedActionsCount / TotalActionsCount * 100;
        }
    }
}

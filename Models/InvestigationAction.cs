using System;

namespace AILegalAsst.Models
{
    /// <summary>
    /// Represents an investigation action item from copilot suggestions
    /// </summary>
    public class InvestigationAction
    {
        public int Id { get; set; }
        
        /// <summary>
        /// Reference to the investigation session
        /// </summary>
        public int InvestigationSessionId { get; set; }
        public virtual InvestigationSession InvestigationSession { get; set; }
        
        /// <summary>
        /// Type of action: BankFreeze, Evidence, Interview, DocumentRequest, TelephoneRequest, SocialMediaTakedown, 
        /// InspectionSearch, RelationshipMapping, TimelineConstruction, LegalNotice
        /// </summary>
        public string Type { get; set; }
        
        /// <summary>
        /// Description of what needs to be done
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// Status: Pending, InProgress, Completed, Blocked, Cancelled
        /// </summary>
        public string Status { get; set; } = "Pending";
        
        /// <summary>
        /// When this action was first created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Due date for completion
        /// </summary>
        public DateTime DueDate { get; set; }
        
        /// <summary>
        /// When action started
        /// </summary>
        public DateTime? StartedAt { get; set; }
        
        /// <summary>
        /// When action was completed
        /// </summary>
        public DateTime? CompletedAt { get; set; }
        
        /// <summary>
        /// Who is assigned to this action (investigator or team member)
        /// </summary>
        public int? AssignedToUserId { get; set; }
        public virtual User AssignedToUser { get; set; }
        
        /// <summary>
        /// Priority level 1-5 (1=low, 5=critical)
        /// </summary>
        public int Priority { get; set; } = 3;
        
        /// <summary>
        /// Estimated hours to complete
        /// </summary>
        public decimal EstimatedHours { get; set; } = 2;
        
        /// <summary>
        /// Notes/comments on progress
        /// </summary>
        public string Notes { get; set; }
        
        /// <summary>
        /// Blocking reason if status is Blocked
        /// </summary>
        public string BlockingReason { get; set; }
        
        /// <summary>
        /// Result/output from completing this action
        /// </summary>
        public string Result { get; set; }
        
        /// <summary>
        /// Is this action dependent on another action?
        /// </summary>
        public int? DependsOnActionId { get; set; }
        public virtual InvestigationAction DependsOnAction { get; set; }
        
        /// <summary>
        /// When last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Get status badge CSS class
        /// </summary>
        public string GetStatusBadgeClass()
        {
            return Status switch
            {
                "Completed" => "badge-success",
                "InProgress" => "badge-primary",
                "Pending" => "badge-warning",
                "Blocked" => "badge-danger",
                "Cancelled" => "badge-secondary",
                _ => "badge-secondary"
            };
        }
        
        /// <summary>
        /// Get progress percentage based on status
        /// </summary>
        public decimal GetProgressPercentage()
        {
            return Status switch
            {
                "Completed" => 100,
                "InProgress" => 50,
                "Pending" => 0,
                _ => 0
            };
        }
        
        /// <summary>
        /// Check if action is overdue
        /// </summary>
        public bool IsOverdue()
        {
            return Status != "Completed" && DueDate < DateTime.UtcNow;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AILegalAsst.Models;
using Microsoft.Extensions.Logging;

namespace AILegalAsst.Services
{
    /// <summary>
    /// Service for managing investigation workflow progression and stage management
    /// </summary>
    public class InvestigationWorkflowService
    {
        private static List<InvestigationSession> _sessions = new();
        private static List<InvestigationAction> _actions = new();
        
        private readonly ILogger<InvestigationWorkflowService> _logger;
        private readonly AzureAgentService _agentService;
        
        // Define investigation stages and their sequence
        private readonly List<string> _investigationStages = new List<string>
        {
            "Filing",
            "Investigation", 
            "Evidence",
            "Chargesheet"
        };
        
        public InvestigationWorkflowService(
            ILogger<InvestigationWorkflowService> logger,
            AzureAgentService agentService)
        {
            _logger = logger;
            _agentService = agentService;
        }
        
        /// <summary>
        /// Get current workflow progress for a session
        /// </summary>
        public async Task<WorkflowProgress> GetProgressAsync(int sessionId)
        {
            try
            {
                var sessions = _sessions ?? new List<InvestigationSession>();
                var actions = _actions ?? new List<InvestigationAction>();
                
                var session = sessions.FirstOrDefault(s => s.Id == sessionId);
                
                if (session == null)
                {
                    return null;
                }
                
                var sessionActions = actions.Where(a => a.InvestigationSessionId == sessionId).ToList();
                var completedCount = sessionActions.Count(a => a.Status == "Completed");
                var totalCount = sessionActions.Count;
                var progressPercent = totalCount > 0 ? (completedCount / (decimal)totalCount) * 100 : 0;
                
                return await Task.FromResult(new WorkflowProgress
                {
                    SessionId = sessionId,
                    CurrentStage = session.CurrentStage,
                    ProgressPercentage = Math.Round(progressPercent, 2),
                    CompletedActions = completedCount,
                    TotalActions = totalCount,
                    EstimatedCompletionDate = session.EstimatedCompletionDate,
                    Status = session.Status
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting progress: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Progress to next stage if ready
        /// </summary>
        public async Task<bool> ProgressStageAsync(int sessionId, string nextStage)
        {
            try
            {
                var sessions = _sessions ?? new List<InvestigationSession>();
                var actions = _actions ?? new List<InvestigationAction>();
                
                var session = sessions.FirstOrDefault(s => s.Id == sessionId);
                
                if (session == null)
                {
                    _logger.LogWarning($"Session {sessionId} not found");
                    return false;
                }
                
                // Validate stage transition
                if (!IsValidTransition(session.CurrentStage, nextStage))
                {
                    _logger.LogWarning($"Invalid stage transition from {session.CurrentStage} to {nextStage}");
                    return false;
                }
                
                // Check if all required actions are completed
                var sessionActions = actions.Where(a => a.InvestigationSessionId == session.Id).ToList();
                var incompletedActions = sessionActions.Where(a => a.Status != "Completed" && a.Status != "Cancelled").ToList();
                if (incompletedActions.Any())
                {
                    _logger.LogWarning($"Cannot progress stage - {incompletedActions.Count} incomplete actions remain");
                    return false;
                }
                
                // Progress stage
                session.CurrentStage = nextStage;
                session.UpdatedAt = DateTime.UtcNow;
                
                _logger.LogInformation($"Session {sessionId} progressed to stage {nextStage}");
                
                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error progressing stage: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Check if stage transition is valid
        /// </summary>
        private bool IsValidTransition(string currentStage, string nextStage)
        {
            var currentIndex = _investigationStages.IndexOf(currentStage);
            var nextIndex = _investigationStages.IndexOf(nextStage);
            
            // Can only progress to next stage in sequence
            return nextIndex == currentIndex + 1;
        }
        
        /// <summary>
        /// Get estimated completion date based on pending actions
        /// </summary>
        public async Task<DateTime> GetEstimatedCompletionDateAsync(int sessionId)
        {
            try
            {
                var sessions = _sessions ?? new List<InvestigationSession>();
                var actions = _actions ?? new List<InvestigationAction>();
                
                var session = sessions.FirstOrDefault(s => s.Id == sessionId);
                
                if (session == null)
                {
                    return DateTime.UtcNow.AddDays(30);
                }
                
                var sessionActions = actions.Where(a => a.InvestigationSessionId == sessionId).ToList();
                var pendingActions = sessionActions.Where(a => a.Status == "Pending" || a.Status == "InProgress").ToList();
                
                if (!pendingActions.Any())
                {
                    return DateTime.UtcNow;
                }
                
                // Calculate based on latest due date
                var latestDueDate = pendingActions.Max(a => a.DueDate);

                // Try AI-powered intelligent time estimation
                if (_agentService.IsReady)
                {
                    try
                    {
                        var completedCount = sessionActions.Count(a => a.Status == "Completed");
                        var prompt = $"Estimate realistic investigation completion time:\n" +
                            $"Current stage: {session.CurrentStage}\n" +
                            $"Completed actions: {completedCount}/{sessionActions.Count}\n" +
                            $"Pending actions: {pendingActions.Count}\n" +
                            $"Latest due date: {latestDueDate:dd MMM yyyy}\n" +
                            "Based on typical Indian police investigation timelines, how many additional days beyond the latest due date should be estimated? " +
                            "Reply with ONLY a number (days to add).";
                        var response = await _agentService.SendMessageAsync(prompt, 
                            "You are an Indian police investigation workflow expert. Reply with only a number.");
                        if (response.Success && int.TryParse(response.Message?.Trim(), out var extraDays) && extraDays >= 0 && extraDays <= 365)
                        {
                            return latestDueDate.AddDays(extraDays);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"AI estimation failed: {ex.Message}");
                    }
                }

                return latestDueDate;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error calculating estimated completion: {ex.Message}");
                return DateTime.UtcNow.AddDays(30);
            }
        }
        
        /// <summary>
        /// Get available next stages from current stage
        /// </summary>
        public List<string> GetAvailableNextStages(string currentStage)
        {
            var currentIndex = _investigationStages.IndexOf(currentStage);
            
            if (currentIndex < 0 || currentIndex >= _investigationStages.Count - 1)
            {
                return new List<string>();
            }
            
            return new List<string> { _investigationStages[currentIndex + 1] };
        }
        
        /// <summary>
        /// Check if session can progress to next stage
        /// </summary>
        public async Task<(bool CanProgress, string Reason)> CanProgressStageAsync(int sessionId)
        {
            try
            {
                var sessions = _sessions ?? new List<InvestigationSession>();
                var actions = _actions ?? new List<InvestigationAction>();
                
                var session = sessions.FirstOrDefault(s => s.Id == sessionId);
                
                if (session == null)
                {
                    return (false, "Session not found");
                }
                
                // Check if current stage is final stage
                if (session.CurrentStage == _investigationStages[_investigationStages.Count - 1])
                {
                    return await Task.FromResult((false, "Already at final stage"));
                }
                
                // Check for blocked actions
                var sessionActions = actions.Where(a => a.InvestigationSessionId == sessionId).ToList();
                var blockedActions = sessionActions.Where(a => a.Status == "Blocked").ToList();
                if (blockedActions.Any())
                {
                    return await Task.FromResult((false, $"{blockedActions.Count} actions are blocked"));
                }
                
                // Check for incomplete actions
                var incompleteActions = sessionActions
                    .Where(a => a.Status != "Completed" && a.Status != "Cancelled")
                    .ToList();
                
                if (incompleteActions.Any())
                {
                    return await Task.FromResult((false, $"{incompleteActions.Count} actions are incomplete"));
                }
                
                return await Task.FromResult((true, "Ready to progress"));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error checking stage progression: {ex.Message}");
                return (false, "Error checking progression status");
            }
        }
    }
    
    /// <summary>
    /// Data transfer object for workflow progress information
    /// </summary>
    public class WorkflowProgress
    {
        public int SessionId { get; set; }
        public string CurrentStage { get; set; }
        public decimal ProgressPercentage { get; set; }
        public int CompletedActions { get; set; }
        public int TotalActions { get; set; }
        public DateTime? EstimatedCompletionDate { get; set; }
        public string Status { get; set; }
    }
}

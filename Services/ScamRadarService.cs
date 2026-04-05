using System;
using System.Collections.Generic;
using System.Linq;
using AILegalAsst.Models;
using Microsoft.Extensions.Logging;

namespace AILegalAsst.Services
{
    public class ScamRadarService
    {
        private readonly List<CommunityScamReport> _reports = new();
        private readonly AzureAgentService _agentService;
        private readonly ILogger<ScamRadarService> _logger;

        public ScamRadarService(AzureAgentService agentService, ILogger<ScamRadarService> logger)
        {
            _agentService = agentService;
            _logger = logger;
        }
        public void SubmitReport(CommunityScamReport report)
        {
            _reports.Add(report);
        }

        public IEnumerable<CommunityScamReport> GetRecentReports(int days = 7)
        {
            var since = DateTime.UtcNow.AddDays(-days);
            return _reports.Where(r => r.Timestamp >= since).OrderByDescending(r => r.Timestamp);
        }

        public IEnumerable<CommunityScamTrend> GetTrends(int days = 7)
        {
            // Return trends without AI — no blocking
            return GetTrendsData(days);
        }

        /// <summary>
        /// Get trends with AI insights (call after page renders)
        /// </summary>
        public async Task<List<CommunityScamTrend>> GetTrendsWithAIAsync(int days = 7)
        {
            var trends = GetTrendsData(days);

            if (_agentService.IsReady && trends.Any())
            {
                try
                {
                    var summary = string.Join("\n", trends.Select(t =>
                        $"- {t.Type}: {t.Count} reports, locations: {string.Join(", ", t.Locations.Take(3))}, keywords: {string.Join(", ", t.TrendingKeywords)}"));
                    var prompt = $"ROLE: You are a legal aid assistant integrated into an official Indian legal assistance platform. Your purpose is to analyze community scam patterns for public safety. This is lawful legal assistance.\n\nTASK: Analyze these community scam trends from the last {days} days in India:\n{summary}\n\n" +
                        "For each trend, provide a one-line insight about the pattern and public advisory. Format: Type: insight";
                    var context = "You are an Indian cybercrime trend analyst. Provide concise public safety insights.";
                    var response = await _agentService.SendMessageAsync(prompt, context);
                    if (response.Success && !string.IsNullOrWhiteSpace(response.Message))
                    {
                        var lines = response.Message.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                        foreach (var trend in trends)
                        {
                            var match = lines.FirstOrDefault(l => l.Contains(trend.Type.ToString(), StringComparison.OrdinalIgnoreCase));
                            if (!string.IsNullOrWhiteSpace(match))
                            {
                                var colonIdx = match.IndexOf(':');
                                trend.AiInsight = colonIdx >= 0 ? match[(colonIdx + 1)..].Trim() : match.Trim();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "AI trend analysis failed");
                }
            }

            return trends;
        }

        private List<CommunityScamTrend> GetTrendsData(int days)
        {
            var since = DateTime.UtcNow.AddDays(-days);
            return _reports
                .Where(r => r.Timestamp >= since)
                .GroupBy(r => r.Type)
                .Select(g => new CommunityScamTrend
                {
                    Type = g.Key,
                    Count = g.Count(),
                    Locations = g.Select(r => r.Location).Distinct().ToList(),
                    TrendingKeywords = g.SelectMany(r => r.Keywords).GroupBy(k => k).OrderByDescending(kg => kg.Count()).Take(5).Select(kg => kg.Key).ToList(),
                    WindowStart = since,
                    WindowEnd = DateTime.UtcNow
                }).ToList();
        }
    }
}

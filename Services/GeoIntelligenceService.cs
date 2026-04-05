using AILegalAsst.Models;
using Microsoft.Extensions.Logging;

namespace AILegalAsst.Services;

/// <summary>
/// GeoIntelligence service for advanced location analytics
/// Provides suspect movement analysis, location clustering, pattern detection, crime hotspot analysis
/// </summary>
public class GeoIntelligenceService
{
    private readonly AzureMapsService _mapsService;
    private readonly LocationTrackingService _locationService;
    private readonly ILogger<GeoIntelligenceService> _logger;
    private readonly AzureAgentService _agentService;

    public GeoIntelligenceService(
        AzureMapsService mapsService,
        LocationTrackingService locationService,
        ILogger<GeoIntelligenceService> logger,
        AzureAgentService agentService)
    {
        _mapsService = mapsService;
        _locationService = locationService;
        _logger = logger;
        _agentService = agentService;
    }

    /// <summary>
    /// Analyze suspect movement patterns from CDR tower locations
    /// </summary>
    public async Task<MovementPattern> AnalyzeSuspectMovementAsync(
        string caseId,
        string suspectName,
        string phoneNumber,
        List<CellTowerLocation> towerLocations)
    {
        _logger.LogInformation(
            "[GeoIntel] Analyzing movement pattern for {Suspect} ({Phone}) - {Count} tower locations",
            suspectName, phoneNumber, towerLocations.Count);

        var locationHistory = towerLocations
            .OrderBy(t => t.FirstCall)
            .Select(t => t.Location)
            .ToList();

        // Calculate total distance traveled
        double totalDistance = 0;
        for (int i = 0; i < locationHistory.Count - 1; i++)
        {
            totalDistance += _locationService.GetDistanceBetween(
                locationHistory[i], 
                locationHistory[i + 1]);
        }

        // Identify frequent locations (visited multiple times)
        var locationGroups = locationHistory
            .GroupBy(l => $"{l.Latitude:F4},{l.Longitude:F4}")
            .Where(g => g.Count() > 1)
            .Select(g => g.First())
            .ToList();

        // Identify home location (most frequent nighttime location 10PM-6AM)
        var nighttimeLocations = towerLocations
            .Where(t => t.FirstCall.Hour >= 22 || t.FirstCall.Hour <= 6)
            .GroupBy(t => t.TowerId)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault();

        LocationData? homeLocation = nighttimeLocations != null
            ? nighttimeLocations.First().Location
            : null;

        // Identify work location (most frequent daytime location 9AM-5PM)
        var daytimeLocations = towerLocations
            .Where(t => t.FirstCall.Hour >= 9 && t.FirstCall.Hour <= 17)
            .GroupBy(t => t.TowerId)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault();

        LocationData? workLocation = daytimeLocations != null
            ? daytimeLocations.First().Location
            : null;

        // Detect anomalies
        var anomalies = new List<string>();

        // Check for unusual speed (>150 km/h indicates tower switching issues or vehicle travel)
        for (int i = 0; i < locationHistory.Count - 1; i++)
        {
            var distance = _locationService.GetDistanceBetween(locationHistory[i], locationHistory[i + 1]);
            var timeDiff = (towerLocations[i + 1].FirstCall - towerLocations[i].FirstCall).TotalHours;
            if (timeDiff > 0)
            {
                var speed = distance / timeDiff;
                if (speed > 150)
                {
                    anomalies.Add($"High-speed movement detected: {speed:F0} km/h on {towerLocations[i].FirstCall:MMM dd}");
                }
            }
        }

        // Check for late-night activity
        var lateNightCalls = towerLocations.Count(t => t.FirstCall.Hour >= 23 || t.FirstCall.Hour <= 4);
        if (lateNightCalls > towerLocations.Count * 0.3)
        {
            anomalies.Add($"High late-night activity: {lateNightCalls} calls between 11PM-4AM");
        }

        // Check for location clustering (staying in one area for extended period)
        var stationaryPeriods = DetectStationaryPeriods(towerLocations);
        if (stationaryPeriods.Count > 0)
        {
            anomalies.Add($"Identified {stationaryPeriods.Count} stationary periods (possible hideouts)");
        }

        var pattern = new MovementPattern
        {
            PatternId = Guid.NewGuid().ToString(),
            SuspectName = suspectName,
            PhoneNumber = phoneNumber,
            CaseId = caseId,
            LocationHistory = locationHistory,
            FrequentLocations = locationGroups,
            HomeLocation = homeLocation,
            WorkLocation = workLocation,
            TotalDistanceKm = totalDistance,
            StartDate = towerLocations.Min(t => t.FirstCall),
            EndDate = towerLocations.Max(t => t.LastCall),
            Anomalies = anomalies
        };

        // Try AI-powered behavioral analysis
        if (_agentService.IsReady && (anomalies.Any() || totalDistance > 500))
        {
            try
            {
                var prompt = $"Analyze this suspect movement pattern for Indian law enforcement:\n" +
                    $"Suspect: {suspectName}, Phone: {phoneNumber}\n" +
                    $"Period: {pattern.StartDate:dd MMM} to {pattern.EndDate:dd MMM yyyy}\n" +
                    $"Total distance: {totalDistance:F1} km across {towerLocations.Count} tower locations\n" +
                    $"Frequent locations: {locationGroups.Count}\n" +
                    $"Home location identified: {(homeLocation != null ? "Yes" : "No")}\n" +
                    $"Work location identified: {(workLocation != null ? "Yes" : "No")}\n" +
                    $"Anomalies detected: {string.Join("; ", anomalies)}\n\n" +
                    "Provide a brief behavioral analysis including: movement pattern type (routine/evasive/suspicious), " +
                    "possible interpretations of anomalies, and investigation recommendations.";
                var context = "You are an Indian police geo-intelligence analyst specializing in CDR-based suspect tracking and movement pattern analysis.";
                var response = await _agentService.SendMessageAsync(prompt, context);
                if (response.Success && !string.IsNullOrWhiteSpace(response.Message))
                {
                    anomalies.Add($"🤖 AI Analysis: {response.Message.Trim()}");
                    pattern.Anomalies = anomalies;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AI movement analysis failed");
            }
        }

        _logger.LogInformation(
            "[GeoIntel] Movement analysis complete: {Distance}km traveled, {Anomalies} anomalies detected",
            totalDistance, anomalies.Count);

        return pattern;
    }

    /// <summary>
    /// Analyze crime hotspots from case locations
    /// </summary>
    public async Task<List<CrimeHotspot>> AnalyzeCrimeHotspotsAsync(
        List<LocationData> caseLocations,
        double radiusKm = 1.0,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        _logger.LogInformation(
            "[GeoIntel] Analyzing crime hotspots from {Count} case locations",
            caseLocations.Count);

        var hotspots = new List<CrimeHotspot>();

        // Filter by date range if provided
        var filteredLocations = caseLocations;
        if (startDate.HasValue)
            filteredLocations = filteredLocations.Where(l => l.Timestamp >= startDate.Value).ToList();
        if (endDate.HasValue)
            filteredLocations = filteredLocations.Where(l => l.Timestamp <= endDate.Value).ToList();

        // Cluster locations by proximity
        var clusters = ClusterLocations(filteredLocations, radiusKm);

        foreach (var cluster in clusters)
        {
            var center = CalculateCentroid(cluster);
            var crimeTypes = cluster
                .GroupBy(l => l.LocationType)
                .ToDictionary(g => g.Key, g => g.Count());

            // Calculate density (cases per square km)
            var area = Math.PI * radiusKm * radiusKm;
            var density = cluster.Count / area;

            // Determine risk level
            string riskLevel = density switch
            {
                > 10 => "Critical",
                > 5 => "High",
                > 2 => "Medium",
                _ => "Low"
            };

            // Get area name from first location
            var areaName = cluster.FirstOrDefault()?.City ?? "Unknown Area";

            // Calculate trend (if we have historical data)
            var trend = "Stable"; // Placeholder - would need historical comparison
            var trendPercentage = 0.0;

            var hotspot = new CrimeHotspot
            {
                HotspotId = Guid.NewGuid().ToString(),
                AreaName = areaName,
                Center = center,
                RadiusKm = radiusKm,
                CaseCount = cluster.Count,
                CrimeTypes = crimeTypes,
                DensityScore = density,
                RiskLevel = riskLevel,
                StartDate = startDate ?? cluster.Min(l => l.Timestamp),
                EndDate = endDate ?? cluster.Max(l => l.Timestamp),
                Trend = trend,
                TrendPercentage = trendPercentage,
                JurisdictionStation = await DetermineJurisdictionAsync(center)
            };

            hotspots.Add(hotspot);
        }

        _logger.LogInformation(
            "[GeoIntel] Identified {Count} crime hotspots",
            hotspots.Count);

        return hotspots;
    }

    /// <summary>
    /// Calculate route for multi-stop investigation
    /// </summary>
    public async Task<GeoRoute?> PlanInvestigationRouteAsync(
        string caseId,
        LocationData startPoint,
        List<LocationData> investigationSites,
        LocationData endPoint)
    {
        _logger.LogInformation(
            "[GeoIntel] Planning investigation route: {Start} → {Sites} sites → {End}",
            startPoint.Address, investigationSites.Count, endPoint.Address);

        try
        {
            // Optimize waypoint order for shortest route (simple nearest neighbor)
            var orderedSites = OptimizeWaypoints(startPoint, investigationSites, endPoint);

            var route = await _locationService.CalculateInvestigationRouteAsync(
                startPoint,
                endPoint,
                orderedSites,
                caseId);

            if (route != null)
            {
                route.RouteName = $"Investigation Route - {investigationSites.Count} sites";
                _logger.LogInformation(
                    "[GeoIntel] Route planned: {Distance}km, {Time}min",
                    route.TotalDistanceKm, route.EstimatedTimeMinutes);
            }

            return route;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GeoIntel] Error planning investigation route");
            return null;
        }
    }

    /// <summary>
    /// Find evidence locations near a suspect's movement pattern
    /// </summary>
    public async Task<List<LocationData>> CorrelateEvidenceWithMovementAsync(
        MovementPattern suspectMovement,
        List<LocationData> evidenceLocations,
        double proximityThresholdKm = 1.0)
    {
        _logger.LogInformation(
            "[GeoIntel] Correlating {Evidence} evidence locations with suspect movement",
            evidenceLocations.Count);

        var correlatedEvidence = new List<LocationData>();

        foreach (var evidenceLoc in evidenceLocations)
        {
            // Check if evidence location is near any of suspect's locations
            foreach (var suspectLoc in suspectMovement.LocationHistory)
            {
                var distance = _locationService.GetDistanceBetween(evidenceLoc, suspectLoc);
                if (distance <= proximityThresholdKm)
                {
                    evidenceLoc.Notes = $"Within {distance:F2}km of suspect location on {suspectLoc.Timestamp:MMM dd}";
                    correlatedEvidence.Add(evidenceLoc);
                    break;
                }
            }
        }

        _logger.LogInformation(
            "[GeoIntel] Found {Count} evidence locations correlated with suspect movement",
            correlatedEvidence.Count);

        return correlatedEvidence;
    }

    #region Helper Methods

    private List<LocationData> DetectStationaryPeriods(List<CellTowerLocation> towers)
    {
        var stationaryLocations = new List<LocationData>();
        var grouped = towers.GroupBy(t => t.TowerId);

        foreach (var group in grouped)
        {
            var duration = (group.Max(t => t.LastCall) - group.Min(t => t.FirstCall)).TotalHours;
            if (duration > 4) // Stayed at same tower for > 4 hours
            {
                stationaryLocations.Add(group.First().Location);
            }
        }

        return stationaryLocations;
    }

    private List<List<LocationData>> ClusterLocations(List<LocationData> locations, double radiusKm)
    {
        var clusters = new List<List<LocationData>>();
        var processed = new HashSet<LocationData>();

        foreach (var location in locations)
        {
            if (processed.Contains(location))
                continue;

            var cluster = new List<LocationData> { location };
            processed.Add(location);

            foreach (var other in locations)
            {
                if (processed.Contains(other))
                    continue;

                var distance = _locationService.GetDistanceBetween(location, other);
                if (distance <= radiusKm)
                {
                    cluster.Add(other);
                    processed.Add(other);
                }
            }

            if (cluster.Count > 1) // Only keep clusters with multiple locations
                clusters.Add(cluster);
        }

        return clusters;
    }

    private LocationData CalculateCentroid(List<LocationData> locations)
    {
        var avgLat = locations.Average(l => l.Latitude);
        var avgLon = locations.Average(l => l.Longitude);

        return new LocationData
        {
            Latitude = avgLat,
            Longitude = avgLon,
            LocationType = "Centroid"
        };
    }

    private async Task<string> DetermineJurisdictionAsync(LocationData location)
    {
        try
        {
            var nearbyPolice = await _mapsService.SearchNearbyAsync(
                location.Latitude,
                location.Longitude,
                "police station",
                5000,
                1);

            return nearbyPolice.FirstOrDefault()?.Name ?? "Unknown Jurisdiction";
        }
        catch
        {
            return "Unknown Jurisdiction";
        }
    }

    private List<LocationData> OptimizeWaypoints(
        LocationData start,
        List<LocationData> waypoints,
        LocationData end)
    {
        // Simple nearest neighbor optimization
        var optimized = new List<LocationData>();
        var remaining = new List<LocationData>(waypoints);
        var current = start;

        while (remaining.Any())
        {
            var nearest = remaining
                .OrderBy(w => _locationService.GetDistanceBetween(current, w))
                .First();

            optimized.Add(nearest);
            remaining.Remove(nearest);
            current = nearest;
        }

        return optimized;
    }

    #endregion
}

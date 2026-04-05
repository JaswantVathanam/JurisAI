using AILegalAsst.Models;

namespace AILegalAsst.Services;

/// <summary>
/// Centralized location tracking service for SOS, Evidence, CDR, and Investigation activities
/// Provides location history, geofencing, and proximity analysis
/// </summary>
public class LocationTrackingService
{
    private readonly AzureMapsService _mapsService;
    private readonly ILogger<LocationTrackingService> _logger;

    public LocationTrackingService(
        AzureMapsService mapsService,
        ILogger<LocationTrackingService> logger)
    {
        _mapsService = mapsService;
        _logger = logger;
    }

    /// <summary>
    /// Track a location event (SOS, Evidence Collection, Investigation Activity)
    /// </summary>
    public async Task<LocationData> TrackLocationAsync(
        double latitude,
        double longitude,
        string locationType,
        string relatedEntityId,
        string recordedBy,
        string notes = "",
        double accuracyMeters = 0)
    {
        try
        {
            // Get address from coordinates
            var reverseGeocode = await _mapsService.ReverseGeocodeAsync(latitude, longitude);
            
            var location = new LocationData
            {
                LocationId = Guid.NewGuid().ToString(),
                Latitude = latitude,
                Longitude = longitude,
                Address = reverseGeocode?.Address ?? "Address lookup in progress",
                City = reverseGeocode?.City ?? "",
                State = reverseGeocode?.State ?? "",
                PostalCode = reverseGeocode?.PostalCode ?? "",
                Country = "India",
                LocationType = locationType,
                RelatedEntityId = relatedEntityId,
                Notes = notes,
                AccuracyMeters = accuracyMeters,
                RecordedBy = recordedBy,
                Timestamp = DateTime.UtcNow,
                IsVerified = accuracyMeters <= 50 // Auto-verify if accuracy is good
            };

            _logger.LogInformation(
                "[LocationTracking] Tracked {Type} location for {Entity}: {Lat}, {Lng} ({Address})",
                locationType, relatedEntityId, latitude, longitude, location.Address);

            return location;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LocationTracking] Error tracking location");
            throw;
        }
    }

    /// <summary>
    /// Get location history for an entity (Case, Evidence, SOS incident)
    /// </summary>
    public async Task<List<LocationData>> GetLocationHistoryAsync(
        string relatedEntityId,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        // In a real implementation, this would query a LocationHistory table
        // For now, returning empty list as placeholder
        _logger.LogInformation(
            "[LocationTracking] Getting location history for {Entity}", relatedEntityId);
        
        return new List<LocationData>();
    }

    /// <summary>
    /// Find nearby emergency services (police stations, hospitals, courts)
    /// </summary>
    public async Task<Dictionary<string, List<NearbyPlace>>> FindNearbyEmergencyServicesAsync(
        double latitude,
        double longitude,
        int radiusMeters = 5000)
    {
        var services = new Dictionary<string, List<NearbyPlace>>();

        try
        {
            // Search for different service types in parallel
            var policeTask = _mapsService.SearchNearbyAsync(latitude, longitude, "police station", radiusMeters, 5);
            var hospitalTask = _mapsService.SearchNearbyAsync(latitude, longitude, "hospital", radiusMeters, 5);
            var courtTask = _mapsService.SearchNearbyAsync(latitude, longitude, "court", radiusMeters, 3);

            await Task.WhenAll(policeTask, hospitalTask, courtTask);

            services["PoliceStations"] = await policeTask;
            services["Hospitals"] = await hospitalTask;
            services["Courts"] = await courtTask;

            _logger.LogInformation(
                "[LocationTracking] Found {Police} police stations, {Hospitals} hospitals, {Courts} courts near {Lat},{Lng}",
                services["PoliceStations"].Count,
                services["Hospitals"].Count,
                services["Courts"].Count,
                latitude, longitude);

            return services;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LocationTracking] Error finding nearby services");
            return services;
        }
    }

    /// <summary>
    /// Calculate route for multi-stop investigation
    /// </summary>
    public async Task<GeoRoute?> CalculateInvestigationRouteAsync(
        LocationData origin,
        LocationData destination,
        List<LocationData>? waypoints = null,
        string caseId = "")
    {
        try
        {
            var route = await _mapsService.CalculateRouteAsync(
                origin,
                destination,
                waypoints);

            if (route != null)
            {
                route.RouteName = $"Investigation Route - {origin.City} to {destination.City}";
                route.RelatedCaseId = caseId;
                
                _logger.LogInformation(
                    "[LocationTracking] Calculated route: {Distance}km, {Time}min",
                    route.TotalDistanceKm, route.EstimatedTimeMinutes);
            }

            return route;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LocationTracking] Error calculating route");
            return null;
        }
    }

    /// <summary>
    /// Check if a location is within a geofence (for evidence custody zones, restricted areas)
    /// </summary>
    public bool IsWithinGeofence(
        double latitude,
        double longitude,
        LocationData centerPoint,
        double radiusKm)
    {
        var distance = _mapsService.CalculateDistance(
            latitude, longitude,
            centerPoint.Latitude, centerPoint.Longitude);

        var isInside = distance <= radiusKm;
        
        _logger.LogInformation(
            "[LocationTracking] Geofence check: {Distance}km from center, {Status}",
            distance, isInside ? "INSIDE" : "OUTSIDE");

        return isInside;
    }

    /// <summary>
    /// Geocode an address (used when entering evidence location or case address)
    /// </summary>
    public async Task<LocationData?> GeocodeAddressAsync(string address)
    {
        try
        {
            var location = await _mapsService.GeocodeAddressAsync(address);
            
            if (location != null)
            {
                _logger.LogInformation(
                    "[LocationTracking] Geocoded '{Address}' to {Lat}, {Lng}",
                    address, location.Latitude, location.Longitude);
            }
            
            return location;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LocationTracking] Error geocoding address: {Address}", address);
            return null;
        }
    }

    /// <summary>
    /// Search for places (used in investigation to find locations mentioned in case)
    /// </summary>
    public async Task<List<LocationData>> SearchPlacesAsync(string query, int limit = 10)
    {
        try
        {
            var results = await _mapsService.SearchPlacesAsync(query, limit);
            
            _logger.LogInformation(
                "[LocationTracking] Search for '{Query}' returned {Count} results",
                query, results.Count);
            
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LocationTracking] Error searching places: {Query}", query);
            return new List<LocationData>();
        }
    }

    /// <summary>
    /// Get distance between two locations (for proximity analysis)
    /// </summary>
    public double GetDistanceBetween(LocationData location1, LocationData location2)
    {
        return _mapsService.CalculateDistance(
            location1.Latitude, location1.Longitude,
            location2.Latitude, location2.Longitude);
    }

    /// <summary>
    /// Validate coordinates (check if they're within India or valid range)
    /// </summary>
    public bool ValidateCoordinates(double latitude, double longitude)
    {
        // Basic validation
        if (latitude < -90 || latitude > 90 || longitude < -180 || longitude > 180)
            return false;

        // India bounds: roughly 8°N to 35°N, 68°E to 97°E
        var isInIndia = latitude >= 8.0 && latitude <= 35.0 &&
                        longitude >= 68.0 && longitude <= 97.0;

        if (!isInIndia)
        {
            _logger.LogWarning(
                "[LocationTracking] Coordinates {Lat}, {Lng} outside India bounds",
                latitude, longitude);
        }

        return true; // Allow coordinates outside India but log warning
    }
}

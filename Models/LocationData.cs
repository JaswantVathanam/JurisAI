namespace AILegalAsst.Models;

/// <summary>
/// Geographic location data model for storing coordinates and address information
/// Used across Emergency SOS, Evidence Custody, CDR Analysis, and Case Management
/// </summary>
public class LocationData
{
    public string LocationId { get; set; } = Guid.NewGuid().ToString();
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = "India";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Type of location: Evidence, Tower, SOS, Case, PoliceStation, Court
    /// </summary>
    public string LocationType { get; set; } = string.Empty;
    
    /// <summary>
    /// Reference to related entity (CaseId, EvidenceId, SOSId, etc.)
    /// </summary>
    public string RelatedEntityId { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional description or notes about this location
    /// </summary>
    public string Notes { get; set; } = string.Empty;
    
    /// <summary>
    /// Accuracy of GPS coordinates in meters
    /// </summary>
    public double AccuracyMeters { get; set; } = 0;

    /// <summary>
    /// GPS accuracy in meters (alias for AccuracyMeters, used by SOS tracking)
    /// </summary>
    public double Accuracy { get => AccuracyMeters; set => AccuracyMeters = value; }

    /// <summary>
    /// Speed in meters per second (from GPS tracking)
    /// </summary>
    public double? Speed { get; set; }
    
    /// <summary>
    /// User or system that recorded this location
    /// </summary>
    public string RecordedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether this location has been verified
    /// </summary>
    public bool IsVerified { get; set; } = false;
}

/// <summary>
/// Route planning and navigation model
/// </summary>
public class GeoRoute
{
    public string RouteId { get; set; } = Guid.NewGuid().ToString();
    public string RouteName { get; set; } = string.Empty;
    public LocationData Origin { get; set; } = new();
    public LocationData Destination { get; set; } = new();
    public List<LocationData> Waypoints { get; set; } = new();
    
    /// <summary>
    /// Total route distance in kilometers
    /// </summary>
    public double TotalDistanceKm { get; set; }
    
    /// <summary>
    /// Estimated travel time in minutes
    /// </summary>
    public int EstimatedTimeMinutes { get; set; }
    
    /// <summary>
    /// Turn-by-turn navigation instructions
    /// </summary>
    public string Instructions { get; set; } = string.Empty;
    
    /// <summary>
    /// GeoJSON route geometry for map rendering
    /// </summary>
    public string RouteGeometry { get; set; } = string.Empty;
    
    /// <summary>
    /// Route type: fastest, shortest, eco
    /// </summary>
    public string RouteType { get; set; } = "fastest";
    
    /// <summary>
    /// When this route was calculated
    /// </summary>
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Associated case or investigation ID
    /// </summary>
    public string RelatedCaseId { get; set; } = string.Empty;
}

/// <summary>
/// Crime density and hotspot analysis model
/// </summary>
public class CrimeHotspot
{
    public string HotspotId { get; set; } = Guid.NewGuid().ToString();
    public string AreaName { get; set; } = string.Empty;
    public LocationData Center { get; set; } = new();
    
    /// <summary>
    /// Radius of the hotspot area in kilometers
    /// </summary>
    public double RadiusKm { get; set; } = 1.0;
    
    /// <summary>
    /// Total number of cases in this area
    /// </summary>
    public int CaseCount { get; set; }
    
    /// <summary>
    /// Breakdown by crime type
    /// </summary>
    public Dictionary<string, int> CrimeTypes { get; set; } = new();
    
    /// <summary>
    /// Density score (cases per square km)
    /// </summary>
    public double DensityScore { get; set; }
    
    /// <summary>
    /// Risk level: Low, Medium, High, Critical
    /// </summary>
    public string RiskLevel { get; set; } = "Medium";
    
    /// <summary>
    /// Date range for this analysis
    /// </summary>
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    
    /// <summary>
    /// Trend: Increasing, Decreasing, Stable
    /// </summary>
    public string Trend { get; set; } = "Stable";
    
    /// <summary>
    /// Percentage change from previous period
    /// </summary>
    public double TrendPercentage { get; set; }
    
    /// <summary>
    /// Police station with jurisdiction
    /// </summary>
    public string JurisdictionStation { get; set; } = string.Empty;
}

/// <summary>
/// Cell tower location for CDR analysis
/// </summary>
public class CellTowerLocation
{
    public string TowerId { get; set; } = string.Empty;
    public string TowerName { get; set; } = string.Empty;
    public LocationData Location { get; set; } = new();
    
    /// <summary>
    /// Telecom operator: Airtel, Jio, Vi, BSNL
    /// </summary>
    public string Operator { get; set; } = string.Empty;
    
    /// <summary>
    /// Number of calls from this tower
    /// </summary>
    public int CallCount { get; set; }
    
    /// <summary>
    /// Total call duration in minutes
    /// </summary>
    public int TotalDurationMinutes { get; set; }
    
    /// <summary>
    /// First and last call timestamps
    /// </summary>
    public DateTime FirstCall { get; set; }
    public DateTime LastCall { get; set; }
    
    /// <summary>
    /// Related phone number or suspect
    /// </summary>
    public string RelatedPhoneNumber { get; set; } = string.Empty;
}

/// <summary>
/// Movement pattern for suspect tracking
/// </summary>
public class MovementPattern
{
    public string PatternId { get; set; } = Guid.NewGuid().ToString();
    public string SuspectName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public List<LocationData> LocationHistory { get; set; } = new();
    
    /// <summary>
    /// Frequently visited locations
    /// </summary>
    public List<LocationData> FrequentLocations { get; set; } = new();
    
    /// <summary>
    /// Home location (most frequent nighttime location)
    /// </summary>
    public LocationData? HomeLocation { get; set; }
    
    /// <summary>
    /// Work location (most frequent daytime location)
    /// </summary>
    public LocationData? WorkLocation { get; set; }
    
    /// <summary>
    /// Total distance traveled in km
    /// </summary>
    public double TotalDistanceKm { get; set; }
    
    /// <summary>
    /// Analysis date range
    /// </summary>
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    
    /// <summary>
    /// Detected anomalies in movement
    /// </summary>
    public List<string> Anomalies { get; set; } = new();
    
    /// <summary>
    /// Associated case ID
    /// </summary>
    public string CaseId { get; set; } = string.Empty;
}

/// <summary>
/// Nearby place search result
/// </summary>
public class NearbyPlace
{
    public string PlaceId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public LocationData Location { get; set; } = new();
    public double DistanceKm { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public bool IsOpen { get; set; }
}

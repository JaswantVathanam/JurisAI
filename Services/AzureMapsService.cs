using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;
using AILegalAsst.Models;
using System.Text.Json;

namespace AILegalAsst.Services;

/// <summary>
/// Azure Maps Service using REST API for geocoding, reverse geocoding, search, and routing
/// Provides geospatial intelligence for Emergency SOS, Evidence Custody, CDR Analysis
/// </summary>
public class AzureMapsService
{
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _configuration;
    private readonly string _subscriptionKey;
    private readonly int _cacheDuration;
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://atlas.microsoft.com";

    public AzureMapsService(IConfiguration configuration, IMemoryCache cache, IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _cache = cache;
        _httpClient = httpClientFactory.CreateClient();
        
        _subscriptionKey = _configuration["AzureMaps:SubscriptionKey"] ?? "";
        _cacheDuration = int.Parse(_configuration["AzureMaps:CacheDurationMinutes"] ?? "120");

        if (string.IsNullOrEmpty(_subscriptionKey))
        {
            throw new InvalidOperationException("Azure Maps Subscription Key not configured");
        }
    }

    /// <summary>
    /// Geocode an address to get latitude/longitude coordinates using REST API
    /// </summary>
    public async Task<LocationData?> GeocodeAddressAsync(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return null;

        var cacheKey = $"geocode-{address.ToLower().Trim()}";
        if (_cache.TryGetValue<LocationData>(cacheKey, out var cachedLocation))
        {
            Console.WriteLine($"[AzureMaps] Geocode cache hit for: {address}");
            return cachedLocation;
        }

        try
        {
            var url = $"{BaseUrl}/search/address/json?api-version=1.0&subscription-key={_subscriptionKey}&query={Uri.EscapeDataString(address)}&countrySet=IN&limit=1";
            
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            var json = JsonSerializer.Deserialize<JsonElement>(content);
            
            if (json.TryGetProperty("results", out var results) && results.GetArrayLength() > 0)
            {
                var result = results[0];
                var position = result.GetProperty("position");
                
                var location = new LocationData
                {
                    Latitude = position.GetProperty("lat").GetDouble(),
                    Longitude = position.GetProperty("lon").GetDouble(),
                    Address = result.GetProperty("address").TryGetProperty("freeformAddress", out var addr) 
                        ? addr.GetString() ?? address : address,
                    City = result.GetProperty("address").TryGetProperty("municipality", out var city) 
                        ? city.GetString() ?? "" : "",
                    State = result.GetProperty("address").TryGetProperty("countrySubdivision", out var state) 
                        ? state.GetString() ?? "" : "",
                    PostalCode = result.GetProperty("address").TryGetProperty("postalCode", out var postal) 
                        ? postal.GetString() ?? "" : "",
                    Country = "India",
                    LocationType = "Geocoded",
                    Timestamp = DateTime.UtcNow,
                    IsVerified = true
                };

                _cache.Set(cacheKey, location, TimeSpan.FromMinutes(_cacheDuration));
                Console.WriteLine($"[AzureMaps] Geocoded: {address} → {location.Latitude}, {location.Longitude}");
                return location;
            }

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AzureMaps] Geocoding error: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Reverse geocode coordinates to get address using REST API
    /// </summary>
    public async Task<LocationData?> ReverseGeocodeAsync(double latitude, double longitude)
    {
        var cacheKey = $"reverse-{latitude:F6}-{longitude:F6}";
        if (_cache.TryGetValue<LocationData>(cacheKey, out var cachedLocation))
        {
            Console.WriteLine($"[AzureMaps] Reverse geocode cache hit for: {latitude}, {longitude}");
            return cachedLocation;
        }

        try
        {
            var url = $"{BaseUrl}/search/address/reverse/json?api-version=1.0&subscription-key={_subscriptionKey}&query={latitude},{longitude}";
            
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            var json = JsonSerializer.Deserialize<JsonElement>(content);
            
            if (json.TryGetProperty("addresses", out var addresses) && addresses.GetArrayLength() > 0)
            {
                var result = addresses[0];
                var address = result.GetProperty("address");
                
                var location = new LocationData
                {
                    Latitude = latitude,
                    Longitude = longitude,
                    Address = address.TryGetProperty("freeformAddress", out var addr) ? addr.GetString() ?? "" : "",
                    City = address.TryGetProperty("municipality", out var city) ? city.GetString() ?? "" : "",
                    State = address.TryGetProperty("countrySubdivision", out var state) ? state.GetString() ?? "" : "",
                    PostalCode = address.TryGetProperty("postalCode", out var postal) ? postal.GetString() ?? "" : "",
                    Country = "India",
                    LocationType = "ReverseGeocoded",
                    Timestamp = DateTime.UtcNow,
                    IsVerified = true
                };

                _cache.Set(cacheKey, location, TimeSpan.FromMinutes(_cacheDuration));
                Console.WriteLine($"[AzureMaps] Reverse geocoded: {latitude}, {longitude} → {location.Address}");
                return location;
            }

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AzureMaps] Reverse geocoding error: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Search for nearby places using REST API
    /// </summary>
    public async Task<List<NearbyPlace>> SearchNearbyAsync(
        double latitude, 
        double longitude, 
        string category, 
        int radiusMeters = 5000, 
        int limit = 10)
    {
        var cacheKey = $"nearby-{latitude:F6}-{longitude:F6}-{category}-{radiusMeters}";
        if (_cache.TryGetValue<List<NearbyPlace>>(cacheKey, out var cachedPlaces))
        {
            Console.WriteLine($"[AzureMaps] Nearby search cache hit for: {category}");
            return cachedPlaces!;
        }

        try
        {
            var url = $"{BaseUrl}/search/nearby/json?api-version=1.0&subscription-key={_subscriptionKey}&lat={latitude}&lon={longitude}&radius={radiusMeters}&limit={limit}&countrySet=IN";
            if (!string.IsNullOrEmpty(category))
            {
                url += $"&query={Uri.EscapeDataString(category)}";
            }
            
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            var json = JsonSerializer.Deserialize<JsonElement>(content);
            
            var places = new List<NearbyPlace>();
            
            if (json.TryGetProperty("results", out var results))
            {
                foreach (var result in results.EnumerateArray().Take(limit))
                {
                    var position = result.GetProperty("position");
                    var address = result.GetProperty("address");
                    
                    var place = new NearbyPlace
                    {
                        PlaceId = result.TryGetProperty("id", out var id) ? id.GetString() ?? Guid.NewGuid().ToString() : Guid.NewGuid().ToString(),
                        Name = result.TryGetProperty("poi", out var poi) && poi.TryGetProperty("name", out var name) 
                            ? name.GetString() ?? "Unknown" : "Unknown",
                        Category = category,
                        Location = new LocationData
                        {
                            Latitude = position.GetProperty("lat").GetDouble(),
                            Longitude = position.GetProperty("lon").GetDouble(),
                            Address = address.TryGetProperty("freeformAddress", out var addr) ? addr.GetString() ?? "" : "",
                            City = address.TryGetProperty("municipality", out var city) ? city.GetString() ?? "" : "",
                            State = address.TryGetProperty("countrySubdivision", out var state) ? state.GetString() ?? "" : "",
                            Country = "India"
                        },
                        DistanceKm = result.TryGetProperty("dist", out var dist) ? dist.GetDouble() / 1000.0 : 0,
                        Phone = result.TryGetProperty("poi", out var poi2) && poi2.TryGetProperty("phone", out var phone) 
                            ? phone.GetString() ?? "" : "",
                        Address = address.TryGetProperty("freeformAddress", out var addr2) ? addr2.GetString() ?? "" : ""
                    };
                    
                    places.Add(place);
                }
            }

            _cache.Set(cacheKey, places, TimeSpan.FromMinutes(_cacheDuration));
            Console.WriteLine($"[AzureMaps] Found {places.Count} nearby {category}(s)");
            return places;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AzureMaps] Nearby search error: {ex.Message}");
            return new List<NearbyPlace>();
        }
    }

    /// <summary>
    /// Calculate route between two points using Azure Maps REST API
    /// </summary>
    /// <param name="origin">Start location</param>
    /// <param name="destination">End location</param>
    /// <param name="waypoints">Optional intermediate stops</param>
    /// <returns>Route with distance, time, and turn-by-turn instructions</returns>
    public async Task<GeoRoute?> CalculateRouteAsync(
        LocationData origin, 
        LocationData destination, 
        List<LocationData>? waypoints = null)
    {
        try
        {
            // Build waypoint string for REST API
            var points = new List<string>
            {
                $"{origin.Latitude},{origin.Longitude}"
            };

            if (waypoints != null && waypoints.Any())
            {
                points.AddRange(waypoints.Select(w => $"{w.Latitude},{w.Longitude}"));
            }

            points.Add($"{destination.Latitude},{destination.Longitude}");
            var waypointString = string.Join(":", points);

            // Call Azure Maps Route Directions REST API
            var url = $"https://atlas.microsoft.com/route/directions/json?api-version=1.0&subscription-key={_subscriptionKey}&query={waypointString}";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var routeData = JsonSerializer.Deserialize<JsonElement>(content);

            if (routeData.TryGetProperty("routes", out var routes) && routes.GetArrayLength() > 0)
            {
                var route = routes[0];
                var summary = route.GetProperty("summary");

                var distanceMeters = summary.GetProperty("lengthInMeters").GetDouble();
                var travelTimeSec = summary.GetProperty("travelTimeInSeconds").GetInt32();

                var geoRoute = new GeoRoute
                {
                    RouteName = $"{origin.City} to {destination.City}",
                    Origin = origin,
                    Destination = destination,
                    Waypoints = waypoints ?? new List<LocationData>(),
                    TotalDistanceKm = distanceMeters / 1000.0,
                    EstimatedTimeMinutes = travelTimeSec / 60,
                    RouteType = "fastest",
                    CalculatedAt = DateTime.UtcNow
                };

                // Extract turn-by-turn instructions if available
                var instructions = new System.Text.StringBuilder();
                if (route.TryGetProperty("guidance", out var guidance) && 
                    guidance.TryGetProperty("instructions", out var instructionsArray))
                {
                    foreach (var instruction in instructionsArray.EnumerateArray())
                    {
                        if (instruction.TryGetProperty("message", out var message))
                        {
                            instructions.AppendLine($"• {message.GetString()}");
                        }
                    }
                }
                geoRoute.Instructions = instructions.ToString();

                Console.WriteLine($"[AzureMaps] Route calculated: {geoRoute.TotalDistanceKm:F2} km, {geoRoute.EstimatedTimeMinutes} min");
                return geoRoute;
            }

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AzureMaps] Route calculation error: {ex.Message}");
            
            // Fallback: return simple straight-line distance route
            var distance = CalculateDistance(origin.Latitude, origin.Longitude, 
                                            destination.Latitude, destination.Longitude);
            return new GeoRoute
            {
                RouteName = $"{origin.City} to {destination.City}",
                Origin = origin,
                Destination = destination,
                Waypoints = waypoints ?? new List<LocationData>(),
                TotalDistanceKm = distance,
                EstimatedTimeMinutes = (int)(distance / 0.75), // Rough estimate at 45 km/h average
                RouteType = "estimated",
                CalculatedAt = DateTime.UtcNow,
                Instructions = "Route calculation unavailable. Distance shown is straight-line estimate."
            };
        }
    }

    /// <summary>
    /// Calculate distance between two points in kilometers
    /// </summary>
    public double CalculateDistance(double lat1, double lng1, double lat2, double lng2)
    {
        const double R = 6371; // Earth's radius in km
        var dLat = ToRadians(lat2 - lat1);
        var dLng = ToRadians(lng2 - lng1);
        
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }

    /// <summary>
    /// Search for a specific address or place using REST API
    /// </summary>
    public async Task<List<LocationData>> SearchPlacesAsync(string query, int limit = 10)
    {
        var cacheKey = $"search-{query.ToLower().Trim()}";
        if (_cache.TryGetValue<List<LocationData>>(cacheKey, out var cachedResults))
        {
            return cachedResults!;
        }

        try
        {
            var url = $"{BaseUrl}/search/address/json?api-version=1.0&subscription-key={_subscriptionKey}&query={Uri.EscapeDataString(query)}&countrySet=IN&limit={limit}";
            
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            var json = JsonSerializer.Deserialize<JsonElement>(content);
            
            var results = new List<LocationData>();
            
            if (json.TryGetProperty("results", out var resultsArray))
            {
                foreach (var result in resultsArray.EnumerateArray().Take(limit))
                {
                    var position = result.GetProperty("position");
                    var address = result.GetProperty("address");
                    
                    var location = new LocationData
                    {
                        Latitude = position.GetProperty("lat").GetDouble(),
                        Longitude = position.GetProperty("lon").GetDouble(),
                        Address = address.TryGetProperty("freeformAddress", out var addr) ? addr.GetString() ?? "" : "",
                        City = address.TryGetProperty("municipality", out var city) ? city.GetString() ?? "" : "",
                        State = address.TryGetProperty("countrySubdivision", out var state) ? state.GetString() ?? "" : "",
                        PostalCode = address.TryGetProperty("postalCode", out var postal) ? postal.GetString() ?? "" : "",
                        Country = "India",
                        LocationType = "SearchResult"
                    };
                    results.Add(location);
                }
            }

            _cache.Set(cacheKey, results, TimeSpan.FromMinutes(_cacheDuration));
            return results;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AzureMaps] Search error: {ex.Message}");
            return new List<LocationData>();
        }
    }

    /// <summary>
    /// Get default map center from configuration (Chennai by default)
    /// </summary>
    public (double Latitude, double Longitude) GetDefaultCenter()
    {
        var lat = double.Parse(_configuration["AzureMaps:DefaultCenter:Latitude"] ?? "13.0827");
        var lng = double.Parse(_configuration["AzureMaps:DefaultCenter:Longitude"] ?? "80.2707");
        return (lat, lng);
    }

    /// <summary>
    /// Get default zoom level from configuration
    /// </summary>
    public int GetDefaultZoom()
    {
        return int.Parse(_configuration["AzureMaps:DefaultZoomLevel"] ?? "12");
    }

    /// <summary>
    /// Get subscription key for JavaScript map initialization
    /// </summary>
    public string GetSubscriptionKey()
    {
        return _subscriptionKey;
    }
}

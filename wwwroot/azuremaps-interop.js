// Azure Maps JavaScript Interop for JurisAI
// Handles map initialization, markers, routes, and user interactions

let azureMapsInstance = null;
let markersLayer = null;
let routesLayer = null;

/**
 * Initialize Azure Maps with subscription key
 * @param {string} elementId - DOM element ID for map container
 * @param {string} subscriptionKey - Azure Maps subscription key
 * @param {number} lat - Initial latitude
 * @param {number} lng - Initial longitude
 * @param {number} zoom - Initial zoom level
 */
window.initializeAzureMap = function (elementId, subscriptionKey, lat, lng, zoom) {
    try {
        // Create map instance
        azureMapsInstance = new atlas.Map(elementId, {
            center: [lng, lat],
            zoom: zoom,
            language: 'en-IN',
            authOptions: {
                authType: 'subscriptionKey',
                subscriptionKey: subscriptionKey
            },
            style: 'road',
            showBuildingModels: true,
            showLogo: false,
            showFeedbackLink: false
        });

        // Wait for map to be ready
        azureMapsInstance.events.add('ready', function () {
            // Create data sources for markers and routes
            const dataSource = new atlas.source.DataSource();
            azureMapsInstance.sources.add(dataSource);

            // Create symbol layer for markers
            markersLayer = new atlas.layer.SymbolLayer(dataSource, null, {
                iconOptions: {
                    image: 'pin-red',
                    anchor: 'bottom',
                    allowOverlap: true,
                    ignorePlacement: true
                },
                textOptions: {
                    textField: ['get', 'title'],
                    offset: [0, -2],
                    color: '#000000',
                    haloColor: '#ffffff',
                    haloWidth: 2
                }
            });
            azureMapsInstance.layers.add(markersLayer);

            // Create line layer for routes
            routesLayer = new atlas.layer.LineLayer(dataSource, null, {
                strokeColor: '#2272B9',
                strokeWidth: 5,
                lineJoin: 'round',
                lineCap: 'round'
            });
            azureMapsInstance.layers.add(routesLayer);

            console.log('Azure Maps initialized successfully');
        });

        return true;
    } catch (error) {
        console.error('Failed to initialize Azure Maps:', error);
        return false;
    }
};

/**
 * Add a marker to the map
 * @param {number} lat - Latitude
 * @param {number} lng - Longitude
 * @param {string} title - Marker title
 * @param {string} description - Marker description
 * @param {string} color - Marker color (red, blue, green, yellow)
 */
window.addMarker = function (lat, lng, title, description, color = 'red') {
    if (!azureMapsInstance) {
        console.error('Map not initialized');
        return false;
    }

    try {
        const dataSource = azureMapsInstance.sources.getById('defaultDataSource');
        if (!dataSource) {
            const newDataSource = new atlas.source.DataSource('defaultDataSource');
            azureMapsInstance.sources.add(newDataSource);
        }

        const marker = new atlas.data.Feature(new atlas.data.Point([lng, lat]), {
            title: title,
            description: description,
            color: color
        });

        azureMapsInstance.sources.getById('defaultDataSource').add(marker);

        // Create popup for marker
        const popup = new atlas.Popup({
            pixelOffset: [0, -30],
            closeButton: true
        });

        azureMapsInstance.events.add('click', markersLayer, function (e) {
            if (e.shapes && e.shapes.length > 0) {
                const properties = e.shapes[0].getProperties();
                popup.setOptions({
                    content: `<div style="padding:10px;"><strong>${properties.title}</strong><br/>${properties.description}</div>`,
                    position: e.shapes[0].getCoordinates()
                });
                popup.open(azureMapsInstance);
            }
        });

        return true;
    } catch (error) {
        console.error('Failed to add marker:', error);
        return false;
    }
};

/**
 * Add multiple markers at once
 * @param {Array} markers - Array of {lat, lng, title, description, color}
 */
window.addMarkers = function (markers) {
    if (!azureMapsInstance) {
        console.error('Map not initialized');
        return false;
    }

    try {
        markers.forEach(marker => {
            addMarker(marker.lat, marker.lng, marker.title, marker.description, marker.color);
        });
        return true;
    } catch (error) {
        console.error('Failed to add markers:', error);
        return false;
    }
};

/**
 * Clear all markers from the map
 */
window.clearMarkers = function () {
    if (!azureMapsInstance) return false;

    try {
        const dataSource = azureMapsInstance.sources.getById('defaultDataSource');
        if (dataSource) {
            dataSource.clear();
        }
        return true;
    } catch (error) {
        console.error('Failed to clear markers:', error);
        return false;
    }
};

/**
 * Draw a route on the map
 * @param {Array} coordinates - Array of [lng, lat] coordinates
 * @param {string} color - Route color
 * @param {number} width - Route line width
 */
window.drawRoute = function (coordinates, color = '#2272B9', width = 5) {
    if (!azureMapsInstance) {
        console.error('Map not initialized');
        return false;
    }

    try {
        const dataSource = azureMapsInstance.sources.getById('defaultDataSource');
        if (!dataSource) {
            const newDataSource = new atlas.source.DataSource('defaultDataSource');
            azureMapsInstance.sources.add(newDataSource);
        }

        const line = new atlas.data.LineString(coordinates);
        azureMapsInstance.sources.getById('defaultDataSource').add(line);

        // Update route layer style
        const routeLayer = azureMapsInstance.layers.getLayerById('routeLayer');
        if (routeLayer) {
            routeLayer.setOptions({
                strokeColor: color,
                strokeWidth: width
            });
        }

        return true;
    } catch (error) {
        console.error('Failed to draw route:', error);
        return false;
    }
};

/**
 * Center map on specific coordinates
 * @param {number} lat - Latitude
 * @param {number} lng - Longitude
 * @param {number} zoom - Zoom level
 */
window.centerMap = function (lat, lng, zoom = 12) {
    if (!azureMapsInstance) return false;

    try {
        azureMapsInstance.setCamera({
            center: [lng, lat],
            zoom: zoom,
            type: 'ease',
            duration: 1000
        });
        return true;
    } catch (error) {
        console.error('Failed to center map:', error);
        return false;
    }
};

/**
 * Get user's current location using browser Geolocation API
 * @param {object} dotNetHelper - .NET object reference for callback
 */
window.getCurrentLocation = function (dotNetHelper) {
    if (!navigator.geolocation) {
        console.error('Geolocation not supported');
        return false;
    }

    navigator.geolocation.getCurrentPosition(
        function (position) {
            const location = {
                latitude: position.coords.latitude,
                longitude: position.coords.longitude,
                accuracy: position.coords.accuracy
            };
            dotNetHelper.invokeMethodAsync('OnLocationReceived', location);
        },
        function (error) {
            console.error('Geolocation error:', error.message);
            dotNetHelper.invokeMethodAsync('OnLocationError', error.message);
        },
        {
            enableHighAccuracy: true,
            timeout: 10000,
            maximumAge: 0
        }
    );

    return true;
};

/**
 * Watch user's location continuously
 * @param {object} dotNetHelper - .NET object reference for callback
 */
let locationWatchId = null;
window.watchLocation = function (dotNetHelper) {
    if (!navigator.geolocation) return false;

    locationWatchId = navigator.geolocation.watchPosition(
        function (position) {
            const location = {
                latitude: position.coords.latitude,
                longitude: position.coords.longitude,
                accuracy: position.coords.accuracy,
                timestamp: position.timestamp
            };
            dotNetHelper.invokeMethodAsync('OnLocationUpdate', location);
        },
        function (error) {
            console.error('Location watch error:', error.message);
        },
        {
            enableHighAccuracy: true,
            maximumAge: 5000,
            timeout: 10000
        }
    );

    return true;
};

/**
 * Stop watching location
 */
window.stopWatchingLocation = function () {
    if (locationWatchId !== null) {
        navigator.geolocation.clearWatch(locationWatchId);
        locationWatchId = null;
        return true;
    }
    return false;
};

/**
 * Add a heatmap layer for crime hotspots
 * @param {Array} points - Array of {lat, lng, weight}
 */
window.addHeatmapLayer = function (points) {
    if (!azureMapsInstance) return false;

    try {
        const heatmapSource = new atlas.source.DataSource('heatmapSource');
        azureMapsInstance.sources.add(heatmapSource);

        // Convert points to features
        const features = points.map(point => {
            return new atlas.data.Feature(
                new atlas.data.Point([point.lng, point.lat]),
                { weight: point.weight || 1 }
            );
        });

        heatmapSource.add(features);

        // Create heatmap layer
        const heatmapLayer = new atlas.layer.HeatMapLayer(heatmapSource, 'heatmapLayer', {
            radius: 20,
            opacity: 0.7,
            color: [
                'interpolate',
                ['linear'],
                ['heatmap-density'],
                0, 'rgba(0,0,255,0)',
                0.2, 'royalblue',
                0.4, 'cyan',
                0.6, 'lime',
                0.8, 'yellow',
                1, 'red'
            ]
        });

        azureMapsInstance.layers.add(heatmapLayer, 'labels');

        return true;
    } catch (error) {
        console.error('Failed to add heatmap:', error);
        return false;
    }
};

/**
 * Fit map bounds to show all markers
 */
window.fitMapToMarkers = function () {
    if (!azureMapsInstance) return false;

    try {
        const dataSource = azureMapsInstance.sources.getById('defaultDataSource');
        if (dataSource) {
            const shapes = dataSource.getShapes();
            if (shapes.length > 0) {
                const bounds = atlas.data.BoundingBox.fromData(shapes);
                azureMapsInstance.setCamera({
                    bounds: bounds,
                    padding: 50,
                    type: 'ease',
                    duration: 1000
                });
            }
        }
        return true;
    } catch (error) {
        console.error('Failed to fit bounds:', error);
        return false;
    }
};

/**
 * Change map style
 * @param {string} style - road, satellite, grayscale_dark, night
 */
window.changeMapStyle = function (style) {
    if (!azureMapsInstance) return false;

    try {
        azureMapsInstance.setStyle({ style: style });
        return true;
    } catch (error) {
        console.error('Failed to change style:', error);
        return false;
    }
};

/**
 * Measure distance between two points
 * @param {number} lat1 - First point latitude
 * @param {number} lng1 - First point longitude
 * @param {number} lat2 - Second point latitude
 * @param {number} lng2 - Second point longitude
 * @returns {number} Distance in kilometers
 */
window.calculateDistance = function (lat1, lng1, lat2, lng2) {
    const R = 6371; // Earth's radius in km
    const dLat = (lat2 - lat1) * Math.PI / 180;
    const dLng = (lng2 - lng1) * Math.PI / 180;
    const a = Math.sin(dLat / 2) * Math.sin(dLat / 2) +
        Math.cos(lat1 * Math.PI / 180) * Math.cos(lat2 * Math.PI / 180) *
        Math.sin(dLng / 2) * Math.sin(dLng / 2);
    const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
    return R * c;
};

console.log('Azure Maps JavaScript interop loaded');

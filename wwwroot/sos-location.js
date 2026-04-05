/**
 * SOS Location & Background Service
 * Real-time location tracking with background execution support
 */

// Global state
let watchId = null;
let wakeLock = null;
let locationHistory = [];
let isTracking = false;
let dotNetReference = null;
let updateInterval = null;
let lastPosition = null;

// Initialize SOS tracking system
window.SOSLocationService = {
    
    /**
     * Initialize the location service with .NET reference
     */
    initialize: function(dotNetRef) {
        dotNetReference = dotNetRef;
        console.log('[SOS] Location service initialized');
        
        // Listen for visibility changes
        document.addEventListener('visibilitychange', this.handleVisibilityChange.bind(this));
        
        // Listen for page unload to warn user during active SOS
        window.addEventListener('beforeunload', this.handleBeforeUnload.bind(this));
        
        return true;
    },

    /**
     * Get current location (one-time)
     */
    getCurrentLocation: function() {
        return new Promise((resolve, reject) => {
            if (!navigator.geolocation) {
                reject({ error: 'Geolocation not supported', code: 0 });
                return;
            }

            navigator.geolocation.getCurrentPosition(
                (position) => {
                    const locationData = this.formatPosition(position);
                    lastPosition = locationData;
                    resolve(locationData);
                },
                (error) => {
                    reject({ error: error.message, code: error.code });
                },
                {
                    enableHighAccuracy: true,
                    timeout: 10000,
                    maximumAge: 0
                }
            );
        });
    },

    /**
     * Start continuous location tracking
     */
    startTracking: function() {
        if (isTracking) {
            console.log('[SOS] Already tracking');
            return true;
        }

        if (!navigator.geolocation) {
            console.error('[SOS] Geolocation not supported');
            return false;
        }

        isTracking = true;
        locationHistory = [];

        // Start watching position
        watchId = navigator.geolocation.watchPosition(
            (position) => {
                const locationData = this.formatPosition(position);
                lastPosition = locationData;
                locationHistory.push(locationData);
                
                // Keep only last 100 positions
                if (locationHistory.length > 100) {
                    locationHistory.shift();
                }

                // Notify .NET of location update
                if (dotNetReference) {
                    dotNetReference.invokeMethodAsync('OnLocationUpdate', locationData);
                }
            },
            (error) => {
                console.error('[SOS] Location error:', error.message);
                if (dotNetReference) {
                    dotNetReference.invokeMethodAsync('OnLocationError', error.message);
                }
            },
            {
                enableHighAccuracy: true,
                timeout: 15000,
                maximumAge: 0
            }
        );

        // Also update at regular intervals for steady UI updates
        updateInterval = setInterval(() => {
            if (lastPosition && dotNetReference) {
                dotNetReference.invokeMethodAsync('OnLocationUpdate', lastPosition);
            }
        }, 5000);

        console.log('[SOS] Location tracking started');
        return true;
    },

    /**
     * Stop location tracking
     */
    stopTracking: function() {
        if (watchId !== null) {
            navigator.geolocation.clearWatch(watchId);
            watchId = null;
        }
        
        if (updateInterval) {
            clearInterval(updateInterval);
            updateInterval = null;
        }

        isTracking = false;
        console.log('[SOS] Location tracking stopped');
        return true;
    },

    /**
     * Get location history
     */
    getLocationHistory: function() {
        return locationHistory;
    },

    /**
     * Request Wake Lock to keep screen active
     */
    requestWakeLock: async function() {
        try {
            if ('wakeLock' in navigator) {
                wakeLock = await navigator.wakeLock.request('screen');
                console.log('[SOS] Wake Lock acquired');
                
                wakeLock.addEventListener('release', () => {
                    console.log('[SOS] Wake Lock released');
                });
                
                return true;
            } else {
                console.warn('[SOS] Wake Lock API not supported');
                return false;
            }
        } catch (err) {
            console.error('[SOS] Wake Lock error:', err);
            return false;
        }
    },

    /**
     * Release Wake Lock
     */
    releaseWakeLock: async function() {
        if (wakeLock) {
            await wakeLock.release();
            wakeLock = null;
            console.log('[SOS] Wake Lock released manually');
        }
        return true;
    },

    /**
     * Request notification permission for background alerts
     */
    requestNotificationPermission: async function() {
        if (!('Notification' in window)) {
            return 'not-supported';
        }
        
        if (Notification.permission === 'granted') {
            return 'granted';
        }
        
        const permission = await Notification.requestPermission();
        return permission;
    },

    /**
     * Show notification (for background alerts)
     */
    showNotification: function(title, body, tag) {
        if (Notification.permission === 'granted') {
            const notification = new Notification(title, {
                body: body,
                icon: '/favicon.ico',
                tag: tag || 'sos-alert',
                requireInteraction: true,
                vibrate: [200, 100, 200, 100, 200]
            });
            
            notification.onclick = function() {
                window.focus();
                notification.close();
            };
            
            return true;
        }
        return false;
    },

    /**
     * Handle visibility change (app going to background)
     */
    handleVisibilityChange: function() {
        if (document.visibilityState === 'hidden' && isTracking) {
            // App went to background while SOS is active
            console.log('[SOS] App in background, SOS active');
            this.showNotification(
                '🚨 SOS Active',
                'Emergency tracking is running in background',
                'sos-background'
            );
        } else if (document.visibilityState === 'visible' && isTracking) {
            // Reacquire wake lock when coming back to foreground
            this.requestWakeLock();
        }
    },

    /**
     * Handle before unload (prevent accidental close during SOS)
     */
    handleBeforeUnload: function(e) {
        if (isTracking) {
            e.preventDefault();
            e.returnValue = 'SOS is active! Are you sure you want to leave?';
            return e.returnValue;
        }
    },

    /**
     * Format position data
     */
    formatPosition: function(position) {
        return {
            latitude: position.coords.latitude,
            longitude: position.coords.longitude,
            accuracy: position.coords.accuracy,
            altitude: position.coords.altitude,
            altitudeAccuracy: position.coords.altitudeAccuracy,
            heading: position.coords.heading,
            speed: position.coords.speed,
            timestamp: new Date(position.timestamp).toISOString()
        };
    },

    /**
     * Reverse geocode location to address
     */
    reverseGeocode: async function(lat, lng) {
        try {
            // Using free Nominatim API (OpenStreetMap)
            const response = await fetch(
                `https://nominatim.openstreetmap.org/reverse?format=json&lat=${lat}&lon=${lng}&zoom=18&addressdetails=1`,
                {
                    headers: {
                        'Accept-Language': 'en',
                        'User-Agent': 'AILegalAssistant/1.0'
                    }
                }
            );
            
            if (response.ok) {
                const data = await response.json();
                return {
                    displayName: data.display_name,
                    address: data.address,
                    success: true
                };
            }
            
            return { success: false, error: 'Geocoding failed' };
        } catch (error) {
            console.error('[SOS] Geocoding error:', error);
            return { success: false, error: error.message };
        }
    },

    /**
     * Get tracking statistics
     */
    getTrackingStats: function() {
        if (locationHistory.length < 2) {
            return {
                totalDistance: 0,
                averageSpeed: 0,
                maxSpeed: 0,
                duration: 0,
                points: locationHistory.length
            };
        }

        let totalDistance = 0;
        let maxSpeed = 0;
        let speedSum = 0;
        let speedCount = 0;

        for (let i = 1; i < locationHistory.length; i++) {
            const prev = locationHistory[i - 1];
            const curr = locationHistory[i];
            
            totalDistance += this.calculateDistance(
                prev.latitude, prev.longitude,
                curr.latitude, curr.longitude
            );
            
            if (curr.speed !== null && curr.speed !== undefined) {
                speedSum += curr.speed;
                speedCount++;
                if (curr.speed > maxSpeed) maxSpeed = curr.speed;
            }
        }

        const startTime = new Date(locationHistory[0].timestamp);
        const endTime = new Date(locationHistory[locationHistory.length - 1].timestamp);
        const duration = (endTime - startTime) / 1000; // in seconds

        return {
            totalDistance: Math.round(totalDistance),
            averageSpeed: speedCount > 0 ? (speedSum / speedCount * 3.6).toFixed(1) : 0, // Convert m/s to km/h
            maxSpeed: (maxSpeed * 3.6).toFixed(1), // Convert m/s to km/h
            duration: duration,
            points: locationHistory.length
        };
    },

    /**
     * Calculate distance between two points (Haversine formula)
     */
    calculateDistance: function(lat1, lon1, lat2, lon2) {
        const R = 6371e3; // Earth's radius in meters
        const φ1 = lat1 * Math.PI / 180;
        const φ2 = lat2 * Math.PI / 180;
        const Δφ = (lat2 - lat1) * Math.PI / 180;
        const Δλ = (lon2 - lon1) * Math.PI / 180;

        const a = Math.sin(Δφ / 2) * Math.sin(Δφ / 2) +
                  Math.cos(φ1) * Math.cos(φ2) *
                  Math.sin(Δλ / 2) * Math.sin(Δλ / 2);
        const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));

        return R * c; // Distance in meters
    },

    /**
     * Check if tracking is active
     */
    isActive: function() {
        return isTracking;
    },

    /**
     * Get last known position
     */
    getLastPosition: function() {
        return lastPosition;
    },

    /**
     * Vibrate device (for alerts)
     */
    vibrate: function(pattern) {
        if ('vibrate' in navigator) {
            navigator.vibrate(pattern || [200, 100, 200, 100, 200, 100, 200]);
            return true;
        }
        return false;
    },

    /**
     * Play alert sound
     */
    playAlertSound: function() {
        try {
            const audioContext = new (window.AudioContext || window.webkitAudioContext)();
            const oscillator = audioContext.createOscillator();
            const gainNode = audioContext.createGain();
            
            oscillator.connect(gainNode);
            gainNode.connect(audioContext.destination);
            
            oscillator.frequency.value = 800;
            oscillator.type = 'sine';
            gainNode.gain.value = 0.5;
            
            oscillator.start();
            
            // Beep pattern
            setTimeout(() => { gainNode.gain.value = 0; }, 200);
            setTimeout(() => { gainNode.gain.value = 0.5; }, 300);
            setTimeout(() => { gainNode.gain.value = 0; }, 500);
            setTimeout(() => { gainNode.gain.value = 0.5; }, 600);
            setTimeout(() => { gainNode.gain.value = 0; }, 800);
            setTimeout(() => { oscillator.stop(); }, 900);
            
            return true;
        } catch (e) {
            console.error('[SOS] Audio error:', e);
            return false;
        }
    },

    // ========================================================================
    // AUDIO RECORDING
    // ========================================================================
    
    mediaRecorder: null,
    audioChunks: [],
    audioStream: null,
    recordingStartTime: null,
    isRecording: false,

    /**
     * Request microphone permission and initialize audio recording
     */
    requestMicrophonePermission: async function() {
        try {
            // Check if MediaRecorder is supported
            if (!navigator.mediaDevices || !window.MediaRecorder) {
                return { success: false, error: 'Audio recording not supported in this browser' };
            }

            // Request microphone access
            const stream = await navigator.mediaDevices.getUserMedia({ 
                audio: {
                    echoCancellation: true,
                    noiseSuppression: true,
                    autoGainControl: true
                } 
            });
            
            this.audioStream = stream;
            console.log('[SOS] Microphone permission granted');
            
            return { success: true, error: null };
        } catch (error) {
            console.error('[SOS] Microphone permission denied:', error);
            let errorMessage = 'Microphone access denied';
            
            if (error.name === 'NotAllowedError') {
                errorMessage = 'Microphone permission denied. Please enable it in browser settings.';
            } else if (error.name === 'NotFoundError') {
                errorMessage = 'No microphone found on this device.';
            } else if (error.name === 'NotReadableError') {
                errorMessage = 'Microphone is already in use by another application.';
            }
            
            return { success: false, error: errorMessage };
        }
    },

    /**
     * Start audio recording
     */
    startRecording: async function() {
        try {
            // Request permission if not already granted
            if (!this.audioStream) {
                const permResult = await this.requestMicrophonePermission();
                if (!permResult.success) {
                    return permResult;
                }
            }

            // Check if stream is still active
            if (!this.audioStream.active) {
                const permResult = await this.requestMicrophonePermission();
                if (!permResult.success) {
                    return permResult;
                }
            }

            // Clear previous chunks
            this.audioChunks = [];
            this.recordingStartTime = Date.now();
            this.isRecording = true;

            // Create MediaRecorder
            const mimeType = MediaRecorder.isTypeSupported('audio/webm;codecs=opus') 
                ? 'audio/webm;codecs=opus' 
                : 'audio/webm';
            
            this.mediaRecorder = new MediaRecorder(this.audioStream, { mimeType });

            this.mediaRecorder.ondataavailable = (event) => {
                if (event.data.size > 0) {
                    this.audioChunks.push(event.data);
                }
            };

            this.mediaRecorder.onstop = () => {
                console.log('[SOS] Recording stopped, chunks:', this.audioChunks.length);
            };

            this.mediaRecorder.onerror = (event) => {
                console.error('[SOS] MediaRecorder error:', event.error);
                if (dotNetReference) {
                    dotNetReference.invokeMethodAsync('OnRecordingError', event.error?.message || 'Recording error');
                }
            };

            // Start recording - collect data every 5 seconds
            this.mediaRecorder.start(5000);
            console.log('[SOS] Recording started');

            // Notify .NET
            if (dotNetReference) {
                dotNetReference.invokeMethodAsync('OnRecordingStarted');
            }

            return { success: true, error: null };
        } catch (error) {
            console.error('[SOS] Start recording error:', error);
            this.isRecording = false;
            return { success: false, error: error.message };
        }
    },

    /**
     * Stop audio recording
     */
    stopRecording: function() {
        return new Promise((resolve) => {
            if (!this.mediaRecorder || this.mediaRecorder.state === 'inactive') {
                this.isRecording = false;
                resolve({ success: true, duration: 0, chunks: 0 });
                return;
            }

            this.mediaRecorder.onstop = () => {
                const duration = Math.floor((Date.now() - this.recordingStartTime) / 1000);
                this.isRecording = false;
                
                console.log('[SOS] Recording stopped. Duration:', duration, 'seconds');
                
                // Notify .NET
                if (dotNetReference) {
                    dotNetReference.invokeMethodAsync('OnRecordingStopped', duration);
                }
                
                resolve({ 
                    success: true, 
                    duration: duration, 
                    chunks: this.audioChunks.length 
                });
            };

            this.mediaRecorder.stop();
        });
    },

    /**
     * Get recording status
     */
    getRecordingStatus: function() {
        if (!this.isRecording || !this.recordingStartTime) {
            return { 
                isRecording: false, 
                duration: 0, 
                chunksCount: 0 
            };
        }

        return {
            isRecording: this.isRecording,
            duration: Math.floor((Date.now() - this.recordingStartTime) / 1000),
            chunksCount: this.audioChunks.length
        };
    },

    /**
     * Get recorded audio as base64 data URL
     */
    getRecordedAudio: async function() {
        if (this.audioChunks.length === 0) {
            return null;
        }

        try {
            const blob = new Blob(this.audioChunks, { type: 'audio/webm' });
            
            return new Promise((resolve) => {
                const reader = new FileReader();
                reader.onloadend = () => {
                    resolve({
                        dataUrl: reader.result,
                        size: blob.size,
                        duration: Math.floor((Date.now() - this.recordingStartTime) / 1000)
                    });
                };
                reader.readAsDataURL(blob);
            });
        } catch (error) {
            console.error('[SOS] Get audio error:', error);
            return null;
        }
    },

    /**
     * Save recording to IndexedDB for backup
     */
    saveRecordingToStorage: async function(sosId) {
        if (this.audioChunks.length === 0) {
            return false;
        }

        try {
            const blob = new Blob(this.audioChunks, { type: 'audio/webm' });
            
            const db = await this.openAudioDB();
            const tx = db.transaction('recordings', 'readwrite');
            const store = tx.objectStore('recordings');
            
            await store.put({
                id: sosId || Date.now(),
                blob: blob,
                timestamp: new Date().toISOString(),
                duration: Math.floor((Date.now() - this.recordingStartTime) / 1000)
            });
            
            console.log('[SOS] Recording saved to storage');
            return true;
        } catch (error) {
            console.error('[SOS] Save recording error:', error);
            return false;
        }
    },

    /**
     * Open IndexedDB for audio storage
     */
    openAudioDB: function() {
        return new Promise((resolve, reject) => {
            const request = indexedDB.open('SOS_AudioDB', 1);
            
            request.onerror = () => reject(request.error);
            request.onsuccess = () => resolve(request.result);
            
            request.onupgradeneeded = (event) => {
                const db = event.target.result;
                if (!db.objectStoreNames.contains('recordings')) {
                    db.createObjectStore('recordings', { keyPath: 'id' });
                }
            };
        });
    },

    /**
     * Release microphone stream
     */
    releaseMicrophone: function() {
        if (this.audioStream) {
            this.audioStream.getTracks().forEach(track => track.stop());
            this.audioStream = null;
            console.log('[SOS] Microphone released');
        }
        
        this.mediaRecorder = null;
        this.audioChunks = [];
        this.isRecording = false;
        this.recordingStartTime = null;
    },

    /**
     * Full cleanup
     */
    cleanup: function() {
        this.stopTracking();
        this.releaseWakeLock();
        this.stopRecording().then(() => {
            this.releaseMicrophone();
        });
        locationHistory = [];
        lastPosition = null;
        dotNetReference = null;
    }
};

// Register service worker for background execution (if available)
if ('serviceWorker' in navigator) {
    window.addEventListener('load', async () => {
        try {
            // Check if we have a service worker file
            const response = await fetch('/sos-service-worker.js', { method: 'HEAD' });
            if (response.ok) {
                const registration = await navigator.serviceWorker.register('/sos-service-worker.js');
                console.log('[SOS] Service Worker registered:', registration.scope);
            }
        } catch (error) {
            // Service worker not available, continue without it
            console.log('[SOS] Service Worker not available');
        }
    });
}

console.log('[SOS] Location service loaded');

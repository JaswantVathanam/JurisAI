/**
 * SOS Service Worker for background execution
 * Enables location tracking and notifications even when app is in background
 */

const CACHE_NAME = 'sos-cache-v1';
const SOS_CHANNEL = new BroadcastChannel('sos-channel');

// Install event
self.addEventListener('install', (event) => {
    console.log('[SOS SW] Installing...');
    self.skipWaiting();
});

// Activate event
self.addEventListener('activate', (event) => {
    console.log('[SOS SW] Activated');
    event.waitUntil(clients.claim());
});

// Push notification event
self.addEventListener('push', (event) => {
    const data = event.data ? event.data.json() : {};
    
    const options = {
        body: data.body || 'SOS Alert is active',
        icon: '/favicon.ico',
        badge: '/favicon.ico',
        vibrate: [200, 100, 200, 100, 200],
        tag: 'sos-alert',
        requireInteraction: true,
        actions: [
            { action: 'view', title: 'View Status' },
            { action: 'safe', title: "I'm Safe" }
        ]
    };

    event.waitUntil(
        self.registration.showNotification(data.title || '🚨 SOS Active', options)
    );
});

// Notification click event
self.addEventListener('notificationclick', (event) => {
    event.notification.close();

    if (event.action === 'safe') {
        // Broadcast to main app that user marked safe
        SOS_CHANNEL.postMessage({ type: 'USER_MARKED_SAFE' });
    }

    // Focus or open the app
    event.waitUntil(
        clients.matchAll({ type: 'window', includeUncontrolled: true })
            .then((clientList) => {
                // Check if app is already open
                for (const client of clientList) {
                    if (client.url.includes('/emergency-sos') && 'focus' in client) {
                        return client.focus();
                    }
                }
                // Open new window
                if (clients.openWindow) {
                    return clients.openWindow('/emergency-sos');
                }
            })
    );
});

// Background sync event (for sending location updates when back online)
self.addEventListener('sync', (event) => {
    if (event.tag === 'sos-location-sync') {
        event.waitUntil(syncLocationData());
    }
});

// Periodic background sync (if supported)
self.addEventListener('periodicsync', (event) => {
    if (event.tag === 'sos-check') {
        event.waitUntil(checkSOSStatus());
    }
});

// Message from main app
self.addEventListener('message', (event) => {
    const { type, data } = event.data;

    switch (type) {
        case 'SOS_ACTIVATED':
            console.log('[SOS SW] SOS Activated');
            // Start periodic notifications if in background
            startBackgroundNotifications(data);
            break;

        case 'SOS_DEACTIVATED':
            console.log('[SOS SW] SOS Deactivated');
            stopBackgroundNotifications();
            break;

        case 'LOCATION_UPDATE':
            // Store location for background sync
            storeLocation(data);
            break;
    }
});

let notificationInterval = null;

function startBackgroundNotifications(data) {
    // Clear any existing interval
    stopBackgroundNotifications();

    // Show notification every 2 minutes if app is in background
    notificationInterval = setInterval(() => {
        self.registration.showNotification('🚨 SOS Still Active', {
            body: `Emergency tracking active for ${data.emergencyType || 'Unknown'}`,
            icon: '/favicon.ico',
            tag: 'sos-reminder',
            requireInteraction: false,
            silent: true
        });
    }, 120000); // 2 minutes
}

function stopBackgroundNotifications() {
    if (notificationInterval) {
        clearInterval(notificationInterval);
        notificationInterval = null;
    }
}

async function syncLocationData() {
    // Get stored locations from IndexedDB and sync
    try {
        const db = await openDatabase();
        const locations = await getStoredLocations(db);
        
        if (locations.length > 0) {
            // Send to server (implement your API endpoint)
            console.log('[SOS SW] Syncing', locations.length, 'locations');
            // Clear synced locations
            await clearLocations(db);
        }
    } catch (error) {
        console.error('[SOS SW] Sync error:', error);
    }
}

async function checkSOSStatus() {
    // Periodic check if SOS is still active
    SOS_CHANNEL.postMessage({ type: 'STATUS_CHECK' });
}

function storeLocation(location) {
    // Store in IndexedDB for offline sync
    openDatabase().then(db => {
        const tx = db.transaction('locations', 'readwrite');
        tx.objectStore('locations').add({
            ...location,
            storedAt: new Date().toISOString()
        });
    }).catch(console.error);
}

function openDatabase() {
    return new Promise((resolve, reject) => {
        const request = indexedDB.open('SOSDatabase', 1);
        
        request.onerror = () => reject(request.error);
        request.onsuccess = () => resolve(request.result);
        
        request.onupgradeneeded = (event) => {
            const db = event.target.result;
            if (!db.objectStoreNames.contains('locations')) {
                db.createObjectStore('locations', { autoIncrement: true });
            }
        };
    });
}

function getStoredLocations(db) {
    return new Promise((resolve, reject) => {
        const tx = db.transaction('locations', 'readonly');
        const request = tx.objectStore('locations').getAll();
        request.onerror = () => reject(request.error);
        request.onsuccess = () => resolve(request.result);
    });
}

function clearLocations(db) {
    return new Promise((resolve, reject) => {
        const tx = db.transaction('locations', 'readwrite');
        const request = tx.objectStore('locations').clear();
        request.onerror = () => reject(request.error);
        request.onsuccess = () => resolve();
    });
}

console.log('[SOS SW] Service Worker loaded');

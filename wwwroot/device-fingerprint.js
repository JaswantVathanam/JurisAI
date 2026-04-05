// Cross-platform device fingerprint generation for session security
// Supports: Windows, macOS, Android, iOS, Linux — each with platform-specific signals
window.deviceFingerprint = {

    // Detect operating system
    detectPlatform: function () {
        var ua = navigator.userAgent || '';
        var platform = navigator.platform || '';
        var uaData = navigator.userAgentData;

        // Use modern UA Client Hints if available
        if (uaData && uaData.platform) {
            var p = uaData.platform.toLowerCase();
            if (p === 'android') return 'Android';
            if (p === 'ios') return 'iOS';
            if (p === 'windows') return 'Windows';
            if (p === 'macos' || p === 'mac os x') return 'macOS';
            if (p === 'linux') return 'Linux';
        }

        // Fallback to UA string parsing
        if (/iPad|iPhone|iPod/.test(ua) && !window.MSStream) return 'iOS';
        if (/android/i.test(ua)) return 'Android';
        if (/Mac/.test(platform)) return 'macOS';
        if (/Win/.test(platform)) return 'Windows';
        if (/Linux/.test(platform)) return 'Linux';
        return 'Unknown';
    },

    // Detect browser
    detectBrowser: function () {
        var ua = navigator.userAgent || '';
        if (/Edg\//i.test(ua)) return 'Edge';
        if (/OPR|Opera/i.test(ua)) return 'Opera';
        if (/Chrome/i.test(ua) && !/Edg/i.test(ua)) return 'Chrome';
        if (/Safari/i.test(ua) && !/Chrome/i.test(ua)) return 'Safari';
        if (/Firefox/i.test(ua)) return 'Firefox';
        return 'Unknown';
    },

    // Detect device type
    detectDeviceType: function () {
        var ua = navigator.userAgent || '';
        if (/Mobi|Android.*Mobile|iPhone|iPod/i.test(ua)) return 'Mobile';
        if (/iPad|Android(?!.*Mobile)|Tablet/i.test(ua)) return 'Tablet';
        return 'Desktop';
    },

    // Check if WebAuthn/biometric is supported
    checkBiometricSupport: async function () {
        try {
            if (window.PublicKeyCredential) {
                var available = await PublicKeyCredential.isUserVerifyingPlatformAuthenticatorAvailable();
                return available;
            }
        } catch (e) { }
        return false;
    },

    // Platform-specific fingerprint components
    getPlatformSignals: function (platform) {
        var signals = [];

        switch (platform) {
            case 'Windows':
                signals.push('dpr:' + (window.devicePixelRatio || 1));
                signals.push('scr:' + screen.width + 'x' + screen.height + 'x' + screen.availWidth + 'x' + screen.availHeight);
                signals.push('cd:' + screen.colorDepth);
                try { signals.push('touch:' + navigator.maxTouchPoints); } catch (e) { signals.push('touch:0'); }
                try { signals.push('mem:' + (navigator.deviceMemory || 'na')); } catch (e) { signals.push('mem:na'); }
                signals.push('cores:' + (navigator.hardwareConcurrency || 'na'));
                try {
                    var conn = navigator.connection || navigator.mozConnection || navigator.webkitConnection;
                    if (conn) signals.push('net:' + (conn.effectiveType || 'na'));
                } catch (e) { signals.push('net:na'); }
                break;

            case 'macOS':
                signals.push('dpr:' + (window.devicePixelRatio || 1));
                signals.push('scr:' + screen.width + 'x' + screen.height);
                signals.push('cd:' + screen.colorDepth);
                signals.push('retina:' + (window.devicePixelRatio >= 2 ? 'yes' : 'no'));
                signals.push('cores:' + (navigator.hardwareConcurrency || 'na'));
                try { signals.push('mem:' + (navigator.deviceMemory || 'na')); } catch (e) { signals.push('mem:na'); }
                // macOS Safari specific
                try { signals.push('webgl:' + (!!document.createElement('canvas').getContext('webgl2') ? '2' : '1')); } catch (e) { signals.push('webgl:na'); }
                break;

            case 'Android':
                signals.push('dpr:' + (window.devicePixelRatio || 1));
                signals.push('scr:' + screen.width + 'x' + screen.height);
                signals.push('orient:' + (screen.orientation ? screen.orientation.type : 'na'));
                signals.push('touch:' + (navigator.maxTouchPoints || 0));
                try { signals.push('mem:' + (navigator.deviceMemory || 'na')); } catch (e) { signals.push('mem:na'); }
                signals.push('cores:' + (navigator.hardwareConcurrency || 'na'));
                try {
                    var conn = navigator.connection || navigator.mozConnection || navigator.webkitConnection;
                    if (conn) {
                        signals.push('net:' + (conn.effectiveType || 'na'));
                        signals.push('dl:' + (conn.downlink || 'na'));
                    }
                } catch (e) { signals.push('net:na'); }
                signals.push('vp:' + window.innerWidth + 'x' + window.innerHeight);
                break;

            case 'iOS':
                signals.push('dpr:' + (window.devicePixelRatio || 1));
                signals.push('scr:' + screen.width + 'x' + screen.height);
                signals.push('touch:' + (navigator.maxTouchPoints || 0));
                signals.push('retina:' + (window.devicePixelRatio >= 2 ? 'yes' : 'no'));
                signals.push('vp:' + window.innerWidth + 'x' + window.innerHeight);
                // iOS Safari safe area detection
                try {
                    var computedStyle = getComputedStyle(document.documentElement);
                    var safeTop = computedStyle.getPropertyValue('env(safe-area-inset-top)') || '0px';
                    signals.push('notch:' + (safeTop !== '0px' ? 'yes' : 'no'));
                } catch (e) { signals.push('notch:na'); }
                signals.push('standalone:' + (window.navigator.standalone ? 'yes' : 'no'));
                break;

            case 'Linux':
                signals.push('dpr:' + (window.devicePixelRatio || 1));
                signals.push('scr:' + screen.width + 'x' + screen.height);
                signals.push('cd:' + screen.colorDepth);
                signals.push('cores:' + (navigator.hardwareConcurrency || 'na'));
                try { signals.push('mem:' + (navigator.deviceMemory || 'na')); } catch (e) { signals.push('mem:na'); }
                try { signals.push('touch:' + navigator.maxTouchPoints); } catch (e) { signals.push('touch:0'); }
                break;

            default:
                signals.push('scr:' + screen.width + 'x' + screen.height);
                signals.push('cd:' + screen.colorDepth);
                break;
        }

        return signals;
    },

    // Canvas fingerprint (works on all platforms)
    getCanvasFingerprint: function () {
        try {
            var canvas = document.createElement('canvas');
            canvas.width = 200;
            canvas.height = 50;
            var ctx = canvas.getContext('2d');
            ctx.textBaseline = 'top';
            ctx.font = '14px Arial';
            ctx.fillStyle = '#f60';
            ctx.fillRect(50, 0, 100, 25);
            ctx.fillStyle = '#069';
            ctx.fillText('AILegalAsst-FP', 2, 2);
            ctx.fillStyle = 'rgba(102,204,0,0.7)';
            ctx.fillText('platform-verify', 4, 18);
            return canvas.toDataURL().slice(-80);
        } catch (e) {
            return 'no-canvas';
        }
    },

    // WebGL renderer info (GPU identifier — unique per device)
    getWebGLRenderer: function () {
        try {
            var canvas = document.createElement('canvas');
            var gl = canvas.getContext('webgl') || canvas.getContext('experimental-webgl');
            if (gl) {
                var debugInfo = gl.getExtension('WEBGL_debug_renderer_info');
                if (debugInfo) {
                    return gl.getParameter(debugInfo.UNMASKED_RENDERER_WEBGL);
                }
            }
        } catch (e) { }
        return 'na';
    },

    // Hash function
    hashString: function (str) {
        var hash = 0;
        for (var i = 0; i < str.length; i++) {
            var c = str.charCodeAt(i);
            hash = ((hash << 5) - hash) + c;
            hash = hash & hash;
        }
        return Math.abs(hash).toString(36);
    },

    // Main: Generate full platform-aware fingerprint string (legacy compat)
    generate: function () {
        var platform = this.detectPlatform();
        var browser = this.detectBrowser();
        var deviceType = this.detectDeviceType();

        var components = [];
        components.push('plt:' + platform);
        components.push('brw:' + browser);
        components.push('dev:' + deviceType);
        components.push('tz:' + Intl.DateTimeFormat().resolvedOptions().timeZone);
        components.push('lang:' + navigator.language);
        components.push('cv:' + this.getCanvasFingerprint());
        components.push('gpu:' + this.getWebGLRenderer());

        // Add platform-specific signals
        var platSignals = this.getPlatformSignals(platform);
        components = components.concat(platSignals);

        var raw = components.join('|');
        return 'fp-' + this.hashString(raw);
    },

    // Extended: Full device profile object for session security
    getDeviceProfile: async function () {
        var platform = this.detectPlatform();
        var browser = this.detectBrowser();
        var deviceType = this.detectDeviceType();
        var biometricSupported = false;
        try { biometricSupported = await this.checkBiometricSupport(); } catch (e) { }

        var components = [];
        components.push('plt:' + platform);
        components.push('brw:' + browser);
        components.push('dev:' + deviceType);
        components.push('tz:' + Intl.DateTimeFormat().resolvedOptions().timeZone);
        components.push('lang:' + navigator.language);
        components.push('cv:' + this.getCanvasFingerprint());
        components.push('gpu:' + this.getWebGLRenderer());

        var platSignals = this.getPlatformSignals(platform);
        components = components.concat(platSignals);

        var raw = components.join('|');
        var fingerprintHash = 'fp-' + this.hashString(raw);

        return {
            fingerprint: fingerprintHash,
            platform: platform,
            browser: browser,
            deviceType: deviceType,
            screenResolution: screen.width + 'x' + screen.height,
            pixelRatio: window.devicePixelRatio || 1,
            timezone: Intl.DateTimeFormat().resolvedOptions().timeZone,
            language: navigator.language,
            touchCapable: navigator.maxTouchPoints > 0,
            touchPoints: navigator.maxTouchPoints || 0,
            biometricSupported: biometricSupported,
            hardwareConcurrency: navigator.hardwareConcurrency || 0,
            colorDepth: screen.colorDepth,
            platformSignals: platSignals.join('|')
        };
    },

    requestBiometric: async function () {
        try {
            if (!window.PublicKeyCredential) return false;
            var available = await PublicKeyCredential.isUserVerifyingPlatformAuthenticatorAvailable();
            if (!available) return false;

            // Create a WebAuthn assertion challenge for platform authenticator
            var challenge = new Uint8Array(32);
            crypto.getRandomValues(challenge);

            var credential = await navigator.credentials.get({
                publicKey: {
                    challenge: challenge,
                    timeout: 60000,
                    userVerification: 'required',
                    rpId: window.location.hostname
                }
            });
            return !!credential;
        } catch (e) {
            return false;
        }
    }
};

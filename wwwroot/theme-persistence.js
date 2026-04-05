// Theme Persistence - Ensures user theme persists across all page navigations

(function() {
    'use strict';
    
    let currentUserId = null;
    let isInitialized = false;
    
    // Function to apply theme based on stored user ID
    function applyCurrentUserTheme() {
        if (currentUserId) {
            console.log('Applying theme for user:', currentUserId);
            if (window.initializeUserTheme) {
                window.initializeUserTheme(currentUserId);
            }
        }
    }
    
    // Expose function to set user ID and apply theme
    window.setThemeUser = function(userId) {
        console.log('Setting theme user:', userId);
        currentUserId = userId;
        applyCurrentUserTheme();
        isInitialized = true;
    };
    
    // Reapply theme (called by Blazor on navigation)
    window.reapplyTheme = function() {
        if (isInitialized && currentUserId) {
            console.log('Reapplying theme for user:', currentUserId);
            applyCurrentUserTheme();
        } else {
            console.log('Theme not initialized yet or no user ID');
        }
    };
    
    console.log('Theme persistence system loaded');
})();

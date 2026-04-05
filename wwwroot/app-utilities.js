// Common utility functions for JurisAI

// File download helper function (for data URLs)
window.downloadFile = function (filename, content, mimeType) {
    // If content is a data URL (legacy support)
    if (typeof content === 'string' && content.startsWith('data:')) {
        const link = document.createElement('a');
        link.href = content;
        link.download = filename;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        return;
    }
    
    // Create blob from content
    const blob = new Blob([content], { type: mimeType || 'text/plain' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = filename;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
};

// Print helper
window.printPage = function () {
    window.print();
};

// Print specific element (for legal notices)
window.printElement = function (elementId) {
    const element = document.getElementById(elementId);
    if (!element) {
        console.error('Print element not found:', elementId);
        return;
    }
    
    // Create print window
    const printWindow = window.open('', '_blank', 'width=800,height=600');
    if (!printWindow) {
        // Fallback to full page print
        window.print();
        return;
    }
    
    // Build print document with explicit styles
    printWindow.document.write(`
        <!DOCTYPE html>
        <html>
        <head>
            <title>Legal Notice</title>
            <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.1/font/bootstrap-icons.css">
            <style>
                * {
                    margin: 0;
                    padding: 0;
                    box-sizing: border-box;
                }
                body { 
                    font-family: 'Times New Roman', Times, serif;
                    font-size: 14px;
                    line-height: 1.6;
                    padding: 40px;
                    background: white;
                    color: #000;
                }
                .notice-letterhead {
                    display: flex;
                    align-items: center;
                    gap: 20px;
                    padding-bottom: 20px;
                    border-bottom: 3px double #000;
                    margin-bottom: 20px;
                    text-align: center;
                    justify-content: center;
                }
                .letterhead-logo {
                    width: 70px;
                    height: 70px;
                    background: #1a365d;
                    border-radius: 50%;
                    display: flex;
                    align-items: center;
                    justify-content: center;
                    color: white;
                    font-size: 32px;
                }
                .letterhead-text h2 {
                    font-size: 22px;
                    font-weight: bold;
                    color: #1a365d;
                    margin: 0;
                }
                .letterhead-text p {
                    font-size: 14px;
                    color: #333;
                    margin: 0;
                }
                .notice-meta {
                    display: flex;
                    justify-content: space-between;
                    margin-bottom: 20px;
                    padding: 10px 0;
                }
                .notice-meta p {
                    margin: 2px 0;
                }
                .notice-recipient {
                    margin-bottom: 20px;
                }
                .notice-recipient p {
                    margin: 2px 0;
                }
                .notice-subject {
                    margin: 20px 0;
                    padding: 10px;
                    background: #f5f5f5;
                    border-left: 4px solid #1a365d;
                }
                .notice-body {
                    margin: 20px 0;
                    text-align: justify;
                }
                .notice-body p {
                    margin-bottom: 12px;
                }
                .notice-body ul, .notice-body ol {
                    margin: 10px 0 10px 30px;
                }
                .notice-body li {
                    margin-bottom: 5px;
                }
                .notice-footer {
                    margin-top: 40px;
                }
                .notice-signature {
                    margin-top: 60px;
                    display: flex;
                    justify-content: space-between;
                }
                .signature-block {
                    text-align: center;
                }
                .signature-line {
                    width: 200px;
                    border-bottom: 1px solid #000;
                    margin-bottom: 5px;
                }
                .notice-stamp {
                    text-align: center;
                    margin-top: 20px;
                    padding: 10px;
                    border: 2px dashed #999;
                    color: #666;
                    font-style: italic;
                }
                strong {
                    font-weight: bold;
                }
                @media print {
                    body {
                        padding: 20px;
                    }
                }
            </style>
        </head>
        <body>
            ${element.innerHTML}
        </body>
        </html>
    `);
    
    printWindow.document.close();
    
    // Wait for fonts and content to load
    printWindow.onload = function() {
        setTimeout(() => {
            printWindow.focus();
            printWindow.print();
        }, 300);
    };
    
    // Fallback if onload doesn't fire
    setTimeout(() => {
        printWindow.focus();
        printWindow.print();
    }, 1000);
};

// ============================================
// THEME MANAGEMENT - SIMPLE & WORKING
// ============================================

// Get system theme preference
window.getSystemTheme = function () {
    if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
        return 'dark';
    }
    return 'light';
};

// Apply theme to the page (MAIN FUNCTION)
window.applyTheme = function (userId) {
    try {
        // Get stored preferences
        const themeKey = `theme-${userId}`;
        const contrastKey = `contrast-${userId}`;
        
        let theme = localStorage.getItem(themeKey) || 'dark';
        const highContrast = localStorage.getItem(contrastKey) === 'true';
        
        // Resolve system theme
        if (theme === 'system') {
            theme = window.getSystemTheme();
        }
        
        // Apply theme attribute
        document.documentElement.setAttribute('data-theme', theme);
        
        // Apply high contrast
        if (highContrast) {
            document.documentElement.classList.add('high-contrast');
        } else {
            document.documentElement.classList.remove('high-contrast');
        }
        
        console.log(`✓ Theme applied for user ${userId}: ${theme}, High Contrast: ${highContrast}`);
        return true;
    } catch (e) {
        console.error('Failed to apply theme:', e);
        return false;
    }
};

// Save theme preference
window.saveThemePreference = function (userId, theme, highContrast) {
    try {
        localStorage.setItem(`theme-${userId}`, theme);
        localStorage.setItem(`contrast-${userId}`, highContrast.toString());
        
        // Immediately apply the theme
        window.applyTheme(userId);
        
        console.log(`✓ Theme saved for user ${userId}: ${theme}, High Contrast: ${highContrast}`);
        return true;
    } catch (e) {
        console.error('Failed to save theme:', e);
        return false;
    }
};

// Get current theme preferences
window.getThemePreference = function (userId) {
    try {
        const theme = localStorage.getItem(`theme-${userId}`) || 'dark';
        const highContrast = localStorage.getItem(`contrast-${userId}`) === 'true';
        
        return { theme: theme, highContrast: highContrast };
    } catch (e) {
        console.error('Failed to get theme preference:', e);
        return { theme: 'dark', highContrast: false };
    }
};

// ============================================
// SCROLL LISTENER FOR NAVBAR
// ============================================

window.addScrollListener = function (dotNetRef) {
    let lastScrolled = false;
    
    const handleScroll = () => {
        const scrolled = window.scrollY > 20;
        if (scrolled !== lastScrolled) {
            lastScrolled = scrolled;
            dotNetRef.invokeMethodAsync('OnScroll', scrolled);
        }
    };
    
    window.addEventListener('scroll', handleScroll, { passive: true });
    
    // Initial check
    handleScroll();
    
    // Return cleanup function reference
    window._scrollCleanup = () => {
        window.removeEventListener('scroll', handleScroll);
    };
};

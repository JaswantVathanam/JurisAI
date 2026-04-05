using Microsoft.JSInterop;

namespace AILegalAsst.Services
{
    public class ThemeService
    {
        private readonly IJSRuntime _jsRuntime;
        private string _currentTheme = "dark";
        private bool _highContrast = false;
        private int? _currentUserId = null;

        public event Action? OnThemeChanged;

        public ThemeService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public string CurrentTheme => _currentTheme;
        public bool HighContrast => _highContrast;

        public async Task InitializeThemeAsync(int userId)
        {
            _currentUserId = userId;
            
            try
            {
                // Load theme from localStorage
                var theme = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", $"theme-{userId}");
                var contrast = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", $"contrast-{userId}");

                _currentTheme = string.IsNullOrEmpty(theme) ? "dark" : theme;
                _highContrast = contrast == "true";

                // Always apply theme to ensure it's set
                await ApplyThemeAsync();
                
                Console.WriteLine($"✓ Theme initialized: {_currentTheme} for user {userId}");
            }
            catch (Exception ex)
            {
                // Fallback to defaults
                Console.WriteLine($"✗ Error loading theme: {ex.Message}");
                _currentTheme = "dark";
                _highContrast = false;
                try
                {
                    await ApplyThemeAsync();
                }
                catch
                {
                    // Silent fail on apply
                }
            }
        }

        public async Task SetThemeAsync(string theme)
        {
            _currentTheme = theme;
            
            if (_currentUserId.HasValue)
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", $"theme-{_currentUserId}", theme);
            }
            
            await ApplyThemeAsync();
            OnThemeChanged?.Invoke();
        }

        public async Task SetHighContrastAsync(bool enabled)
        {
            _highContrast = enabled;
            
            if (_currentUserId.HasValue)
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", $"contrast-{_currentUserId}", enabled.ToString().ToLower());
            }
            
            await ApplyThemeAsync();
            OnThemeChanged?.Invoke();
        }

        public async Task ApplyThemeAsync()
        {
            try
            {
                // Determine actual theme (resolve "system" to dark/light)
                string actualTheme = _currentTheme;
                if (_currentTheme == "system")
                {
                    try
                    {
                        actualTheme = await _jsRuntime.InvokeAsync<string>("eval", 
                            "window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'");
                    }
                    catch
                    {
                        actualTheme = "dark"; // Fallback
                    }
                }

                // Apply theme via data attribute using eval for reliability
                await _jsRuntime.InvokeVoidAsync("eval", 
                    $"document.documentElement.setAttribute('data-theme', '{actualTheme}')");
                
                // Apply high contrast class
                if (_highContrast)
                {
                    await _jsRuntime.InvokeVoidAsync("eval", 
                        "document.documentElement.classList.add('high-contrast')");
                }
                else
                {
                    await _jsRuntime.InvokeVoidAsync("eval", 
                        "document.documentElement.classList.remove('high-contrast')");
                }

                Console.WriteLine($"✓ Theme applied: {actualTheme}, High Contrast: {_highContrast}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error applying theme: {ex.Message}");
            }
        }
    }
}

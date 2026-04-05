using Microsoft.JSInterop;
using AILegalAsst.Models;

namespace AILegalAsst.Services;

/// <summary>
/// Global Language Service for multi-language support across the application
/// Supports 12 Indian languages with persistent user preferences
/// </summary>
public class LanguageService
{
    private readonly IJSRuntime _jsRuntime;
    private string _currentLanguage = "en";
    private int? _currentUserId = null;

    public event Action? OnLanguageChanged;

    public LanguageService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public string CurrentLanguage => _currentLanguage;
    
    public LanguageInfo CurrentLanguageInfo => 
        LanguageSupport.SupportedLanguages.TryGetValue(_currentLanguage, out var info) 
            ? info 
            : LanguageSupport.SupportedLanguages["en"];

    /// <summary>
    /// Initialize language from user preferences stored in localStorage
    /// </summary>
    public async Task InitializeLanguageAsync(int userId)
    {
        _currentUserId = userId;
        
        try
        {
            var language = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", $"language-{userId}");
            
            if (!string.IsNullOrEmpty(language) && LanguageSupport.SupportedLanguages.ContainsKey(language))
            {
                _currentLanguage = language;
            }
            else
            {
                _currentLanguage = "en";
            }
            
            await ApplyLanguageAsync();
        }
        catch
        {
            _currentLanguage = "en";
        }
    }

    /// <summary>
    /// Set the current language and persist to localStorage
    /// </summary>
    public async Task SetLanguageAsync(string languageCode)
    {
        if (!LanguageSupport.SupportedLanguages.ContainsKey(languageCode))
        {
            languageCode = "en";
        }
        
        _currentLanguage = languageCode;
        
        if (_currentUserId.HasValue)
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", $"language-{_currentUserId}", languageCode);
        }
        
        await ApplyLanguageAsync();
        OnLanguageChanged?.Invoke();
    }

    /// <summary>
    /// Apply language settings to the document
    /// </summary>
    private async Task ApplyLanguageAsync()
    {
        try
        {
            var langInfo = CurrentLanguageInfo;
            
            // Set HTML lang attribute
            await _jsRuntime.InvokeVoidAsync("eval", $"document.documentElement.lang = '{_currentLanguage}'");
            
            // Set RTL direction for Urdu
            var direction = langInfo.IsRTL ? "rtl" : "ltr";
            await _jsRuntime.InvokeVoidAsync("eval", $"document.documentElement.dir = '{direction}'");
        }
        catch
        {
            // Ignore JS errors
        }
    }

    /// <summary>
    /// Get all available languages
    /// </summary>
    public Dictionary<string, LanguageInfo> GetAvailableLanguages()
    {
        return LanguageSupport.SupportedLanguages;
    }

    /// <summary>
    /// Get display text for a language code
    /// </summary>
    public string GetLanguageDisplay(string code)
    {
        if (LanguageSupport.SupportedLanguages.TryGetValue(code, out var info))
        {
            return $"{info.Icon} {info.NativeName}";
        }
        return "🌐 English";
    }

    /// <summary>
    /// Get short display (icon + code) for navbar
    /// </summary>
    public string GetLanguageShortDisplay()
    {
        var info = CurrentLanguageInfo;
        return $"{info.Icon} {_currentLanguage.ToUpper()}";
    }

    /// <summary>
    /// Get translated text for a key in the current language
    /// </summary>
    public string T(string key)
    {
        return Translations.Get(key, _currentLanguage);
    }

    /// <summary>
    /// Get translated text for a key in a specific language
    /// </summary>
    public string T(string key, string languageCode)
    {
        return Translations.Get(key, languageCode);
    }
}

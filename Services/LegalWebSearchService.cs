using System.Net;
using System.Text.RegularExpressions;

namespace AILegalAsst.Services;

/// <summary>
/// Service for searching legal resources online (Indian Kanoon, etc.)
/// </summary>
public class LegalWebSearchService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<LegalWebSearchService> _logger;

    public LegalWebSearchService(IHttpClientFactory httpClientFactory, ILogger<LegalWebSearchService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Search Indian Kanoon for laws, acts, sections, and case law.
    /// Returns a list of search result snippets as context for the AI agent.
    /// </summary>
    public async Task<List<OnlineLegalResult>> SearchIndianKanoonAsync(string query, int maxResults = 5, CancellationToken cancellationToken = default)
    {
        var results = new List<OnlineLegalResult>();
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            client.DefaultRequestHeaders.Add("User-Agent", "AILegalAssistant/1.0");

            var encodedQuery = Uri.EscapeDataString(query);
            var url = $"https://indiankanoon.org/search/?formInput={encodedQuery}";

            var response = await client.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Indian Kanoon search returned {StatusCode}", response.StatusCode);
                return results;
            }

            var html = await response.Content.ReadAsStringAsync(cancellationToken);
            results = ParseSearchResults(html, maxResults);
            _logger.LogInformation("Indian Kanoon search for '{Query}' returned {Count} results", query, results.Count);
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("Indian Kanoon search timed out for query: {Query}", query);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Indian Kanoon search failed for query: {Query}", query);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during Indian Kanoon search");
        }
        return results;
    }

    private List<OnlineLegalResult> ParseSearchResults(string html, int maxResults)
    {
        var results = new List<OnlineLegalResult>();

        // Parse result blocks from Indian Kanoon HTML
        var resultPattern = new Regex(
            @"<div\s+class=""result""[^>]*>.*?<div\s+class=""result_title""[^>]*>\s*<a[^>]*href=""([^""]+)""[^>]*>(.*?)</a>.*?<div\s+class=""result_text""[^>]*>(.*?)</div>",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);

        var matches = resultPattern.Matches(html);
        foreach (Match match in matches)
        {
            if (results.Count >= maxResults) break;

            var link = match.Groups[1].Value.Trim();
            var title = StripHtml(match.Groups[2].Value).Trim();
            var snippet = StripHtml(match.Groups[3].Value).Trim();

            if (!string.IsNullOrWhiteSpace(title))
            {
                if (!link.StartsWith("http"))
                    link = $"https://indiankanoon.org{link}";

                results.Add(new OnlineLegalResult
                {
                    Title = title,
                    Snippet = snippet.Length > 300 ? snippet[..300] + "..." : snippet,
                    SourceUrl = link,
                    Source = "Indian Kanoon"
                });
            }
        }

        // Fallback: try simpler pattern for title extraction
        if (results.Count == 0)
        {
            var simpleTitlePattern = new Regex(
                @"<a\s+href=""(/doc(?:fragment)?/\d+/)""[^>]*>(.*?)</a>",
                RegexOptions.Singleline | RegexOptions.IgnoreCase);

            var simpleMatches = simpleTitlePattern.Matches(html);
            foreach (Match match in simpleMatches)
            {
                if (results.Count >= maxResults) break;

                var link = match.Groups[1].Value.Trim();
                var title = StripHtml(match.Groups[2].Value).Trim();

                if (!string.IsNullOrWhiteSpace(title) && title.Length > 5)
                {
                    results.Add(new OnlineLegalResult
                    {
                        Title = title,
                        Snippet = "",
                        SourceUrl = $"https://indiankanoon.org{link}",
                        Source = "Indian Kanoon"
                    });
                }
            }
        }

        return results;
    }

    private static string StripHtml(string html)
    {
        if (string.IsNullOrEmpty(html)) return "";
        var stripped = Regex.Replace(html, @"<[^>]+>", " ");
        stripped = WebUtility.HtmlDecode(stripped);
        stripped = Regex.Replace(stripped, @"\s+", " ");
        return stripped.Trim();
    }
}

public class OnlineLegalResult
{
    public string Title { get; set; } = "";
    public string Snippet { get; set; } = "";
    public string SourceUrl { get; set; } = "";
    public string Source { get; set; } = "";
}

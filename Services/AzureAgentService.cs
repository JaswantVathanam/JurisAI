using Azure.AI.Projects;
using Azure.AI.Projects.OpenAI;
using Azure.Identity;
using OpenAI;
using OpenAI.Responses;

#pragma warning disable OPENAI001

namespace AILegalAsst.Services;

/// <summary>
/// Service for interacting with Azure AI Agent for legal assistance
/// </summary>
public class AzureAgentService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AzureAgentService> _logger;
    private AIProjectClient? _projectClient;
    private AgentRecord? _agentRecord;
    private OpenAIResponseClient? _responseClient;
    private bool _isInitialized = false;
    private string? _initializationError;
    private DateTime? _lastInitFailure;
    private static readonly TimeSpan InitRetryCooldown = TimeSpan.FromMinutes(2);
    private readonly SemaphoreSlim _initLock = new(1, 1);

    // Configuration keys
    private const string ProjectEndpointKey = "AzureAgent:ProjectEndpoint";
    private const string AgentNameKey = "AzureAgent:AgentName";
    private const string TenantIdKey = "AzureAgent:TenantId";

    public AzureAgentService(IConfiguration configuration, ILogger<AzureAgentService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Initialize the Azure Agent connection
    /// </summary>
    public async Task<bool> InitializeAsync()
    {
        if (_isInitialized)
            return true;

        // Cooldown: don't retry immediately after a failure
        if (_lastInitFailure.HasValue && DateTime.Now - _lastInitFailure.Value < InitRetryCooldown)
        {
            return false;
        }

        await _initLock.WaitAsync();
        try
        {
            // Double-check after acquiring lock
            if (_isInitialized)
                return true;
            var projectEndpoint = _configuration[ProjectEndpointKey];
            var agentName = _configuration[AgentNameKey];
            var tenantId = _configuration[TenantIdKey];

            if (string.IsNullOrEmpty(projectEndpoint) || string.IsNullOrEmpty(agentName))
            {
                _initializationError = "Azure Agent configuration is missing. Please configure ProjectEndpoint and AgentName in appsettings.json";
                _logger.LogWarning(_initializationError);
                return false;
            }

            _logger.LogInformation("Initializing Azure Agent connection to {Endpoint}", projectEndpoint);

            // Use DefaultAzureCredential which tries multiple auth methods:
            // Environment variables, Managed Identity, Visual Studio, Azure CLI, etc.
            // Falls back to InteractiveBrowser only if all others fail.
            var credentialOptions = new DefaultAzureCredentialOptions
            {
                TenantId = !string.IsNullOrEmpty(tenantId) ? tenantId : null,
                ExcludeInteractiveBrowserCredential = false
            };
            var credential = new DefaultAzureCredential(credentialOptions);

            // Connect to the Azure AI Project
            _projectClient = new AIProjectClient(
                endpoint: new Uri(projectEndpoint),
                tokenProvider: credential
            );

            // Get the agent by name with a timeout to avoid blocking the app
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            _agentRecord = await Task.Run(() => _projectClient.Agents.GetAgent(agentName), cts.Token);
            _logger.LogInformation("Agent retrieved successfully (name: {Name}, id: {Id})", _agentRecord.Name, _agentRecord.Id);

            // Get the OpenAI response client for the agent
            _responseClient = _projectClient.OpenAI.GetProjectResponsesClientForAgent(_agentRecord);

            _isInitialized = true;
            _initializationError = null;
            return true;
        }
        catch (OperationCanceledException)
        {
            _initializationError = "Azure Agent initialization timed out. The app will use offline responses. Check your Azure credentials (az login) and try restarting.";
            _logger.LogWarning(_initializationError);
            _lastInitFailure = DateTime.Now;
            return false;
        }
        catch (Exception ex)
        {
            _initializationError = $"Failed to initialize Azure Agent: {ex.Message}";
            _logger.LogError(ex, "Failed to initialize Azure Agent");
            _lastInitFailure = DateTime.Now;
            return false;
        }
        finally
        {
            _initLock.Release();
        }
    }

    /// <summary>
    /// Check if the agent service is properly initialized and ready
    /// </summary>
    public bool IsReady => _isInitialized && _responseClient != null;

    /// <summary>
    /// Get the initialization error message if any
    /// </summary>
    public string? GetInitializationError() => _initializationError;

    /// <summary>
    /// Send a message to the Azure AI Agent and get a response
    /// </summary>
    /// <param name="message">The user's message</param>
    /// <param name="context">Optional context about the user or conversation</param>
    /// <returns>The agent's response</returns>
    public async Task<AgentResponse> SendMessageAsync(string message, string? context = null, CancellationToken cancellationToken = default)
    {
        if (!_isInitialized || _responseClient == null)
        {
            // Try to initialize if not already done
            var initialized = await InitializeAsync();
            if (!initialized)
            {
                return new AgentResponse
                {
                    Success = false,
                    Message = _initializationError ?? "Azure Agent is not initialized",
                    IsFallback = true
                };
            }
        }

        try
        {
            // Build the prompt with context if provided
            var prompt = string.IsNullOrEmpty(context) 
                ? message 
                : $"Context: {context}\n\nUser Query: {message}";

            _logger.LogInformation("Sending message to Azure Agent: {Message}", message);

            // Get response from the agent with timeout + external cancellation
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(60));
            OpenAIResponse response = await Task.Run(() => _responseClient!.CreateResponse(prompt), cts.Token);
            var responseText = response.GetOutputText();

            _logger.LogInformation("Received response from Azure Agent");

            return new AgentResponse
            {
                Success = true,
                Message = responseText,
                IsFallback = false
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Azure Agent request timed out after 60 seconds");
            return new AgentResponse
            {
                Success = false,
                Message = "The AI agent took too long to respond. Please try again.",
                IsFallback = true,
                Error = "Request timed out"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting response from Azure Agent");
            return new AgentResponse
            {
                Success = false,
                Message = $"Error communicating with AI Agent: {ex.Message}",
                IsFallback = true,
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// Send a message with conversation history for better context
    /// </summary>
    public async Task<AgentResponse> SendMessageWithHistoryAsync(string message, List<ConversationMessage> history, string? userContext = null, CancellationToken cancellationToken = default)
    {
        if (!_isInitialized || _responseClient == null)
        {
            var initialized = await InitializeAsync();
            if (!initialized)
            {
                return new AgentResponse
                {
                    Success = false,
                    Message = _initializationError ?? "Azure Agent is not initialized",
                    IsFallback = true
                };
            }
        }

        try
        {
            // Build conversation context from history
            var conversationContext = string.Join("\n", history.TakeLast(5).Select(h => 
                h.IsUser ? $"User: {h.Message}" : $"Assistant: {h.Message}"));

            var fullPrompt = new System.Text.StringBuilder();
            
            if (!string.IsNullOrEmpty(userContext))
            {
                fullPrompt.AppendLine($"User Context: {userContext}");
                fullPrompt.AppendLine();
            }

            if (!string.IsNullOrEmpty(conversationContext))
            {
                fullPrompt.AppendLine("Previous conversation:");
                fullPrompt.AppendLine(conversationContext);
                fullPrompt.AppendLine();
            }

            fullPrompt.AppendLine($"Current question: {message}");

            _logger.LogInformation("Sending message with history to Azure Agent");

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(60));
            OpenAIResponse response = await Task.Run(() => _responseClient!.CreateResponse(fullPrompt.ToString()), cts.Token);
            var responseText = response.GetOutputText();

            return new AgentResponse
            {
                Success = true,
                Message = responseText,
                IsFallback = false
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Azure Agent request timed out after 60 seconds");
            return new AgentResponse
            {
                Success = false,
                Message = "The AI agent took too long to respond. Please try again.",
                IsFallback = true,
                Error = "Request timed out"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting response from Azure Agent with history");
            return new AgentResponse
            {
                Success = false,
                Message = $"Error communicating with AI Agent: {ex.Message}",
                IsFallback = true,
                Error = ex.Message
            };
        }
    }
}

/// <summary>
/// Response from the Azure AI Agent
/// </summary>
public class AgentResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsFallback { get; set; }
    public string? Error { get; set; }
    public List<string> RelatedLaws { get; set; } = new();
    public List<string> RelatedCases { get; set; } = new();
}

/// <summary>
/// Represents a message in the conversation history
/// </summary>
public class ConversationMessage
{
    public string Message { get; set; } = string.Empty;
    public bool IsUser { get; set; }
    public DateTime Timestamp { get; set; }
}

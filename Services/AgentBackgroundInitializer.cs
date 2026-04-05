namespace AILegalAsst.Services;

/// <summary>
/// Background service that initializes the Azure AI Agent after app startup,
/// so pages render immediately and the agent is ready when needed.
/// </summary>
public class AgentBackgroundInitializer : BackgroundService
{
    private readonly AzureAgentService _agentService;
    private readonly ILogger<AgentBackgroundInitializer> _logger;

    public AgentBackgroundInitializer(AzureAgentService agentService, ILogger<AgentBackgroundInitializer> logger)
    {
        _agentService = agentService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Small delay to let the app finish starting up and begin serving pages
        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

        _logger.LogInformation("Background agent initialization starting...");

        try
        {
            var result = await _agentService.InitializeAsync();
            if (result)
            {
                _logger.LogInformation("Azure Agent initialized successfully in background");
            }
            else
            {
                _logger.LogWarning("Azure Agent background initialization failed — will retry on first use");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Azure Agent background initialization error — will retry on first use");
        }
    }
}

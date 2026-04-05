using AILegalAsst.Components;
using AILegalAsst.Models;
using AILegalAsst.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Configure Phone Intelligence API settings
builder.Services.Configure<PhoneIntelAPIConfig>(
    builder.Configuration.GetSection("PhoneIntelAPI"));

// Add Memory Cache for API response caching
builder.Services.AddMemoryCache();

// Add HttpClient for Phone Intelligence API
builder.Services.AddHttpClient<PhoneIntelAPIClient>()
    .SetHandlerLifetime(TimeSpan.FromMinutes(5));

// Register Phone Intelligence API Client
builder.Services.AddScoped<PhoneIntelAPIClient>();

// Register application services
builder.Services.AddSingleton<AuthenticationService>(sp => 
    new AuthenticationService(builder.Configuration, sp.GetRequiredService<SessionSecurityService>()));
builder.Services.AddSingleton<CaseService>();
    builder.Services.AddSingleton<ScamRadarService>();
builder.Services.AddSingleton<PrecedentService>();
builder.Services.AddSingleton<LawService>();
builder.Services.AddSingleton<CybercrimeService>();
builder.Services.AddSingleton<LegalDatabaseService>();

// Register Comprehensive Lawbook Service (Constitution, Acts, Precedents, Procedures)
builder.Services.AddSingleton<LawbookService>();

// Register Legal Web Search Service (Indian Kanoon online search)
builder.Services.AddScoped<LegalWebSearchService>();
builder.Services.AddScoped<AILegalChatService>();
builder.Services.AddScoped<ChatStateService>();

// Register Azure AI Agent Service
builder.Services.AddSingleton<AzureAgentService>();
// Pre-initialize agent in background so pages load first, agent is ready when needed
builder.Services.AddHostedService<AgentBackgroundInitializer>();

// Register Security & Identity Services
builder.Services.AddSingleton<SessionSecurityService>();
builder.Services.AddSingleton<IdentityVerificationService>();

// Register Agent Case Filing Service (for Citizens)
builder.Services.AddScoped<AgentCaseFilingService>();

// Register Agent Case Management Service (for Police and Lawyers)
builder.Services.AddScoped<AgentCaseManagementService>();

// Register CaseIQ Service (Phase 1 - AI-powered investigation guidance)
builder.Services.AddScoped<CaseIQService>();

// Register Investigation Workflow Service (Phase 1 - workflow progression)
builder.Services.AddScoped<InvestigationWorkflowService>();

// Register Legal Notice Service (for Police - generates official notices)
builder.Services.AddSingleton<LegalNoticeService>();

// Register Case Timeline Service (for case progress tracking)
builder.Services.AddSingleton<CaseTimelineService>();

// Register Scam Pattern Detection Service
builder.Services.AddScoped<ScamPatternService>();

// Register Emergency SOS Service
builder.Services.AddScoped<EmergencySOSService>();

// Register FIR Draft Generation Service (Phase 1A - MahaCrimeOS matching features)
builder.Services.AddScoped<FIRDraftService>();

// Register Global Language Service (Multi-language support)
builder.Services.AddScoped<LanguageService>();

// Register Theme Service (Dark mode, high contrast, theme persistence)
builder.Services.AddScoped<ThemeService>();

// ========== PHASE 1: INTELLIGENCE GATHERING INFRASTRUCTURE ==========

// Register Data Source Integration Service (Telecom, Banking, Social Media, Police APIs)
builder.Services.AddScoped<DataSourceIntegrationService>();

// Register Intelligence Gathering Service (Master orchestrator for all intelligence)
builder.Services.AddScoped<IntelligenceGatheringService>();

// Register Phone Intelligence Service (Analyze single phone across all sources)
builder.Services.AddScoped<PhoneIntelligenceService>();

// Register Suspect Network Service (Build relationship graphs)
builder.Services.AddScoped<SuspectNetworkService>();

// ========== PHASE 1B: ADVANCED INVESTIGATION TOOLS ==========

// Register CDR Analysis Service (Parse and analyze Call Detail Records)
builder.Services.AddScoped<CDRAnalysisService>();

// Register Evidence Custody Service (Blockchain-style chain of custody tracking)
builder.Services.AddScoped<EvidenceCustodyService>();

// Register Deadline Tracker Service (BNSS statutory deadline management)
builder.Services.AddScoped<DeadlineTrackerService>();

// ========== PHASE 2: DOCUMENT EXPORT SERVICES ==========

// Register PDF Export Service (Generate professional PDFs for FIRs, Notices, Reports)
builder.Services.AddScoped<PdfExportService>();

// ========== PHASE 3: AZURE MAPS INTEGRATION ==========

// Register Azure Maps Service (Geocoding, routing, search for location-based features)
builder.Services.AddScoped<AzureMapsService>();
builder.Services.AddHttpClient(); // Required for Azure Maps REST API calls

// Register Location Tracking Service (SOS, Evidence, CDR, Investigation location tracking)
builder.Services.AddScoped<LocationTrackingService>();

// Register GeoIntelligence Service (Movement analysis, crime hotspots, location clustering)
builder.Services.AddScoped<GeoIntelligenceService>();

// Register Agent Orchestration Service (Multi-agent coordination and parallel data gathering)
builder.Services.AddScoped<AgentOrchestrationService>();

// Add API Controllers support for PDF downloads
builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

// Map API Controllers for PDF export endpoints
app.MapControllers();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

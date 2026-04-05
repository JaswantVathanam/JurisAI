# CHAPTER V – IMPLEMENTATION

## 5.1 Development Environment Setup

The development environment for the AI Legal Assistant was configured with the following tools and platforms:

| Component | Tool / Version | Purpose |
|---|---|---|
| IDE | Visual Studio Code 1.96+ | Primary code editor with Blazor extensions |
| Runtime | .NET 10.0 SDK (Preview) | Build and run the Blazor Server application |
| Language | C# 13.0 | Primary programming language |
| Version Control | Git 2.43+ / GitHub | Source code management and collaboration |
| PDF Rendering | QuestPDF 2024.12.2 | Professional PDF document generation |
| AI Agent | Azure AI Foundry | Conversational AI and NLP capabilities |
| Browser Testing | Chrome 120+, Edge 120+ | Frontend testing and debugging |
| Terminal | Windows PowerShell 7 | Build automation and task execution |

### 5.1.1 Project Initialization

The project was initialized using the .NET CLI:

```bash
dotnet new blazor -n AILegalAsst --interactivity Server
```

This command scaffolds a Blazor Server project with Interactive Server Components enabled. The project structure follows the standard ASP.NET Core Web Application layout with the addition of Blazor-specific directories (`Components/Pages`, `Components/Layout`, `Components/Shared`).

### 5.1.2 NuGet Dependencies

Four external NuGet packages were integrated into the project:

| Package | Version | Purpose | License |
|---|---|---|---|
| Azure.AI.Projects | 1.2.0-beta.4 | Azure AI Foundry SDK for creating conversational agents, managing agent threads, and processing AI responses | MIT |
| Azure.Identity | 1.17.1 | Azure Active Directory authentication for securing connections to Azure AI services using DefaultAzureCredential | MIT |
| Azure.Identity.Broker | 1.3.1 | WAM (Web Account Manager) authentication broker for Windows desktop SSO integration | MIT |
| QuestPDF | 2024.12.2 | Fluent API for generating structured PDF documents for FIRs, legal notices, and case reports | Community |

These were installed using `dotnet add package` commands:

```bash
dotnet add package Azure.AI.Projects --version 1.2.0-beta.4
dotnet add package Azure.Identity --version 1.17.1
dotnet add package Azure.Identity.Broker --version 1.3.1
dotnet add package QuestPDF --version 2024.12.2
```

## 5.2 Application Configuration

### 5.2.1 Program.cs — Service Registration and Middleware Pipeline

The application entry point (`Program.cs`) follows the minimal hosting model introduced in .NET 6+. It is responsible for three tasks: (a) registering all 30+ services with the dependency injection container, (b) configuring the HTTP request middleware pipeline, and (c) mapping component endpoints.

**Service Registration Strategy:**

Services are registered with one of two lifetimes based on their state management requirements:

- **Singleton services** (`AddSingleton<T>`): Used for services that maintain persistent data stores shared across all requests and users. These services manage data consistency for the lifetime of the application process. Examples include `AuthenticationService` (user registry), `CaseService` (case store), `LegalNoticeService` (notice store), `CaseTimelineService` (timeline events).

- **Scoped services** (`AddScoped<T>`): Used for services that operate on per-request data without maintaining persistent state. Each Blazor circuit (user session) receives its own instance. Examples include `CDRAnalysisService`, `EvidenceCustodyService`, `DeadlineTrackerService`, and all AI-related services.

The `PhoneIntelAPIClient` is registered using the typed HTTP client pattern (`AddHttpClient<PhoneIntelAPIClient>`), which integrates with `IHttpClientFactory` to manage HTTP connection pooling and handler lifecycle (5-minute rotation).

**Middleware Pipeline:**

```text
UseHttpsRedirection → UseAntiforgery → MapControllers → MapStaticAssets → MapRazorComponents
```

The middleware pipeline enforces role-based access control at the component level through the `AuthenticationService.HasAccess()` method, which validates the current user's role against the required access level for each page and navigation element. This component-level authorization approach provides fine-grained control over feature visibility based on user roles.

### 5.2.2 Configuration Files

**Application Configuration:** The application configuration manages Azure AI Foundry connection parameters, Phone Intelligence API endpoints, and environment-specific settings:

```json
{
  "AzureAI": {
    "ConnectionString": "<Azure AI Foundry Project Connection String>",
    "ModelName": "gpt-4o-mini"
  },
  "PhoneIntelAPI": {
    "TelecomAPIEndpoint": "https://api.tafcop.dgtelecom.gov.in/v1",
    "CacheExpirationMinutes": 60
  }
}
```

**Launch Configuration:** Environment-specific build profiles configure HTTPS enforcement, debug settings, and deployment parameters.

## 5.3 Module-by-Module Implementation

### 5.3.1 Authentication Module Implementation

**File:** `Services/AuthenticationService.cs`  
**Registration:** `AddSingleton<AuthenticationService>`

The authentication service is the foundational module. It is constructed with `IConfiguration` to load user credentials from the application configuration store at startup. The implementation includes:

- **User Store:** A `List<User>` populated from the configuration subsystem.
- **Login Validation:** `LoginAsync(username, password)` method checks credentials against the registered user store. Returns a boolean and sets the authenticated user context.
- **Role Checking:** `HasAccess(requiredRole)` method compares the current user's `UserRole` enum value against navigation requirements.
- **Registration:** `RegisterAsync(user)` validates uniqueness and adds the new user to the registry. For Police and Lawyer roles, sets `VerificationStatus` to `Pending`.
- **Verification Workflow:** `ApproveVerificationAsync(userId)` and `RejectVerificationAsync(userId, reason)` update the user's status and create `VerificationLog` entries.

The authentication service manages user session state within the Blazor Server circuit lifecycle, ensuring that each connected client maintains an independent authentication context.

### 5.3.2 AI Chat Module Implementation

**Files:** `Services/AzureAgentService.cs`, `Services/AgentCaseFilingService.cs`, `Services/AgentCaseManagementService.cs`, `Services/AILegalChatService.cs`, `Services/ChatStateService.cs`

The AI chat system has three operating modes:

**Mode 1 — AI-Powered Chat (Azure AI Foundry):**

```csharp
// AzureAgentService initialization
var connectionString = "endpoint;subscription;resource-group;project";
var client = new AIProjectClient(connectionString, new DefaultAzureCredential());
var agent = await client.GetAgentsClient()
    .CreateAgentAsync("gpt-4o-mini", instructions: systemPrompt);
```

The `AzureAgentService` maintains a persistent AI agent definition and creates per-user threads for conversation context. Each message is sent with the full conversation history for context retention.

**Mode 2 — Guided Case Filing (Citizens):**

The `AgentCaseFilingService` implements a state machine with five states:
1. `Greeting` — Welcome and ask for incident description
2. `CollectingDescription` — Record the narrative and ask for details
3. `CollectingDetails` — Extract location, date, parties involved
4. `IdentifyingSections` — Match keywords to IPC/BNS sections
5. `CreatingCase` — Compile data and invoke `CaseService.CreateCaseAsync()`

Each state transition validates the input, sends the conversation to Azure AI for enhanced processing, and prompts the user for the next piece of information.

**Mode 3 — Rule-Based Fallback:**

The `AILegalChatService` maintains a dictionary of keyword-to-response mappings for common legal queries (e.g., "What is FIR?", "How to file a complaint?"). This operates independently of Azure AI and serves as a fallback when the cloud service is unavailable.

### 5.3.3 Case Management Implementation

**Files:** `Services/CaseService.cs`, `Services/CaseTimelineService.cs`

The `CaseService` manages the case lifecycle with persistence:

```csharp
public async Task<Case> CreateCaseAsync(Case newCase)
{
    newCase.CaseId = GenerateUniqueCaseId();
    newCase.Status = CaseStatus.Filed;
    newCase.DateFiled = DateTime.Now;
    _cases.Add(newCase);
    await PersistCaseDataAsync(); // Persist to structured data store
    return newCase;
}
```

The `CaseTimelineService` tracks every significant event using a timeline pattern. Each event includes the event type (from 40+ defined types), a descriptive message, the performing user, and a timestamp. Events are loaded from and persisted to the structured data store on each write operation.

### 5.3.4 Phone Intelligence Implementation

**Files:** `Services/PhoneIntelAPIClient.cs`, `Services/PhoneIntelligenceService.cs`, `Services/IntelligenceGatheringService.cs`, `Services/DataSourceIntegrationService.cs`, `Services/SuspectNetworkService.cs`

The Phone Intelligence module follows a layered query pattern:

1. **Input:** Police officer enters a phone number.
2. **Orchestration:** `IntelligenceGatheringService` dispatches queries to all four data sources.
3. **API Calls:** `PhoneIntelAPIClient` executes four concurrent HTTP requests using `Task.WhenAll()`:

```csharp
var tasks = new[]
{
    GetTelecomDataAsync(phoneNumber),
    GetBankingDataAsync(phoneNumber),
    GetOSINTDataAsync(phoneNumber),
    GetPoliceDataAsync(phoneNumber)
};
await Task.WhenAll(tasks);
```

4. **Caching:** Results are cached using server-side memory cache with a 60-minute sliding expiration, keyed by phone number and source type, to minimise redundant calls to government APIs.
5. **Fallback Mechanism:** When external API endpoints are temporarily unreachable, the client activates a resilient fallback strategy with configurable retry policies and cached responses to ensure uninterrupted service.
6. **Aggregation:** `PhoneIntelligenceService` combines results into a unified `IntelligenceRecord`.
7. **Network Graphing:** `SuspectNetworkService` analyses CDR data to build relationship graphs, identify communication clusters, and recommend intervention strategies.

### 5.3.5 CDR Analysis Implementation

**File:** `Services/CDRAnalysisService.cs`

The CDR Analysis module processes call detail records through five analytical passes:

**Pass 1 — Record Parsing:** Structured input is parsed into `CDRRecord` objects with fields: CallDateTime, CallerNumber, ReceiverNumber, DurationSeconds, CallType (Voice/SMS/Data), TowerID, and Location.

**Pass 2 — Frequent Contact Extraction:**

```csharp
var contacts = records
    .GroupBy(r => r.ReceiverNumber)
    .Select(g => new FrequentContact
    {
        PhoneNumber = g.Key,
        CallCount = g.Count(),
        TotalDuration = g.Sum(r => r.DurationSeconds),
        FirstContact = g.Min(r => r.CallDateTime),
        LastContact = g.Max(r => r.CallDateTime)
    })
    .OrderByDescending(c => c.CallCount);
```

**Pass 3 — Burst Detection:** Identifies time windows where call frequency exceeds a configurable threshold. A sliding window of 30 minutes moves across the timeline, counting activities in each window. Windows exceeding 5 calls are flagged as bursts.

**Pass 4 — Location Clustering:** Groups cell tower IDs by geographic proximity and extracts the most-visited locations. This produces a heatmap-ready dataset showing areas of concentration.

**Pass 5 — Temporal Distribution:** Computes hourly and daily activity profiles that reveal patterns (e.g., a suspect who only makes calls between 2 AM and 4 AM suggests organized activity).

### 5.3.6 Evidence Chain of Custody Implementation

**File:** `Services/EvidenceCustodyService.cs`

The evidence management module implements tamper detection using cryptographic hashing:

**Hash Computation on Registration:**

```csharp
public EvidenceItem RegisterEvidence(EvidenceRegistration registration)
{
    var item = new EvidenceItem
    {
        EvidenceId = Guid.NewGuid().ToString(),
        Type = registration.Type,
        SHA256Hash = ComputeSHA256(registration.Content),
        MD5Hash = ComputeMD5(registration.Content),
        Status = EvidenceStatus.Collected,
        RegisteredAt = DateTime.Now
    };
    _evidenceItems.Add(item);
    AddCustodyLog(item.EvidenceId, CustodyAction.InitialCollection, ...);
    return item;
}
```

**Integrity Verification:**

```csharp
public EvidenceVerification VerifyIntegrity(string evidenceId)
{
    var item = _evidenceItems.Find(e => e.EvidenceId == evidenceId);
    var currentHash = ComputeSHA256(GetCurrentContent(item));
    return new EvidenceVerification
    {
        IsIntact = currentHash == item.SHA256Hash,
        OriginalHash = item.SHA256Hash,
        CurrentHash = currentHash,
        VerifiedAt = DateTime.Now
    };
}
```

**Custody Transfer:** Each transfer creates an immutable log entry recording the previous handler, current handler, action type (from 24 possible actions), timestamp, location, and a digital signature. The log is append-only, which ensures a complete audit trail.

### 5.3.7 BNSS Deadline Tracker Implementation

**File:** `Services/DeadlineTrackerService.cs`

The deadline tracker encodes 8 BNSS statutory rules as `DeadlineRule` objects:

| Rule | BNSS Section | Deadline | Description |
|---|---|---|---|
| DR-01 | S.173 | 60 days | Chargesheet filing (imprisonment ≤ 3 years) |
| DR-02 | S.173 | 90 days | Chargesheet filing (imprisonment > 3 years) |
| DR-03 | S.187 | 15 days | Maximum police custody remand |
| DR-04 | S.187 | 60 days | Maximum judicial custody (imprisonment ≤ 3 years) |
| DR-05 | S.187 | 90 days | Maximum judicial custody (imprisonment > 3 years) |
| DR-06 | S.193 | 30 days | Bail hearing for offences with imprisonment ≤ 7 years |
| DR-07 | S.230 | 45 days | Committal to Sessions Court |
| DR-08 | S.258 | 2 years | Maximum trial duration for summons cases |

The alert generation algorithm runs on each page load and evaluates all active deadlines:

```csharp
foreach (var deadline in activeDeadlines)
{
    var daysRemaining = (deadline.DueDate - DateTime.Now).TotalDays;
    deadline.Priority = daysRemaining switch
    {
        <= 0 => DeadlinePriority.Critical,   // Overdue
        <= 1 => DeadlinePriority.High,       // Due tomorrow
        <= 3 => DeadlinePriority.Medium,     // Due within 3 days
        <= 7 => DeadlinePriority.Low,        // Due within a week
        _ => DeadlinePriority.Normal         // Not yet urgent
    };
}
```

### 5.3.8 FIR and Document Generation Implementation

**Files:** `Services/FIRDraftService.cs`, `Services/PdfExportService.cs`, `Controllers/PdfExportController.cs`

**IPC/BNS Section Detection:** The FIR service maintains a keyword-to-section mapping table. When a complainant describes an incident, the service scans the description for keywords and recommends applicable sections:

```csharp
private static readonly Dictionary<string, List<string>> KeywordSections = new()
{
    ["fraud"] = new() { "IPC 420 - Cheating", "BNS 318 - Cheating" },
    ["theft"] = new() { "IPC 378 - Theft", "BNS 303 - Theft" },
    ["assault"] = new() { "IPC 351 - Assault", "BNS 131 - Assault" },
    ["cybercrime"] = new() { "IT Act 66 - Computer Offence", "IT Act 66C - Identity Theft" },
    // ... 15+ keyword categories
};
```

**PDF Generation with QuestPDF:** The `PdfExportService` uses QuestPDF's fluent API to compose structured documents:

```csharp
Document.Create(container =>
{
    container.Page(page =>
    {
        page.Size(PageSizes.A4);
        page.Margin(2, Unit.Centimetre);
        page.Header().Text("FIRST INFORMATION REPORT").Bold().FontSize(16);
        page.Content().Column(col =>
        {
            col.Item().Text($"FIR No: {fir.FIRNumber}");
            col.Item().Text($"Date: {fir.DateFiled:dd/MM/yyyy}");
            col.Item().Text($"Police Station: {fir.PoliceStation}");
            // ... structured sections
        });
    });
}).GeneratePdf(stream);
```

**API Controller:** The `PdfExportController` exposes three download endpoints:
- `GET /api/PdfExport/fir` — Downloads the most recently generated FIR as PDF.
- `GET /api/PdfExport/notice` — Downloads a legal notice as PDF.
- `GET /api/PdfExport/case/{id}` — Downloads a case summary report as PDF.

### 5.3.9 Legal Notice Generation Implementation

**File:** `Services/LegalNoticeService.cs`

Nine notice templates are implemented, each with a predefined structure containing:
- Notice title and reference number (auto-generated)
- Sender information (auto-filled from logged-in officer's details)
- Recipient directory (pre-populated lists of banks, telecom providers, ISPs, cryptocurrency exchanges, social media platforms)
- Legal authority section with relevant act and section citations
- Deadline for compliance (configurable per notice type)

The notice is composed from a template and persisted to the structured data store. Both HTML preview and PDF download are available.

### 5.3.10 Cybercrime Portal Implementation

**File:** `Services/CybercrimeService.cs`

The cybercrime portal supports 18 crime categories including Online Fraud, Phishing, Ransomware, Identity Theft, Cyberstalking, Data Breach, SIM Swap Fraud, UPI Fraud, and others. Each report captures: victim details, incident description, category, financial loss amount, suspect information, digital evidence links, and bank/wallet details for freezing requests.

The service maintains aggregate statistics (`CybercrimeStatistics`) that track total reports by category, average resolution time, and financial loss trends. An auto-generated resource section provides relevant helpline numbers and filing links for each crime category.

### 5.3.11 Scam Detection Hub Implementation

**Files:** `Services/ScamPatternService.cs`, `Services/ScamRadarService.cs`

The scam detection system operates at two levels:

**Pattern Matching (ScamPatternService):** Maintains a database of 18 scam patterns with keyword dictionaries, known phone number prefixes, typical message templates, and red flag indicators. When a user submits a phone number or message for analysis, the service computes a similarity score against each pattern and reports matches above a configurable threshold.

**Community Reporting (ScamRadarService):** Aggregates community-submitted scam reports and computes trend analysis. Citizens can report suspected scam calls/messages, and the system identifies emerging patterns based on frequency spikes in specific categories or geographic regions.

### 5.3.12 Emergency SOS Implementation

**File:** `Services/EmergencySOSService.cs`

The SOS module provides:
- **One-Touch Activation:** A single button press captures the user's GPS coordinates (via browser Geolocation API) and creates an `SOSAlert` with the current timestamp and emergency type.
- **Helpline Directory:** Pre-loaded list of emergency helplines categorized by type (Police: 100, Women: 1091, Cyber: 1930, Child: 1098, etc.).
- **Legal Rights Display:** Upon activation, immediately shows the user their legal rights in their selected language — right to remain silent, right to legal representation, right to inform a family member.
- **Nearest Lawyer Alert:** Generates a `LawyerAlert` payload that, in the production version, would be dispatched to registered lawyers in the vicinity.

### 5.3.13 Multi-Language Support Implementation

**Files:** `Services/LanguageService.cs`, `Models/Translations.cs`

The language system supports 12 Indian languages:

| Code | Language | Script |
|---|---|---|
| en | English | Latin |
| hi | Hindi | Devanagari |
| ta | Tamil | Tamil |
| te | Telugu | Telugu |
| kn | Kannada | Kannada |
| ml | Malayalam | Malayalam |
| mr | Marathi | Devanagari |
| bn | Bengali | Bengali |
| gu | Gujarati | Gujarati |
| pa | Punjabi | Gurmukhi |
| or | Odia | Odia |
| ur | Urdu | Nastaliq |

Translations are stored as a static `Dictionary<string, Dictionary<string, string>>` in `Translations.cs`, containing approximately 1,976 key-value pairs across all languages. The `LanguageService.Translate(key)` method retrieves the translation for the currently selected language, falling back to English if a translation is unavailable.

## 5.4 Frontend Implementation

### 5.4.1 Component Architecture

The Blazor frontend consists of 37 components organized into three categories:

**Page Components (29):** Each page implements a specific feature module. Pages use the `@page` directive with unique route paths and receive services through `@inject` directive.

| Page Component | Route | Role Access | Module |
|---|---|---|---|
| Login.razor | /login | All | Authentication |
| Signup.razor | /signup | All | Authentication |
| Home.razor | / | Authenticated | Dashboard |
| AIChat.razor | /aichat | Authenticated | AI Assistant |
| Cases.razor | /cases | Authenticated | Case Management |
| CaseTracker.razor | /case-tracker | Authenticated | Case Management |
| CaseIQ.razor | /caseiq | Police | Investigation |
| PhoneIntelligenceDashboard.razor | /phone-intelligence | Police | Intelligence |
| IntelligenceDashboard.razor | /intelligence | Police | Intelligence |
| CDRAnalysis.razor | /cdr-analysis | Police | CDR Analysis |
| EvidenceCustody.razor | /evidence-custody | Police | Evidence |
| DeadlineTracker.razor | /deadline-tracker | Police | Deadlines |
| LegalNotices.razor | /legal-notices | Police | Notices |
| FIRGenerator.razor | /fir-generator | Citizen/Police | FIR |
| BankFreeze.razor | /bank-freeze | Citizen/Police | FIR |
| Cybercrime.razor | /cybercrime | Citizen | Cybercrime |
| ScamHub.razor | /scam-hub | All | Scam Detection |
| ReportScam.razor | /report-scam | Citizen | Scam Detection |
| EmergencySOS.razor | /emergency-sos | Citizen | Emergency |
| Database.razor | /database | Lawyer/All | Law DB |
| Precedents.razor | /precedents | Lawyer/All | Precedents |
| Constitution.razor | /constitution | All | Law DB |
| Reports.razor | /reports | Police/Admin | Reporting |
| AdminDashboard.razor | /admin | Admin | Administration |
| UserManagement.razor | /user-management | Admin | Administration |
| Settings.razor | /settings | Authenticated | System |
| Profile.razor | /profile | Authenticated | System |
| VerificationPending.razor | /verification-pending | Police/Lawyer | Authentication |
| Error.razor | /Error | All | System |

**Shared Components (5):**
- `ChatWidget.razor` — Floating AI chat bubble embedded in MainLayout
- `CaseTimeline.razor` — Reusable timeline visualization component
- `ActionItemsList.razor` — Displays task items with status indicators
- `CopilotSuggestionCard.razor` — AI suggestion display card
- `CustomErrorBoundary.razor` — Graceful error handling wrapper

**Layout Components (3):**
- `MainLayout.razor` — Master page with navigation and content area
- `NavBar.razor` — Role-adaptive navigation sidebar
- `MainLayout.razor.css` — Scoped CSS for layout styling

### 5.4.2 Theme System Implementation

The theme system provides three display modes:

- **Light Mode:** White backgrounds, dark text, standard Bootstrap colours.
- **Dark Mode:** Dark grey backgrounds (#1a1a2e, #16213e), light text, accent colours adjusted for dark surfaces.
- **High Contrast Mode:** Black backgrounds, white text, bright accent colours, and thicker borders for accessibility compliance.

Theme persistence is implemented using JavaScript interop (`localStorage.setItem`) called from the `ThemeService`. The selected theme is stored in the browser's local storage and restored on page load, ensuring consistency across sessions.

### 5.4.3 CSS Architecture

The project uses 50+ CSS files organized by module. Key stylesheets include:

| File | Purpose | Lines (approx.) |
|---|---|---|
| app.css | Base application styles, root variables | 600+ |
| ai-chat.css / aichat-v2.css | AI chat interface styling | 800+ |
| admin-dashboard.css / admin-dashboard-v2.css | Admin panel layouts | 500+ |
| auth-pages-v2.css | Login/Signup modern UI | 400+ |
| case-tracker-v2.css | Case timeline and tracker | 400+ |
| phone-intelligence.css | Intelligence dashboard | 300+ |
| emergency-sos.css | SOS activation UI | 200+ |
| evidence-custody.css | Evidence management | 200+ |

Each CSS file follows a BEM-inspired naming convention adapted for Blazor components. CSS custom properties (variables) defined in `app.css` are used for theming:

```css
:root {
    --bg-primary: #ffffff;
    --text-primary: #212529;
    --accent-color: #3498db;
}
[data-theme="dark"] {
    --bg-primary: #1a1a2e;
    --text-primary: #e2e8f0;
    --accent-color: #60a5fa;
}
```

## 5.5 Coding Standards Followed

The following coding standards were observed throughout the implementation:

1. **Naming Conventions:** PascalCase for public members, camelCase prefixed with underscore for private fields (e.g., `_caseCollection`, `_userContext`). Async method names suffixed with `Async`.
2. **Dependency Injection:** All inter-service dependencies are resolved through constructor injection. No service locator pattern used.
3. **Null Safety:** Nullable reference types enabled project-wide (`<Nullable>enable</Nullable>`). Null-conditional operators (`?.`) and null-coalescing operators (`??`) used throughout.
4. **Async/Await:** All IO-bound operations use asynchronous patterns. `Task.WhenAll()` used for parallel operations (e.g., Phone Intelligence API calls).
5. **Error Handling:** Try-catch blocks at service boundaries with descriptive error messages. Custom error boundaries for Blazor component trees.
6. **Code Organization:** One service per file. One model per conceptual entity (related enums in the same file). One page per feature route.

> **Academic Course Reference:** The implementation patterns described in this chapter — dependency injection, service-oriented architecture, and the ASP.NET Core middleware pipeline — are drawn from concepts in the **Web Application Development (CSXXXX)** and **Software Design Patterns (CSXXXX)** courses.

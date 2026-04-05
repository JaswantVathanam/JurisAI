# CHAPTER IV – SYSTEM DESIGN

## 4.1 Overall System Architecture

The AI Legal Assistant follows a **layered service-oriented architecture** built on the ASP.NET Core dependency injection framework. The architecture separates presentation, business logic, and data access into distinct layers, enabling independent development, testing, and future scaling of each layer.

The system comprises four primary architectural layers:

**Presentation Layer (Blazor Components):** 29 interactive Razor page components and 5 shared components handle user interaction. Each page component receives services through constructor injection and communicates with the user through Blazor's SignalR-based real-time rendering pipeline. The pages are organized by functional domain and enforce role-based visibility through the AuthenticationService.

**Service Layer (30 Services):** The business logic is encapsulated in 30 independent services registered with the ASP.NET Core dependency injection container. Services are registered with appropriate lifetimes — Singleton for stateful services that maintain persistent data stores (AuthenticationService, CaseService, CaseTimelineService, LegalNoticeService, CybercrimeService, ScamRadarService, LawService, PrecedentService), and Scoped for request-specific services (all others). Each service has a clearly defined responsibility boundary, reducing coupling and enabling isolated testing.

**Data Layer:** The system employs a layered data persistence architecture with structured file-based storage for critical datasets. Case records, timeline events, and legal notices are persisted to structured data files that are loaded at startup and synchronised on write operations. User credentials are managed through the application configuration subsystem. This architecture was deliberately designed to facilitate horizontal scaling through cloud database services (Azure Cosmos DB or SQL Server) by centralising data access within service methods.

**External Integration Layer:** The platform integrates with two categories of external services: (a) Azure AI Foundry for conversational intelligence, accessed through the Azure.AI.Projects SDK, and (b) Phone Intelligence APIs for telecom, banking, OSINT, and police database queries, accessed through a typed HTTP client with response caching. The Phone Intelligence API layer connects to government data sources (TAFCOP, CCTNS, NPCI, CERT-In) through secure inter-agency communication protocols.

<!-- 
[PLACEHOLDER: Figure 4.1 – Overall System Architecture Diagram]
Draw a layered architecture diagram using Draw.io showing:
- Top layer: User roles (Citizen, Lawyer, Police, Admin) with browser icons
- Presentation layer: Blazor Components (grouped by role)
- Service layer: 30 services grouped by domain (Case, AI, Intelligence, Evidence, Legal, etc.)
- Data layer: Persistent data stores + Structured file storage
- External layer: Azure AI Foundry + Phone Intel APIs (Telecom, Banking, OSINT, Police DB)
- Cross-cutting concerns: Authentication, Language, Theme on the side
Use arrows to show dependency direction.
Dimensions: Full page, landscape orientation.
-->

## 4.2 Module Design

The system is decomposed into 13 functional modules. Each module encapsulates a cohesive set of features, the services that implement them, and the data models they operate on.

### 4.2.1 Module 1: Authentication and User Management

**Purpose:** Manages user registration, login, session management, role-based access control, and account verification workflows for Police and Lawyer accounts.

**Services:** `AuthenticationService` (Singleton)

**Models:** `User`, `UserRole`, `VerificationLog`, `VerificationStatus`

**Key Design Decisions:**
- The authentication service maintains a registry of users loaded from the application configuration at startup, with the capability to register new users at runtime.
- Police accounts require verification of Police ID Number and Service ID Card upload. Lawyer accounts require Bar Council Registration Number and certificate upload. Admin accounts must verify both.
- Verification requests are displayed in the Admin Dashboard. The admin can approve or reject each request, and every decision is logged in the VerificationLog with the admin's name, timestamp, and comment.
- The `HasAccess()` method checks the current user's role against the required role for each page and navigation item, enforcing role-based access control.

**Role-Specific Behaviour:**
- *Citizen:* Immediate access upon registration. No verification required.
- *Police:* Registered in "Pending" verification state. Redirected to a waiting page until an administrator approves the account.
- *Lawyer:* Same verification workflow as Police, requiring Bar Council certificate.
- *Admin:* Pre-configured accounts only; no self-registration.

<!-- 
[PLACEHOLDER: Figure 4.2 – Authentication Flow - Activity Diagram]
Draw an activity diagram showing:
Start → Registration Form → [Role?] 
  → Citizen: Grant immediate access
  → Police/Lawyer: Submit documents → Admin Verification Queue → [Approved?]
    → Yes: Grant role-specific access
    → No: Notify rejection reason
  → Admin: Not available for self-registration
Dimensions: Full page width, approximately 500px height.
-->

### 4.2.2 Module 2: Case Management and Workflow

**Purpose:** Manages the complete lifecycle of legal cases — creation, status tracking, workflow progression through investigation stages, timeline events, and role-based case filtering.

**Services:** `CaseService` (Singleton), `CaseTimelineService` (Singleton)

**Models:** `Case`, `CaseType` (5 types), `CaseStatus` (11 statuses), `CaseDocument`, `CaseWorkflowStep`, `CaseTimelineEvent`, `TimelineEventType` (40+ types), `MilestoneType`

**Key Design Decisions:**
- Cases support five types: Cybercrime, CivilDispute, CriminalCase, FamilyLaw, PropertyDispute.
- The workflow engine defines 10 progression stages: Filed → FIRRegistered → UnderInvestigation → ChargesheetFiled → CourtHearing → TrialInProgress → ArgumentsCompleted → JudgmentReserved → JudgmentDelivered → CaseClosed.
- The timeline system tracks 40+ event types including FIR lodging, evidence collection, witness statements, hearing dates, order copies, and notice dispatches.
- Citizens see only their own cases. Lawyers see cases where they are assigned as counsel. Police see all cases. This filtering is implemented in `GetCasesByRoleAsync()`.
- Case data is persisted to structured storage files to ensure data durability across application restarts.

**Role-Specific Behaviour:**
- *Citizen:* Can create cases (via form or AI chat). Views only own cases. Can track progress.
- *Police:* Views all cases. Can update workflow stage, add timeline events, assign counsels.
- *Lawyer:* Views assigned cases. Can add timeline events and notes.

### 4.2.3 Module 3: AI Chat Assistant

**Purpose:** Provides a conversational AI interface powered by Azure AI Foundry that adapts its behaviour based on the user's role — guided case filing for Citizens, case management commands for Police and Lawyers.

**Services:** `AzureAgentService` (Singleton), `AgentCaseFilingService` (Scoped), `AgentCaseManagementService` (Scoped), `AILegalChatService` (Scoped), `ChatStateService` (Scoped)

**Models:** `ChatMessage`, `ChatSession`

**Key Design Decisions:**
- The AzureAgentService connects to Azure AI Foundry using the Azure.AI.Projects SDK. It maintains a persistent agent connection and supports message-with-history for contextual conversations.
- For Citizens, the AgentCaseFilingService implements a multi-step conversational filing workflow: greeting → incident description → detail collection → section identification → case creation. Each step validates the input and prompts for missing details.
- For Police and Lawyers, the AgentCaseManagementService implements NLP intent detection to interpret commands such as "show my pending cases," "update case status," "search cases by crime type."
- The AILegalChatService provides a rule-based fallback when the Azure AI service is unavailable, offering pre-defined responses for common legal queries.
- A floating ChatWidget component is embedded in the MainLayout, appearing on all authenticated pages except the dedicated AI Chat page.

**Role-Specific Behaviour:**
- *Citizen:* Conversational case filing with guided prompts. Legal Q&A.
- *Police:* Case management through natural language commands. Investigation queries.
- *Lawyer:* Case lookup, precedent queries, and case management through chat.

### 4.2.4 Module 4: FIR and Legal Document Generation

**Purpose:** Generates First Information Report drafts with automatic detection of applicable IPC/BNS sections, bank freeze request letters, and professional PDF documents.

**Services:** `FIRDraftService` (Scoped), `PdfExportService` (Scoped)

**Models:** `FIRDraft`, `FIRCrimeType` (15 types), `FIRCrimeSubType` (15 subtypes), `FIRStatus`

**Key Design Decisions:**
- The FIR generator analyses the complaint description and maps keywords and patterns to applicable sections under both the Indian Penal Code (IPC) and the Bharatiya Nyaya Sanhita (BNS), displaying the corresponding section numbers and maximum penalties.
- The bank freeze request module generates formatted letters addressed to bank compliance officers, pre-filled with the complainant's details and the suspect's account information.
- PDF generation uses the QuestPDF library, producing documents with proper headers, section formatting, and official styling. The PdfExportController exposes three endpoints: `/api/PdfExport/fir`, `/api/PdfExport/notice`, and `/api/PdfExport/case/{id}`.
- FIR drafts include multi-language translation capability, allowing citizens to translate the generated FIR content into their preferred language using the LanguageService.

### 4.2.5 Module 5: Phone Intelligence and Network Analysis

**Purpose:** Aggregates intelligence from four external data sources (Telecom, Banking, OSINT, Police Database) for a given phone number and builds criminal network graphs from communication patterns.

**Services:** `PhoneIntelAPIClient` (Scoped), `PhoneIntelligenceService` (Scoped), `IntelligenceGatheringService` (Scoped), `DataSourceIntegrationService` (Scoped), `SuspectNetworkService` (Scoped)

**Models:** `IntelligenceRecord`, `TelecomIntelligence`, `BankingIntelligence`, `OSINTIntelligence`, `PoliceIntelligence`, `PhoneIntelAPIConfig`, `TelecomDataResponse`, `BankingDataResponse`, `OSINTDataResponse`, `PoliceDataResponse` (and 15+ sub-models)

**Key Design Decisions:**
- The PhoneIntelAPIClient is registered as a typed HTTP client with server-side caching (60-minute TTL) to reduce redundant API calls. The client connects to government data sources (TAFCOP, CCTNS, NPCI, CERT-In) through authenticated REST endpoints with comprehensive error handling and fallback mechanisms.
- All four API calls (telecom, banking, OSINT, police) execute in parallel using `Task.WhenAll()`, reducing total response time.
- The SuspectNetworkService builds network graphs from CDR data, identifying communication hubs, detecting clusters, predicting next activity, and recommending intervention strategies. AI-powered analysis uses Azure AI Foundry to interpret the network topology.
- Intelligence records aggregate data from all four sources into a unified `IntelligenceRecord` model with sub-sections for each source type.

<!-- 
[PLACEHOLDER: Figure 4.3 – Phone Intelligence Data Flow Diagram]
Draw a DFD showing:
- Input: Phone Number (from Police user)
- Process 1: PhoneIntelAPIClient → calls 4 APIs in parallel
- Data Stores: Telecom DB, Banking DB, OSINT DB, Police DB
- Process 2: IntelligenceGatheringService → aggregates responses
- Output: Unified IntelligenceRecord → displayed on PhoneIntelligenceDashboard
- Side process: SuspectNetworkService → builds network graph
Dimensions: Full page width, approximately 400px height.
-->

### 4.2.6 Module 6: CDR Analysis

**Purpose:** Parses and analyses Call Detail Records to identify communication patterns, frequent contacts, burst activities, location clusters, and temporal distributions.

**Services:** `CDRAnalysisService` (Scoped)

**Models:** `CDRRecord`, `CDRAnalysis`, `CallPattern`, `FrequentContact`, `BurstActivity`, `LocationCluster`, `HourlyActivity`, `DailyActivity`

**Key Design Decisions:**
- The CDR parser accepts structured data input (simulating CSV file upload) and extracts call records with fields: DateTime, CallerNumber, ReceiverNumber, Duration, Type, TowerID, Location.
- **Frequent Contact Analysis** counts the number of interactions with each unique phone number and ranks them by frequency.
- **Burst Activity Detection** identifies periods of abnormally high call/SMS frequency within short time windows (configurable threshold), which may indicate coordination for criminal activities.
- **Location Clustering** groups cell tower locations to identify geographic areas where the suspect is most active. This is useful for establishing presence patterns and travel routes.
- **Hourly and Daily Distribution** analyses compute call volume by hour-of-day and day-of-week, producing activity profiles that investigators can use to understand operational patterns.

### 4.2.7 Module 7: Evidence Chain of Custody

**Purpose:** Manages the lifecycle of physical and digital evidence items with tamper-proof integrity verification using cryptographic hash functions.

**Services:** `EvidenceCustodyService` (Scoped)

**Models:** `EvidenceItem`, `EvidenceType` (21 types), `EvidenceStatus`, `CustodyLog`, `CustodyAction` (24 actions), `EvidenceVerification`

**Key Design Decisions:**
- Evidence is registered with a type classification from 21 categories (HardDrive, MobilePhone, CCTV_Footage, FinancialRecord, CallDetailRecord, etc.).
- Upon registration, two hash values are computed: SHA-256 (primary) and MD5 (secondary). These are stored as the baseline for future integrity verification.
- Every custody action (collection, transfer, storage, analysis, court submission, return, destruction) is logged in an immutable `CustodyLog` with fields: Timestamp, PreviousHandler, CurrentHandler, Action, Description, Location, and DigitalSignature.
- The integrity verification function recomputes the current hash and compares it against the registered baseline. If any discrepancy is detected, the evidence is flagged as potentially tampered. This mechanism follows the blockchain-inspired approach recommended by Cosic and Baca (2010).

<!-- 
[PLACEHOLDER: Figure 4.4 – Evidence Chain of Custody - Sequence Diagram]
Draw a sequence diagram showing:
Actors: Police Officer, EvidenceCustodyService, HashEngine
1. Officer → RegisterEvidence(item details)
2. Service → HashEngine: ComputeSHA256(content)
3. HashEngine → Service: hash value
4. Service → HashEngine: ComputeMD5(content)
5. HashEngine → Service: hash value
6. Service → Store (EvidenceItem + hashes)
7. Service → CreateCustodyLog(initial collection entry)
... Later ...
8. Officer → VerifyIntegrity(evidenceId)
9. Service → HashEngine: RecomputeSHA256(current content)
10. Service → Compare(stored hash vs current hash)
11. Service → Return verification result (pass/fail)
Dimensions: Full page width, approximately 500px height.
-->

### 4.2.8 Module 8: BNSS Statutory Deadline Tracker

**Purpose:** Tracks investigation deadlines mandated by the Bharatiya Nagarik Suraksha Sanhita with automated alert generation.

**Services:** `DeadlineTrackerService` (Scoped)

**Models:** `CaseDeadline`, `DeadlineType`, `BNSSDeadlineType`, `DeadlineStatus`, `DeadlinePriority`, `DeadlineAlert`, `BNSSDeadlineRules`, `DeadlineRule`

**Key Design Decisions:**
- The BNSS prescribes specific timelines that are encoded as rules in `BNSSDeadlineRules`: chargesheet filing within 60 days (offences with imprisonment up to 3 years) or 90 days (offences with imprisonment above 3 years), maximum police custody remand of 15 days, maximum judicial custody of 60 or 90 days, and others.
- Upon case creation, `CreateDeadlinesForCaseAsync()` automatically generates a set of applicable deadlines based on the case type and the offences charged.
- The alert system generates notifications at four intervals: 7 days before deadline (Low priority), 3 days (Medium), 1 day (High), and overdue (Critical).
- Officers can request deadline extensions with documented justification. Extension requests are logged with the original deadline, new deadline, and reason for extension.
- A calendar view displays all deadlines with colour-coded severity indicators.

### 4.2.9 Module 9: Legal Notice Generation

**Purpose:** Generates nine types of official legal notices with pre-defined recipient directories and auto-filled officer details.

**Services:** `LegalNoticeService` (Singleton)

**Models:** `LegalNotice`, `LegalNoticeType`, `NoticeRecipient`, `LegalNoticeRequest`

**Supported Notice Types:**
1. Bank Account Freeze Notice
2. CDR Data Request Notice
3. Social Media Content Takedown Notice
4. ISP IP Address Request Notice
5. Payment Gateway Freeze Notice
6. Cryptocurrency Exchange Freeze Notice
7. Witness Summons
8. Court Summons
9. General Investigation Notice

### 4.2.10 Module 10: Cybercrime Portal

**Purpose:** Provides a structured cybercrime reporting and tracking system with 18 crime categories.

**Services:** `CybercrimeService` (Singleton)

**Models:** `CybercrimeReport`, `CybercrimeType` (18 types), `ReportStatus`, `CybercrimeUpdate`, `CybercrimeResource`, `CybercrimeStatistics`

### 4.2.11 Module 11: Scam Detection Hub

**Purpose:** Maintains a pattern database of known scam types with AI-powered analysis and a community reporting mechanism.

**Services:** `ScamPatternService` (Scoped), `ScamRadarService` (Singleton)

**Models:** `ScamPattern`, `ScamMatch`, `ScamAnalysisResult`, `AIScamAnalysis`, `ScamStatistics`, `ScamCategory` (18 categories), `ScamReport`, `CommunityScamReport`, `CommunityScamTrend`

### 4.2.12 Module 12: Emergency SOS

**Purpose:** Provides citizens with immediate assistance tools during emergencies including GPS location sharing, legal rights display, and helpline directories.

**Services:** `EmergencySOSService` (Scoped)

**Models:** `SOSAlert`, `EmergencyContact`, `LawyerAlert`, `LegalRight`, `Helpline`, `SOSConfiguration`, `SOSHistory`, `EmergencyType` (10 types), `SOSStatus`

### 4.2.13 Module 13: Multi-Language and Accessibility

**Purpose:** Provides UI localization across 12 Indian languages and theme management with accessibility options.

**Services:** `LanguageService` (Scoped), `ThemeService` (Scoped)

**Models:** `LanguageSupport`, `LanguageInfo`, `Translations` (static dictionary class)

## 4.3 Database Design

### 4.3.1 Data Persistence Strategy

The AI Legal Assistant employs a layered data persistence strategy with structured file-based storage for critical datasets and application-level caching for transient operational data. This approach was designed to support both rapid development iteration and straightforward migration to enterprise database platforms in production environments.

| Collection | Persistence Strategy | Storage Mechanism |
|---|---|---|
| Users | Configuration-based | Application configuration store |
| Cases | File-backed persistent | Structured data files |
| Case Timelines | File-backed persistent | Structured data files |
| Legal Notices | File-backed persistent | Structured data files |
| Laws and Precedents | Static reference data | Pre-loaded legal corpus |
| Evidence Items | Application-level persistent | Service-managed data store |
| Deadlines | Application-level persistent | Service-managed data store |
| Cybercrime Reports | Application-level persistent | Service-managed data store |
| CDR Analyses | Application-level persistent | Service-managed data store |
| Intelligence Records | Application-level persistent | Service-managed data store |
| Chat Sessions | Session-scoped transient | Per-circuit session store |
| Scam Reports | Application-level persistent | Service-managed data store |
| SOS Alerts | Application-level persistent | Service-managed data store |

### 4.3.2 Entity Relationship Diagram

The following ER diagram represents the conceptual data model as if the system were backed by a relational database. Each entity corresponds to a model class in the system.

<!-- 
[PLACEHOLDER: Figure 4.5 – Entity Relationship Diagram]
Draw an ER diagram using Draw.io with the following entities and relationships:

Entities:
- User (UserId PK, Username, Email, Role, FullName, VerificationStatus)
- Case (CaseId PK, Title, Type, Status, FiledById FK→User, AssignedLawyerId FK→User)
- CaseTimelineEvent (EventId PK, CaseId FK→Case, EventType, DateTime, Description)
- EvidenceItem (EvidenceId PK, CaseId FK→Case, Type, SHA256Hash, MD5Hash, Status)
- CustodyLog (LogId PK, EvidenceId FK→EvidenceItem, Action, Handler, DateTime)
- CaseDeadline (DeadlineId PK, CaseId FK→Case, Type, DueDate, Status)
- LegalNotice (NoticeId PK, CaseId FK→Case, NoticeType, GeneratingOfficerId FK→User)
- FIRDraft (FIRId PK, CaseId FK→Case, CreatedById FK→User, CrimeType, Status)
- CybercrimeReport (ReportId PK, VictimId FK→User, CrimeType, Status)
- IntelligenceRecord (RecordId PK, PhoneNumber, CreatedById FK→User)
- CDRAnalysis (AnalysisId PK, CaseId FK→Case, RecordCount, AnalysisDate)
- ChatSession (SessionId PK, UserId FK→User, StartTime)

Relationships:
- User 1→M Case (filed by)
- User 1→M Case (assigned lawyer)
- Case 1→M CaseTimelineEvent
- Case 1→M EvidenceItem
- EvidenceItem 1→M CustodyLog
- Case 1→M CaseDeadline
- Case 1→M LegalNotice
- Case 1→1 FIRDraft
- User 1→M CybercrimeReport
- User 1→M ChatSession

Dimensions: Full page, landscape orientation.
-->

## 4.4 User Interface Design

### 4.4.1 UI Design Principles

The user interface was designed following these principles:
- **Role-Adaptive Layout:** The navigation sidebar and dashboard content change based on the logged-in user's role. Police officers see investigation tools. Citizens see case filing and help tools. Lawyers see precedent search and case access.
- **Glassmorphism Navigation:** The top navigation bar uses a frosted-glass effect (`backdrop-filter: blur()`) that remains visible while scrolling, providing persistent access to navigation without obstructing content.
- **Dark/Light/High-Contrast Modes:** Three theme variants accommodate different usage environments — fieldwork (dark mode for low-light), office work (light mode), and accessibility requirements (high-contrast mode).
- **Responsive Design:** All layouts use Bootstrap 5's grid system with custom breakpoints to function across devices from 320px (mobile) to 2560px (4K desktop).
- **Consistent Card Patterns:** Information is presented in card containers with consistent header, body, and action sections across all modules.

### 4.4.2 User Flow Diagrams

<!-- 
[PLACEHOLDER: Figure 4.6 – User Flow Diagram: Citizen Journey]
Draw a user flow diagram showing a citizen's path through:
Login → Dashboard → [Choose action]
  → File Case: AI Chat → Guided conversation → Case created → Case Tracker
  → Generate FIR: FIR Generator → Fill details → Preview → Download PDF
  → Emergency: SOS → Activate → GPS captured → Helplines shown
  → Report Scam: Scam Hub → Submit report → View trends
  → Report Cybercrime: Cybercrime Portal → Submit report → Track status
Dimensions: Full page width, approximately 500px height.
-->

<!-- 
[PLACEHOLDER: Figure 4.7 – User Flow Diagram: Police Officer Journey]
Draw a user flow diagram showing a police officer's path:
Login → Verification waiting (if new) → Dashboard → [Choose action]
  → Investigate Case: CaseIQ → AI suggestions → Accept/Reject → Actions
  → Phone Intel: Intelligence Dashboard → Search phone → View results → Network graph
  → CDR Analysis: Upload CDR → Pattern analysis → Burst detection → Location map
  → Evidence: Register → Hash computed → Transfer custody → Verify integrity
  → Deadlines: View calendar → Track deadlines → Request extension
  → Legal Notices: Select template → Fill details → Generate → Send
  → AI Chat: Ask investigation queries → Get AI recommendations
Dimensions: Full page width, approximately 500px height.
-->

<!-- 
[PLACEHOLDER: Figure 4.8 – Use Case Diagram: All Actors]
Draw a UML Use Case diagram with 4 actors and their use cases:

Actor: Citizen
- Register Account, Login, File Case via AI Chat, Generate FIR, Download FIR PDF
- Bank Freeze Request, Activate Emergency SOS, Report Cybercrime, Report Scam
- Track Case, Browse Laws, Search Precedents, AI Chat

Actor: Police Officer
- Login, View All Cases, Investigate with CaseIQ, Phone Intelligence Search
- CDR Analysis, Register Evidence, Verify Evidence, Track Deadlines
- Generate Legal Notices, View Cybercrime Reports, AI Chat

Actor: Lawyer
- Login (requires Bar Council verification), View Assigned Cases
- Search Precedents, Browse Laws, AI Chat (case management commands)

Actor: Admin
- Login, Verify Police/Lawyer Accounts, View System Reports
- Manage Users, System Settings

Dimensions: Full page, landscape orientation.
-->

<!-- 
[PLACEHOLDER: Figure 4.9 – Class Diagram: Core Models]
Draw a UML Class Diagram showing key model classes with attributes and relationships:
- User (attributes: UserId, Username, Role, VerificationStatus)
- Case (attributes: CaseId, Title, Type, Status, FiledBy, AssignedLawyer)
- EvidenceItem (attributes: EvidenceId, Type, SHA256Hash, MD5Hash)
- CustodyLog (attributes: LogId, Action, Handler, DateTime)
- IntelligenceRecord (attributes: PhoneNumber, TelecomData, BankingData, OSINTData, PoliceData)
- CDRRecord (attributes: DateTime, Caller, Receiver, Duration, Tower, Location)
Dimensions: Full page width, approximately 600px height.
-->

<!-- 
[PLACEHOLDER: Figure 4.10 – Deployment Diagram]
Draw a UML Deployment diagram showing:
- Client node: Browser (Chrome/Firefox/Edge) – Blazor UI rendering
- Server node: ASP.NET Core Host → Blazor Server (SignalR WebSocket)
  - Contains: 30 Services, Razor Components, Static Files
- Azure node: Azure AI Foundry (Agent API)
- External nodes: Telecom API, Banking API, OSINT API, Police DB API
- Storage: Persistent data stores → Azure Cosmos DB (production scaling)
Dimensions: Full page width, approximately 400px height.
-->

<!-- 
[PLACEHOLDER: Figure 4.11 – Data Flow Diagram Level 0 (Context Diagram)]
Draw a Level 0 DFD showing:
- External entities: Citizen, Police Officer, Lawyer, Admin, Azure AI, Phone Intel APIs
- Central process: AI Legal Assistant System
- Data flows: Case filings, FIR drafts, Intelligence queries, Evidence records, 
  Legal notices, Chat messages, Verification requests
Dimensions: Full page width, approximately 350px height.
-->

<!-- 
[PLACEHOLDER: Figure 4.12 – Data Flow Diagram Level 1]
Draw a Level 1 DFD expanding the central process into sub-processes:
P1: Authentication & User Management
P2: Case Management
P3: AI Chat & Case Filing
P4: FIR & Document Generation
P5: Phone Intelligence
P6: CDR Analysis
P7: Evidence Custody
P8: Deadline Tracking
P9: Legal Notice Generation
P10: Cybercrime Reporting
P11: Scam Detection
P12: Emergency SOS
P13: Law & Precedent Database
With data flows between processes and to external entities.
Dimensions: Full page, landscape orientation.
-->

> **Academic Course Reference:** The UML diagrams (use case, sequence, activity, class, deployment) prepared in this chapter follow the notation standards studied in the **Software Engineering (CS3XXX)** course. The data flow diagram methodology was covered in the **Database Management Systems (CS3XXX)** course.

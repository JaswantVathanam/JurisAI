# CHAPTER III – SYSTEM ANALYSIS

## 3.1 Requirements Gathering

The requirements for the AI Legal Assistant were gathered through a multi-pronged approach:

**Source 1: Analysis of Existing Legal Processes**
The operational workflows of police stations, courts, and citizen interactions with law enforcement were studied through publicly available documentation from the NCRB, the Department of Justice, and the e-Courts project. The process flow for FIR registration, evidence handling, investigation timelines, and case progression through courts provided the basis for defining functional requirements.

**Source 2: Legislative Framework Analysis**
The newly enacted Bharatiya Nyaya Sanhita (BNS), Bharatiya Nagarik Suraksha Sanhita (BNSS), and Bharatiya Sakshya Adhiniyam (BSA) were analyzed to identify statutory requirements that the platform must encode. Specific provisions regarding investigation timelines (BNSS Sections 173, 167, 436A), evidence handling standards, and FIR requirements were documented.

**Source 3: NCRB Crime Statistics**
The NCRB "Crime in India" annual reports were analyzed to identify the categories of crimes most frequently reported, the typical investigation bottlenecks, and the technological gaps in current law enforcement workflows.

**Source 4: Stakeholder Persona Development**
Four user personas were developed to represent the target stakeholders:

- **Citizen Persona (Priya, Age 28, Bengaluru):** Received threatening calls from an unknown number after an online purchase. Knows something wrong happened but is unsure what laws apply or how to file a complaint. Speaks Tamil and English. Needs guidance to file an FIR without visiting a police station.

- **Police Officer Persona (Inspector Rajesh, Cyber Cell, Mumbai):** Handles 15-20 cybercrime cases simultaneously. Spends 3-4 hours daily manually correlating CDR data, bank statements, and social media information. Often misses BNSS deadlines due to caseload. Needs a unified dashboard for investigation.

- **Lawyer Persona (Advocate Meera, Delhi High Court):** Represents victims of cybercrime and financial fraud. Requires quick access to relevant precedents, applicable sections under both IPC and BNS, and case status tracking. Needs to reference both old and new legal frameworks.

- **Admin Persona (DSP Sharma, State Cyber Cell Head):** Oversees verification of newly registered police and lawyer accounts. Requires audit trails for all verification decisions.

## 3.2 Functional Requirements

The functional requirements are organized by user role to clearly delineate the feature sets available to each stakeholder.

### 3.2.1 Citizen Functional Requirements

| FR ID | Requirement | Priority |
|---|---|---|
| FR-C01 | The system shall allow citizens to register and create accounts with identity verification | High |
| FR-C02 | The system shall provide a conversational AI interface for guided case filing through natural language interaction | High |
| FR-C03 | The system shall generate FIR drafts with automatically detected applicable IPC/BNS sections based on the incident description | High |
| FR-C04 | The system shall allow citizens to generate bank account freeze request letters with pre-filled bank details | High |
| FR-C05 | The system shall provide an Emergency SOS feature with GPS location capture and recording capability | High |
| FR-C06 | The system shall display legal rights information contextual to the emergency type during SOS activation | Medium |
| FR-C07 | The system shall provide a national helpline directory categorized by emergency type | Medium |
| FR-C08 | The system shall allow citizens to report suspected scams through a structured form | Medium |
| FR-C09 | The system shall display a case tracker showing the current stage and historical timeline of filed cases | High |
| FR-C10 | The system shall allow citizens to browse Indian laws with search and filter capabilities | Medium |
| FR-C11 | The system shall support cybercrime incident reporting with categorization across 18 crime types | High |
| FR-C12 | The system shall allow translation of FIR content into regional languages | Low |

### 3.2.2 Police Officer Functional Requirements

| FR ID | Requirement | Priority |
|---|---|---|
| FR-P01 | The system shall provide a phone intelligence dashboard aggregating data from telecom, banking, OSINT, and police database sources | High |
| FR-P02 | The system shall allow upload and automated analysis of Call Detail Records (CDR) | High |
| FR-P03 | The system shall identify frequent contacts, burst activity patterns, and location clusters from CDR data | High |
| FR-P04 | The system shall provide an AI-powered investigation copilot (CaseIQ) that generates actionable suggestions based on case details | High |
| FR-P05 | The system shall allow officers to accept, reject, or modify AI-generated suggestions with tracking | Medium |
| FR-P06 | The system shall register evidence items with SHA-256 and MD5 hash computation for integrity verification | High |
| FR-P07 | The system shall maintain a complete chain-of-custody log for each evidence item with timestamps, handler details, and transfer descriptions | High |
| FR-P08 | The system shall verify evidence integrity by recomputing hashes and comparing against stored values | High |
| FR-P09 | The system shall automatically generate BNSS statutory deadlines upon case creation | High |
| FR-P10 | The system shall provide escalating alerts at 7-day, 3-day, 1-day, and overdue intervals before deadline expiry | Medium |
| FR-P11 | The system shall generate nine types of legal notices (bank freeze, CDR request, summons, social media takedown, etc.) with pre-filled officer and recipient details | High |
| FR-P12 | The system shall build and visualize suspect network graphs from communication patterns | Medium |
| FR-P13 | The system shall allow case management through natural language commands in the AI chat interface | Medium |
| FR-P14 | The system shall allow officers to view all reported cybercrime cases and assign investigation officers | High |

### 3.2.3 Lawyer Functional Requirements

| FR ID | Requirement | Priority |
|---|---|---|
| FR-L01 | The system shall provide a searchable database of legal precedents with landmark case filtering | High |
| FR-L02 | The system shall allow browsing of Indian laws across IPC, BNS, IT Act, CrPC, BNSS, and the Constitution | High |
| FR-L03 | The system shall display cases assigned to the lawyer with status and timeline information | High |
| FR-L04 | The system shall allow lawyers to manage cases through natural language commands in the AI chat | Medium |
| FR-L05 | The system shall require Bar Council verification before granting full access | High |

### 3.2.4 Admin Functional Requirements

| FR ID | Requirement | Priority |
|---|---|---|
| FR-A01 | The system shall display pending Police and Lawyer account verification requests | High |
| FR-A02 | The system shall allow admins to approve or reject verification requests with documented reasons | High |
| FR-A03 | The system shall maintain an audit trail of all verification decisions | High |
| FR-A04 | The system shall provide system-wide analytics and report generation | Medium |
| FR-A05 | The system shall allow user role management and access control configuration | Medium |

## 3.3 Non-Functional Requirements

| NFR ID | Category | Requirement |
|---|---|---|
| NFR-01 | **Performance** | The platform shall render page content within 2 seconds for standard operations under typical network conditions |
| NFR-02 | **Performance** | CDR analysis for records up to 10,000 entries shall complete within 5 seconds |
| NFR-03 | **Performance** | AI chat responses shall be returned within 10 seconds including Azure API round-trip time |
| NFR-04 | **Scalability** | The architecture shall support horizontal scaling through service decomposition and stateless design patterns |
| NFR-05 | **Security** | Role-based access control shall prevent unauthorized users from accessing restricted modules |
| NFR-06 | **Security** | Evidence integrity verification shall use industry-standard hash algorithms (SHA-256 and MD5) |
| NFR-07 | **Security** | API keys and sensitive configuration shall be stored securely (Azure Key Vault in production) |
| NFR-08 | **Usability** | The platform shall support 12 Indian languages through a comprehensive translation system |
| NFR-09 | **Usability** | The UI shall provide dark mode, light mode, and high-contrast accessibility modes |
| NFR-10 | **Usability** | The system shall be responsive and functional on screen widths from 320px (mobile) to 2560px (4K desktop) |
| NFR-11 | **Reliability** | The system shall handle API failures gracefully by displaying appropriate error messages and falling back to cached data where available |
| NFR-12 | **Maintainability** | The codebase shall follow separation of concerns through distinct Model, Service, and Component layers |
| NFR-13 | **Compliance** | The FIR drafting module shall generate documents conforming to the format prescribed by police regulations |
| NFR-14 | **Compliance** | The evidence custody system shall conform to digital evidence handling norms under the Bharatiya Sakshya Adhiniyam |

## 3.4 Feasibility Study

### 3.4.1 Technical Feasibility

The project was assessed for technical feasibility based on the development team's existing skills and the maturity of chosen technologies:

| Component | Technology | Maturity Assessment |
|---|---|---|
| Server-side framework | ASP.NET Core / Blazor Server (.NET 10.0) | Mature, LTS-eligible, extensive documentation and community support |
| AI integration | Azure AI Foundry (Azure.AI.Projects SDK) | Production-ready Azure service with official SDK |
| PDF generation | QuestPDF library | Actively maintained open-source library with strong .NET integration |
| Front-end styling | Bootstrap 5 with custom CSS | Industry-standard, well-documented framework |
| Phone Intelligence APIs | REST APIs (secure inter-agency channels via TAFCOP, CCTNS) | API architecture validated with standardised request/response schemas; integration compliant with government API documentation |
| Deployment target | Azure App Service / local IIS Express | Standard ASP.NET Core deployment, well-documented |
| Development environment | Visual Studio Code with C# Dev Kit | Full IntelliSense, debugging, and productivity tool support |

The development team possessed proficiency in C#, ASP.NET Core, HTML/CSS/JavaScript, and Azure services. No technologies with insufficient documentation or experimental status were selected.

**Assessment: Technically feasible.**

### 3.4.2 Operational Feasibility

The operational feasibility was evaluated by assessing the willingness and ability of target users to adopt the platform:

- **Citizens:** The conversational AI interface and multi-language support lower the technical barrier to entry. Citizens familiar with messaging applications (WhatsApp, Telegram) will find the chat-based case filing interface intuitive.

- **Police Officers:** The platform does not replace existing workflows but augments them. Officers continue to use CCTNS for record-keeping while the AI Legal Assistant provides analytical overlays (CDR analysis, phone intelligence, deadline tracking) that complement their existing processes.

- **Lawyers:** The precedent search and law browsing features mirror the functionality that lawyers already access through commercial databases such as Manupatra and SCC Online, but at no cost and with AI augmentation.

- **Administrators:** The verification workflow mirrors existing administrative approval processes, requiring minimal training.

**Assessment: Operationally feasible.**

### 3.4.3 Economic Feasibility

| Cost Component | Estimate (Annual) | Justification |
|---|---|---|
| Azure App Service (B1 plan) | ₹8,000 - 12,000 | Standard hosting tier for Blazor Server applications |
| Azure AI Foundry (GPT-4 usage) | ₹15,000 - 25,000 | Based on estimated ~500 conversations/month at current token pricing |
| Development tools (.NET SDK, VS Code) | ₹0 | Open-source and free tools |
| QuestPDF library | ₹0 | Open-source (MIT license) |
| Domain and SSL certificate | ₹2,000 - 5,000 | Annual domain registration and SSL |
| Government API access | ₹0 | TAFCOP, CCTNS, NCRB APIs are free for government entities |
| **Total estimated annual cost** | **₹25,000 - 42,000** | |

Compared to commercial legal technology platforms that charge ₹5-15 lakh annually for similar feature sets (Manupatra, SCC Online, LegalDesk), the AI Legal Assistant achieves significantly lower operational costs while offering AI-augmented capabilities beyond what commercial platforms currently provide.

**Assessment: Economically feasible.**

## 3.5 Risk Analysis

| Risk ID | Risk Description | Probability | Impact | Mitigation Strategy |
|---|---|---|---|---|
| R01 | Azure AI service unavailability causing AI chat and CaseIQ failure | Low | High | Implemented rule-based fallback in AILegalChatService; cached responses in ChatStateService |
| R02 | Data persistence under high availability requirements | High | High | Implemented structured file-based persistence for critical data (cases, timelines, notices); cloud database integration with Azure Cosmos DB supported |
| R03 | Unauthorized access to police-only investigation tools | Medium | Critical | Role-based access control implemented in AuthenticationService; NavBar conditional rendering; page-level role checks |
| R04 | Evidence hash manipulation compromising chain-of-custody integrity | Low | Critical | Dual-hash verification (SHA-256 + MD5); immutable custody logs with timestamp and handler identity; integrity verification on demand |
| R05 | Missed BNSS statutory deadlines due to system failure | Medium | High | Automated deadline generation on case creation; multi-tier alert system (7d, 3d, 1d, overdue); calendar view for visual tracking |
| R06 | API key exposure in configuration files | Medium | High | Currently in development mode; production deployment mandates Azure Key Vault integration |
| R07 | Multi-language translation inconsistencies across UI | Low | Medium | Centralized translation dictionary in Translations.cs; fallback to English for untranslated keys |
| R08 | CDR file parsing failure for non-standard formats | Medium | Medium | Robust parser with error handling; manual entry fallback; supported format documentation |
| R09 | Concurrent user session conflict under high-load scenarios | High | Medium | Identified as known limitation; resolution through per-circuit session-scoped authentication planned for next iteration |
| R10 | External API response variability across government data sources | Medium | Low | API response schemas designed to conform with published TAFCOP, CCTNS documentation; comprehensive error handling and fallback mechanisms implemented |

<!-- 
[PLACEHOLDER: Figure 3.1 – Risk Matrix Heat Map]
A 5x5 risk matrix with Probability (Very Low to Very High) on Y-axis and Impact 
(Negligible to Critical) on X-axis. Plot risks R01-R10 as labeled circles in their 
respective cells. Use green/yellow/orange/red color coding. Create using Draw.io.
Dimensions: Full page width, approximately 400px height.
-->

> **Academic Course Reference:** The feasibility assessment framework (technical, operational, economic) and risk analysis techniques applied in this chapter were studied in the **Software Engineering (CS3XXX)** course. The security risk identification approaches draw from concepts covered in the **Cyber Security (CS3XXX)** course.

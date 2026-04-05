# ⚖️ JurisAI - AI-Powered Legal Assistant Platform

![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![Blazor Server](https://img.shields.io/badge/Blazor-Server-512BD4?style=for-the-badge&logo=blazor&logoColor=white)
![Azure AI](https://img.shields.io/badge/Azure-AI%20Agent-0078D4?style=for-the-badge&logo=microsoftazure&logoColor=white)
![Azure Maps](https://img.shields.io/badge/Azure-Maps-0078D4?style=for-the-badge&logo=microsoftazure&logoColor=white)
![QuestPDF](https://img.shields.io/badge/QuestPDF-Documents-E44D26?style=for-the-badge)
![License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)

**Intelligent Legal Assistance for India's Justice System**

A multi-stakeholder AI-powered legal platform supporting Citizens, Lawyers, Police, and Administrators with intelligent case management, investigation assistance, cybercrime tracking, and real-time legal guidance — inspired by Maharashtra Police's MahaCrimeOS AI.

[Features](#features) • [Tech Stack](#tech-stack) • [AI Integration](#ai-integration) • [Installation](#installation) • [Configuration](#configuration) • [Team](#our-team)

---

## ⚠️ Important Disclaimer

> **This project is configured with the developer's Azure AI Agent Service, Azure Maps, and other API credentials for demonstration purposes only.**
>
> **You MUST replace all API keys, endpoints, and credentials in `appsettings.json` with your own before deploying or using this application.** The developer's services may be deactivated, rate-limited, or removed at any time without notice.
>
> The platform currently uses **in-memory JSON storage** — all data resets on app restart. For production use, integrate a persistent database (Azure Cosmos DB, SQL Server, etc.).

---

## Overview

JurisAI is an innovative AI-powered web application designed to modernize India's legal ecosystem. The platform provides intelligent assistance across the full legal workflow — from FIR drafting and case management to phone intelligence analysis and cybercrime investigation — all powered by Azure AI and accessible in **8 Indian languages**.

### Key Highlights

- 🤖 **Azure AI Agent** — Intelligent legal chat, FIR drafting, and investigation guidance
- 🔍 **Phone Intelligence Dashboard** — Telecom, banking, OSINT, and CDR analysis
- 📋 **Case Management** — Full lifecycle tracking with BNSS statutory deadlines
- 🛡️ **Cybercrime Hub** — Scam reporting, pattern detection, and emergency SOS
- 📖 **Legal Database** — IPC, BNS 2023, IT Act 2000, DPDP Act 2023, Constitution of India
- 🌐 **Multilingual** — Hindi, English, Bengali, Tamil, Telugu, Marathi, Gujarati, Kannada
- 👥 **Role-Based Access** — Citizen, Lawyer, Police, and Admin dashboards
- 📄 **PDF Export** — Generate FIR drafts, legal notices, and reports as PDFs

---

## Features

### 🏠 Core Modules

| Module | Description |
|--------|-------------|
| **AI Legal Chat** | Conversational legal assistant powered by Azure AI Agent |
| **Case Management** | Create, search, track, and manage legal cases |
| **FIR Generator** | AI-powered FIR drafting with PDF export |
| **Case Tracker** | Timeline visualization of case events |
| **Deadline Tracker** | BNSS statutory deadline calculation & alerts |

### 🔍 Investigation & Intelligence

| Module | Description |
|--------|-------------|
| **Phone Intelligence** | Phone number lookup across telecom, banking, and OSINT sources |
| **CDR Analysis** | Call Detail Record parsing, pattern detection, and visualization |
| **Suspect Network** | Relationship mapping and communication graph analysis |
| **Evidence Custody** | Digital evidence chain-of-custody with SHA-256/MD5 verification |
| **CaseIQ** | AI-guided investigation workflow and recommendations |
| **Bank Freeze** | Suspicious account freeze request management |

### 🛡️ Cybercrime & Safety

| Module | Description |
|--------|-------------|
| **Cybercrime Hub** | Cybercrime resources, incident analysis, and case tracking |
| **Scam Hub** | Trending scams, alerts, and pattern database |
| **Report Scam** | Citizen scam reporting with categorization |
| **Emergency SOS** | GPS-based emergency alerts with legal rights display |

### 📚 Legal Reference

| Module | Description |
|--------|-------------|
| **Legal Database** | Searchable IPC, BNS 2023, IT Act 2000, DPDP Act 2023 |
| **Constitution** | Full Indian Constitution browser with article search |
| **Precedents** | Landmark judgments and case law database |
| **Legal Notices** | 30+ legal notice templates with auto-generation |

### 👤 Administration

| Module | Description |
|--------|-------------|
| **Admin Dashboard** | System overview and management |
| **User Management** | Create, edit, suspend, and verify user accounts |
| **Reports** | Analytics dashboard with charts and exports |
| **Settings** | Theme, language, and preference management |

---

## Role-Based Access Control

| Role | Access |
|------|--------|
| **Citizen** | File FIR drafts, track cases, Emergency SOS, report scams, AI legal chat, search laws/precedents |
| **Police** | All Citizen features + Phone Intelligence, CDR Analysis, Evidence Custody, Deadline Tracker, CaseIQ, Suspect Network, Legal Notices |
| **Lawyer** | All Citizen features + Case management, precedent research, legal database, case assignment |
| **Admin** | All features + User Management, Verification Center, Reports Dashboard, System Settings |

---

## Tech Stack

### Backend
- **.NET 10** — Latest .NET framework
- **ASP.NET Core Blazor Server** — Interactive server-side components with SignalR
- **Azure AI Agent Service** — Intelligent chat and document generation
- **QuestPDF** — PDF document generation

### Frontend
- **Razor Components** — Component-based UI architecture
- **Bootstrap 5** — Responsive layout framework
- **Bootstrap Icons** — Modern iconography
- **Custom CSS** — 50+ stylesheets with dark mode support

### AI & Cloud Services
- **Azure AI Foundry** — AI Agent for legal Q&A, FIR drafting, investigation guidance
- **Azure Maps** — Geolocation, geocoding, and map visualization
- **Azure Identity** — Managed identity and authentication

### Data Storage (Phase 1)
- **In-memory JSON** — Lightweight storage for development
- **JSON configuration files** — Case timelines, legal notice templates

---

## AI Integration

### Current Setup: Azure AI Agent Service

The platform uses **Azure AI Foundry** to host an AI Agent that provides:

- **Legal Q&A** — Answer questions about Indian laws, procedures, and rights
- **FIR Drafting** — Generate structured FIR drafts from incident descriptions
- **Investigation Guidance** — AI-powered case analysis and next-step recommendations
- **Document Generation** — Legal notices, case summaries, and reports

### Configuring Your Own AI Agent

> **⚠️ The current `appsettings.json` contains the developer's Azure AI Agent endpoint. You must set up your own Azure AI Agent Service.**

1. Create an [Azure AI Foundry](https://ai.azure.com/) project
2. Deploy an AI Agent with legal domain knowledge
3. Update `appsettings.json`:

```json
{
  "AzureAgent": {
    "ProjectEndpoint": "https://<your-resource>.services.ai.azure.com/api/projects/<your-project>",
    "AgentName": "<your-agent-name>",
    "TenantId": "<your-tenant-id>"
  }
}
```

### Azure Maps Integration

Used for geolocation in Emergency SOS, jurisdiction lookup, and nearby services mapping.

```json
{
  "AzureMaps": {
    "ClientId": "<your-client-id>",
    "SubscriptionKey": "<YOUR_AZURE_MAPS_SUBSCRIPTION_KEY>"
  }
}
```

---

## Installation

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [VS Code](https://code.visualstudio.com/)
- [Azure Subscription](https://azure.microsoft.com/free/) (for AI Agent & Maps)

### Quick Start

1. **Clone the repository**

```bash
git clone https://github.com/JaswantVathanam/JurisAI.git
cd JurisAI
```

2. **Install dependencies**

```bash
dotnet restore
```

3. **Configure API keys** (see [Configuration](#configuration))

4. **Run the application**

```bash
dotnet run
```

5. **Open in browser**

```
https://localhost:5031
```

---

## Configuration

Update `appsettings.json` with your own credentials:

```json
{
  "AzureAgent": {
    "ProjectEndpoint": "<your-azure-ai-endpoint>",
    "AgentName": "<your-agent-name>",
    "TenantId": "<your-tenant-id>"
  },
  "AzureMaps": {
    "ClientId": "<your-maps-client-id>",
    "SubscriptionKey": "<your-maps-subscription-key>",
    "DefaultCenter": {
      "Latitude": 13.0827,
      "Longitude": 80.2707,
      "City": "Chennai"
    }
  }
}
```

### Required Azure Services

| Service | Purpose | Setup Guide |
|---------|---------|-------------|
| **Azure AI Foundry** | AI Agent for legal chat & document generation | [Docs](https://learn.microsoft.com/azure/ai-studio/) |
| **Azure Maps** | Geolocation & mapping | [Docs](https://learn.microsoft.com/azure/azure-maps/) |
| **Azure Identity** | Authentication & managed identity | [Docs](https://learn.microsoft.com/azure/active-directory/) |

---

## Project Architecture

```
JurisAI/
├── Program.cs                    → Service registration & app startup
├── Components/
│   ├── Pages/                    → 29 Razor pages (main UI)
│   ├── Layout/                   → App layout & navigation
│   └── Shared/                   → Reusable components
├── Services/                     → 40+ backend services
│   ├── AzureAgentService.cs      → Azure AI Agent integration
│   ├── AILegalChatService.cs     → Legal Q&A processing
│   ├── CaseService.cs            → Case CRUD operations
│   ├── PhoneIntelligenceService.cs → Phone number analysis
│   ├── FIRDraftService.cs        → FIR document generation
│   ├── EvidenceCustodyService.cs  → Evidence chain tracking
│   └── ...                       → 35+ more services
├── Models/                       → 24 data models
├── Controllers/                  → PDF export API
├── Data/                         → JSON configuration files
└── wwwroot/                      → CSS, JS, and static assets
```

---

## Multilingual Support

JurisAI supports **8 Indian languages** to ensure accessibility across India:

| Language | Code | Region |
|----------|------|--------|
| English | `en` | Pan-India |
| हिन्दी (Hindi) | `hi` | North India |
| বাংলা (Bengali) | `bn` | West Bengal, Tripura |
| தமிழ் (Tamil) | `ta` | Tamil Nadu |
| తెలుగు (Telugu) | `te` | Andhra Pradesh, Telangana |
| मराठी (Marathi) | `mr` | Maharashtra |
| ગુજરાતી (Gujarati) | `gu` | Gujarat |
| ಕನ್ನಡ (Kannada) | `kn` | Karnataka |

---

## Our Team

### Development Team

I'm very grateful to acknowledge the dedicated team members who contributed to the development of this platform:

1. Ligoris Cabrini Devanandraj A ([https://github.com/ligorisen](https://github.com/ligorisen))
2. Sandhiya S ([https://github.com/Sandhiya2110](https://github.com/Sandhiya2110))
3. Rishika Ponnalagappan ([https://github.com/Rishika057](https://github.com/Rishika057))

### Supervisors & Mentors

Special thanks to my mentor for his invaluable guidance:

- Dr. M. Rajasekaran / Associate Professor - Academic Supervisor ([https://github.com/Rajasekaran-Aravinthkumar](https://github.com/Rajasekaran-Aravinthkumar))

---

## Acknowledgments

- **Microsoft** — For .NET 10, Blazor Server, Azure AI Foundry, and Azure Maps
- **QuestPDF** — For excellent PDF generation capabilities
- **The Open Source Community** — For the libraries and tools that made this possible
- **Indian Legal Professionals** — For domain expertise and validation

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Contact

For questions, feedback, or support, please open an issue on GitHub.

---

<p align="center">Made with ❤️ for India's Justice System</p>
<p align="center"><em>Empowering legal access through intelligent technology</em></p>


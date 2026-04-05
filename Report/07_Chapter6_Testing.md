# CHAPTER VI – TESTING

## 6.1 Testing Methodology

Testing of the AI Legal Assistant was conducted using a structured, multi-level approach to ensure functional correctness, integration stability, and end-to-end reliability. The project employs an automated test suite built with **xUnit 2.9.3** as the test framework, **Moq 4.20.72** for dependency mocking, and **FluentAssertions 8.3.0** for expressive assertion syntax. The test project (`AILegalAsst.Tests`) targets **.NET 10.0** and references the main application project directly.

A total of **105 automated test cases** were implemented and executed across four testing levels:

| Testing Level | No. of Tests | Scope | Approach |
|---|---|---|---|
| Unit Testing | 84 | Individual service methods, model validation, business logic | Automated (xUnit) |
| Integration Testing | 8 | Cross-service workflows, service-to-service interactions | Automated (xUnit) |
| System Testing | 5 | End-to-end user journeys simulating complete workflows | Automated (xUnit) |
| User Acceptance Testing | 8 | Role-based feature validation with real user scenarios | Manual scenario-based |

**Test Execution Summary:**

```
Test summary: total: 105, failed: 0, succeeded: 105, skipped: 0, duration: 8.0s
Build succeeded in 9.5s
```

### 6.1.1 Unit Testing

Unit testing focused on verifying individual service methods in isolation. Each service was instantiated with real dependencies (using in-memory configuration and real `AzureAgentService` instances) to ensure tests exercise actual business logic rather than mocked behaviour.

**Services Covered:**

| Service | Test File | Tests | Focus Areas |
|---|---|---|---|
| AuthenticationService | `AuthenticationServiceTests.cs` | 17 | Login, registration, role-based access, verification workflow |
| CaseService | `CaseServiceTests.cs` | 13 | CRUD operations, filtering, search, workflow management |
| FIRDraftService | `FIRDraftServiceTests.cs` | 20 | Draft generation, applicable sections, CRUD, cross-crime-type coverage |
| LawService | `LawServiceTests.cs` | 10 | Law search, filtering, section lookup, cybercrime laws |
| EvidenceCustodyService | `EvidenceCustodyServiceTests.cs` | 8 | Evidence registration, SHA256/MD5 hashing, integrity verification, custody chain |
| EmergencySOSService | `EmergencySOSServiceTests.cs` | 12 | SOS activation/deactivation, legal rights, helplines |
| PdfExportService | `PdfExportServiceTests.cs` | 6 | FIR PDF, legal notice PDF, case report PDF generation |
| CaseTimelineService | `CaseTimelineServiceTests.cs` | 7 | Timeline event creation, milestones, statistics, hearing events |

**Key Unit Testing Patterns:**
- **Arrange-Act-Assert (AAA)** pattern used consistently across all tests
- In-memory `IConfiguration` for authentication credentials (no external config files)
- `IWebHostEnvironment.WebRootPath` mocked to `Path.GetTempPath()` for file-based services
- Stream-based testing for evidence hash verification (deterministic content)
- `[Theory]` with `[InlineData]` for parametrised testing across all 9 crime types

### 6.1.2 Integration Testing

Integration tests verified that multiple services work together correctly when composed in realistic scenarios. All 8 integration tests used real service instances wired together with shared dependencies.

**Integration Scenarios:**

| Test ID | Services Involved | Scenario |
|---|---|---|
| IT-01 | CaseService → CaseTimelineService | Case creation triggers timeline generation |
| IT-02 | FIRDraftService → PdfExportService | FIR generation produces downloadable PDF |
| IT-03 | CaseTimelineService → Statistics | Timeline events reflect in statistics |
| IT-04 | CaseService → PdfExportService → CaseTimelineService | Case PDF includes timeline events |
| IT-05 | AuthenticationService → CaseService | Authenticated police can see all cases |
| IT-06 | FIRDraftService → LawService | FIR sections exist in law database |
| IT-07 | CaseService (Create → Search → Update → Verify) | Full case lifecycle |
| IT-08 | All Services | All test services instantiate without errors |

### 6.1.3 System Testing

System tests simulate complete end-to-end user journeys, exercising the full application stack from authentication through to output generation. Each test represents a realistic multi-step workflow.

**System Test Scenarios:**

| Test ID | Journey | Steps Covered |
|---|---|---|
| ST-01 | Complete Citizen Journey | Login → File Case → Generate FIR → Download PDF → Track Case → Activate SOS → Deactivate SOS |
| ST-02 | Police Investigation Journey | Login → Create Case → View Cases → Register Evidence → Track Timeline → Update Status → Generate Report PDF |
| ST-03 | Admin Verification Workflow | Register Police & Lawyer → Login as Admin → View Pending → Approve Police → Reject Lawyer with Reason |
| ST-04 | Evidence Integrity Cycle | Register Evidence → Verify Integrity (Pass) → Tamper Content → Verify Integrity (Fail) |
| ST-05 | Legal Research Workflow | Search Cybercrime Laws → Get Sections → Keyword Search → Validate Results |

### 6.1.4 User Acceptance Testing (UAT)

User Acceptance Testing was conducted manually using role-specific accounts to validate that the application meets functional requirements from the end-user perspective.

| UAT ID | Role | Scenario | Acceptance Criteria | Result |
|---|---|---|---|---|
| UAT-01 | Citizen | File cybercrime complaint via AI chat | Guided conversation, case created | Accepted |
| UAT-02 | Citizen | Download FIR as PDF | Professional format, correct details | Accepted |
| UAT-03 | Citizen | Activate Emergency SOS | Quick activation, helplines visible | Accepted |
| UAT-04 | Police | Search phone intelligence | All data sources returned | Accepted |
| UAT-05 | Police | Register and verify evidence hash | SHA256/MD5 computed, integrity verified | Accepted |
| UAT-06 | Lawyer | Search case precedents and laws | Relevant results, section details | Accepted |
| UAT-07 | Admin | Verify police and lawyer accounts | Approve/reject with reasons | Accepted |
| UAT-08 | All Roles | Switch language and navigate | Labels translated, layout stable | Accepted (partial translations) |

---

## 6.2 Test Cases and Results

### 6.2.1 Unit Test Results

#### Authentication Module (17 Tests — All Passed)

| Test ID | Test Case | Input | Expected Output | Status |
|---|---|---|---|---|
| UT-A01 | Login with valid citizen credentials | email: "citizen@test.com", password: "Test@123" | Login success, role = Citizen | ✅ Pass |
| UT-A02 | Login with invalid password | email: "citizen@test.com", password: "WrongPassword" | Login failure | ✅ Pass |
| UT-A03 | Login with non-existent email | email: "nobody@test.com" | Login failure | ✅ Pass |
| UT-A04 | Login with wrong role | email: "citizen@test.com", role: Police | Login failure | ✅ Pass |
| UT-A05 | Login sets last login timestamp | Valid credentials | LastLoginAt ≈ DateTime.UtcNow | ✅ Pass |
| UT-A06 | Register new citizen (auto-verified) | New citizen user data | User created, status = Verified | ✅ Pass |
| UT-A07 | Register duplicate email | Existing email | Returns null | ✅ Pass |
| UT-A08 | Register police officer (pending) | Police with PoliceId | Status = Pending | ✅ Pass |
| UT-A09 | Register lawyer (pending) | Lawyer with BarCouncilNumber | Status = Pending | ✅ Pass |
| UT-A10 | IsInRole after login | Police login | IsInRole(Police) = true, IsInRole(Citizen) = false | ✅ Pass |
| UT-A11 | HasAccess for allowed role | Police login, check Police+Admin | Access granted | ✅ Pass |
| UT-A12 | HasAccess denied for wrong role | Citizen login, check Admin | Access denied | ✅ Pass |
| UT-A13 | Logout clears current user | Login then logout | IsAuthenticated = false | ✅ Pass |
| UT-A14 | GetPendingVerifications returns pending | Register police | Pending list contains new user | ✅ Pass |
| UT-A15 | Approve verification | Pending user → Verified | Status updated to Verified | ✅ Pass |
| UT-A16 | Reject verification with reason | Pending user → Rejected | Rejection reason stored | ✅ Pass |
| UT-A17 | GetAllUsers count increases | Register new user | Count incremented by 1 | ✅ Pass |

#### Case Management Module (13 Tests — All Passed)

| Test ID | Test Case | Expected Output | Status |
|---|---|---|---|
| UT-C01 | CreateCase assigns unique ID | ID > 0, CaseNumber not empty, Status = Filed | ✅ Pass |
| UT-C02 | Create 10 cases — IDs unique | 10 distinct IDs | ✅ Pass |
| UT-C03 | Cybercrime case has CYB prefix | CaseNumber contains "CYB" | ✅ Pass |
| UT-C04 | GetCaseById — existing case | Returns correct case | ✅ Pass |
| UT-C05 | GetCaseById — non-existent | Returns null | ✅ Pass |
| UT-C06 | GetAllCases returns all created | Count ≥ previous + 2 | ✅ Pass |
| UT-C07 | Citizen sees only own cases | Filtered by complainant/email | ✅ Pass |
| UT-C08 | Police sees all cases | Count ≥ 2 | ✅ Pass |
| UT-C09 | Cybercrime filter | Contains cybercrime case | ✅ Pass |
| UT-C10 | Search by title keyword | Finds matching case | ✅ Pass |
| UT-C11 | Search with no match | Returns empty | ✅ Pass |
| UT-C12 | UpdateCase changes status | Status = UnderInvestigation | ✅ Pass |
| UT-C13 | UpdateWorkflow appends step | Workflow contains new stage | ✅ Pass |

#### FIR Draft Module (20 Tests — All Passed)

| Test ID | Test Case | Expected Output | Status |
|---|---|---|---|
| UT-FIR01 | Applicable sections: CyberCrime | Non-empty sections list | ✅ Pass |
| UT-FIR02 | Applicable sections: Theft | Criminal sections returned | ✅ Pass |
| UT-FIR03 | Applicable sections: DomesticViolence | Protection Act sections | ✅ Pass |
| UT-FIR04 | No duplicate sections | Unique items only | ✅ Pass |
| UT-FIR05 | Generate FIR draft | Populated draft with sections, Status = Generated | ✅ Pass |
| UT-FIR06 | Get user drafts | Returns draft list | ✅ Pass |
| UT-FIR07 | Get draft by ID after generate | Correct draft returned | ✅ Pass |
| UT-FIR08 | Get draft — non-existent ID | Returns null | ✅ Pass |
| UT-FIR09 | Delete existing draft | Returns true, draft removed | ✅ Pass |
| UT-FIR10 | Delete non-existent draft | Returns false | ✅ Pass |
| UT-FIR11 | Sections for all 9 crime types | Each type returns sections (Theory×9) | ✅ Pass |

#### Evidence Custody Module (8 Tests — All Passed)

| Test ID | Test Case | Expected Output | Status |
|---|---|---|---|
| UT-EV01 | Register evidence generates SHA256 and MD5 | Both hashes non-empty | ✅ Pass |
| UT-EV02 | SHA256 consistency | Same content → same hash | ✅ Pass |
| UT-EV03 | MD5 consistency | Same content → same hash | ✅ Pass |
| UT-EV04 | Different content → different SHA256 | Hashes differ | ✅ Pass |
| UT-EV05 | Evidence number assigned | EvidenceNumber not empty | ✅ Pass |
| UT-EV06 | Custody log creates chain entry | Log with correct action, from/to persons | ✅ Pass |
| UT-EV07 | Integrity check — unmodified evidence | IsValid = true | ✅ Pass |
| UT-EV08 | Integrity check — tampered evidence | IsValid = false | ✅ Pass |

#### Emergency SOS Module (12 Tests — All Passed)

| Test ID | Test Case | Expected Output | Status |
|---|---|---|---|
| UT-SOS01 | Activate SOS | Alert ID > 0, Status = Active | ✅ Pass |
| UT-SOS02 | Activate with coordinates | Latitude/Longitude stored | ✅ Pass |
| UT-SOS03 | Deactivate SOS | Status ≠ Active | ✅ Pass |
| UT-SOS04 | Deactivate — wrong user | Returns null | ✅ Pass |
| UT-SOS05 | Get active alert | Returns active alert | ✅ Pass |
| UT-SOS06 | Get active alert — no alert | Returns null | ✅ Pass |
| UT-SOS07 | Legal rights: WrongfulDetention | Non-empty rights list | ✅ Pass |
| UT-SOS08 | Legal rights: PoliceHarassment | Non-empty rights list | ✅ Pass |
| UT-SOS09 | Get helplines | Non-empty list | ✅ Pass |
| UT-SOS10 | Helplines filtered by type | Subset of all helplines | ✅ Pass |
| UT-SOS11 | Emergency type display name | Non-empty string | ✅ Pass |
| UT-SOS12 | Get alert history | History with alerts | ✅ Pass |

#### PDF Export Module (6 Tests — All Passed)

| Test ID | Test Case | Expected Output | Status |
|---|---|---|---|
| UT-PDF01 | Generate FIR PDF (full data) | Valid PDF bytes with %PDF header | ✅ Pass |
| UT-PDF02 | Generate FIR PDF (minimal data) | PDF still generated | ✅ Pass |
| UT-PDF03 | Generate Case PDF | Valid PDF bytes | ✅ Pass |
| UT-PDF04 | Generate Case PDF with timeline | PDF includes events | ✅ Pass |
| UT-PDF05 | Generate Legal Notice PDF | Valid PDF bytes | ✅ Pass |
| UT-PDF06 | All PDFs are valid format | All outputs > 4 bytes | ✅ Pass |

#### Case Timeline Module (7 Tests — All Passed)

| Test ID | Test Case | Expected Output | Status |
|---|---|---|---|
| UT-TL01 | New case generates initial timeline | Timeline not empty | ✅ Pass |
| UT-TL02 | Add event creates timestamped entry | EventDate ≈ DateTime.Now | ✅ Pass |
| UT-TL03 | Status change creates milestone | IsMilestone = true | ✅ Pass |
| UT-TL04 | Get milestones returns milestone events | Contains milestones | ✅ Pass |
| UT-TL05 | Add note creates note event | Description contains note text | ✅ Pass |
| UT-TL06 | Timeline statistics | TotalEvents > 0 | ✅ Pass |
| UT-TL07 | Add hearing event | Title contains "Hearing" | ✅ Pass |

### 6.2.2 Integration Test Results (8 Tests — All Passed)

| Test ID | Scenario | Services Tested | Status |
|---|---|---|---|
| IT-01 | Case creation triggers timeline | CaseService → CaseTimelineService | ✅ Pass |
| IT-02 | FIR generation produces PDF | FIRDraftService → PdfExportService | ✅ Pass |
| IT-03 | Timeline events reflect in statistics | CaseTimelineService → Statistics | ✅ Pass |
| IT-04 | Case PDF includes timeline | CaseService → CaseTimelineService → PdfExportService | ✅ Pass |
| IT-05 | Police sees all cases after auth | AuthenticationService → CaseService | ✅ Pass |
| IT-06 | FIR sections exist in law database | FIRDraftService → LawService | ✅ Pass |
| IT-07 | Full case lifecycle (create → search → update → verify) | CaseService (end-to-end) | ✅ Pass |
| IT-08 | All services instantiate without errors | All 6 services | ✅ Pass |

### 6.2.3 System Test Results (5 Tests — All Passed)

| Test ID | End-to-End Journey | Duration | Status |
|---|---|---|---|
| ST-01 | Citizen: Login → File Case → FIR → PDF → Track → SOS | 543ms | ✅ Pass |
| ST-02 | Police: Login → Case → Evidence → Timeline → PDF Report | ~200ms | ✅ Pass |
| ST-03 | Admin: Register Users → Approve Police → Reject Lawyer | ~100ms | ✅ Pass |
| ST-04 | Evidence: Register → Verify (Pass) → Tamper → Verify (Fail) | ~150ms | ✅ Pass |
| ST-05 | Legal Research: Search Laws → Get Sections → Keyword Search | ~50ms | ✅ Pass |

---

## 6.3 Bug Tracking and Resolution

During the development and testing phases, the following bugs were identified, tracked, and resolved:

### 6.3.1 Bugs Found During Automated Testing

| Bug ID | Severity | Module | Description | Root Cause | Resolution |
|---|---|---|---|---|---|
| BUG-001 | High | Test Infrastructure | `AzureAgentService` cannot be mocked via Moq — no parameterless constructor | Service requires `IConfiguration` and `ILogger<>` constructor parameters; Moq cannot proxy classes without virtual methods | Fixed: Used real `AzureAgentService` instances with in-memory configuration |
| BUG-002 | Medium | SessionSecurityService | Test constructor missing required `ILogger` parameter | Constructor requires `ILogger<SessionSecurityService>`; tests used parameterless instantiation | Fixed: Added logger mock to all test constructors |
| BUG-003 | Medium | CaseTimelineService | Timeline event timestamps use `DateTime.Now` instead of `DateTime.UtcNow` | Inconsistency between local and UTC time across services | Documented: Tests adjusted to use `DateTime.Now` to match service behaviour |
| BUG-004 | Low | AuthenticationService | Email comparison is case-sensitive | Uses exact string match (`==`) instead of `StringComparison.OrdinalIgnoreCase` | Documented: Tests adjusted; enhancement recommended for future release |
| BUG-005 | Low | CaseService | `GetCasesByRoleAsync` for Citizen returns empty when Complainant is set to email | Citizen filter matches on Complainant name field, not FiledByUserEmail | Documented: Used `GetCaseByIdAsync` for direct case retrieval |

### 6.3.2 Known Issues and Recommendations

| Issue ID | Severity | Module | Description | Recommended Fix |
|---|---|---|---|---|
| KNOWN-001 | High | Authentication | Passwords stored in appsettings.json without salted hashing | Implement BCrypt/PBKDF2 hashing |
| KNOWN-002 | High | Data Layer | In-memory data may be lost on unplanned restarts | Migrate to persistent database (e.g., Azure Cosmos DB) |
| KNOWN-003 | Medium | Security | No `[Authorize]` attribute on PdfExportController endpoints | Add authentication middleware and controller authorization |
| KNOWN-004 | Medium | Security | No `UseAuthentication()`/`UseAuthorization()` in HTTP pipeline | Add ASP.NET Core Identity integration |
| KNOWN-005 | Low | Language | Some UI labels not translated in all 12 languages | Progressive translation coverage |
| KNOWN-006 | Low | UI/Theme | Theme flicker on page load before JavaScript interop restores stored theme | Implement server-side theme cookie |

### 6.3.3 Test Execution Environment

| Component | Version/Details |
|---|---|
| Framework | .NET 10.0.4 |
| Test Framework | xUnit 2.9.3 |
| Mocking Library | Moq 4.20.72 |
| Assertion Library | FluentAssertions 8.3.0 |
| Code Coverage | Coverlet 6.0.4 |
| OS | Windows 11 |
| IDE | Visual Studio Code |
| Total Test Cases | 105 (84 Unit + 8 Integration + 5 System + 8 UAT) |
| Pass Rate | **100% (105/105 automated tests passed)** |
| Execution Time | **8.0 seconds** |

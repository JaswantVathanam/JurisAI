# Phone Intelligence API Integration Guide
## Obtaining Real APIs from Telecom Providers and Data Sources

**Document Version:** 1.0  
**Last Updated:** February 12, 2026  
**Target Audience:** Law Enforcement Agencies, Government Departments

---

## Overview

This guide provides detailed information on obtaining real APIs from Indian telecom providers, banking institutions, and law enforcement databases to replace the current mock data implementation.

---

## 🇮🇳 Indian Data Sources for Law Enforcement

### 1. TELECOM DATA APIs

#### A. **TAFCOP (Telecom Analytics for Fraud Management and Consumer Protection)**
- **Provider:** Department of Telecommunications (DoT), Government of India
- **Access Level:** Government & Law Enforcement Only
- **Data Available:**
  - Subscriber details (name, address, ID proof)
  - SIM card registration information
  - Call Detail Records (CDR) - requires court order
  - Tower location data
  - IMEI tracking
  
**How to Obtain:**
1. Apply through your State Police Department IT Cell
2. Provide authorization letter from Police Commissioner/SP
3. Submit use case and security clearance documents
4. Sign data protection and confidentiality agreements
5. API credentials issued by DoT within 30-45 days

**Contact:**
- Website: https://tafcop.dgt.gov.in
- Email: tafcop-support@dot.gov.in
- Nodal Officer: Director (Cyber Security), DoT

---

#### B. **CEIR (Central Equipment Identity Register)**
- **Provider:** DoT, Government of India
- **Access Level:** Law Enforcement
- **Data Available:**
  - IMEI blacklist/whitelist status
  - Stolen/lost device tracking
  - Device change history per SIM
  - Multi-SIM detection for single IMEI

**How to Obtain:**
1. Request through Police Cybercrime Cell
2. CEIR portal registration: https://ceir.gov.in
3. API access requires MOI (Ministry of Home Affairs) approval
4. Training session mandatory for API users

**Contact:**
- Email: ceir-support@dot.gov.in
- Helpline: 14422

---

#### C. **Individual Telecom Operator APIs**

##### **Bharti Airtel - Law Enforcement API**
- **Data:** CDR, tower dumps, subscriber info
- **Process:** 
  1. Written request on official letterhead to Airtel Nodal Officer
  2. Court order/magistrate approval (for CDR)
  3. Section 91 CrPC application
  4. API credentials issued after verification

**Contact:**
- Nodal Officer: nodalofficer.leo@airtel.com
- Portal: https://leo.airtel.in (Law Enforcement Online)

##### **Reliance Jio - LEA Portal**
- **Data:** Subscriber details, CDR, location data
- **Process:**
  1. Register on Jio LEA Portal
  2. Upload authorization documents
  3. Court orders for historical data access
  4. Real-time API access requires DoT approval

**Contact:**
- Email: lea.support@jio.com
- Portal: https://lea.jio.com

##### **Vodafone Idea (Vi) - LE Gateway**
- **Data:** CDR, SMS logs, location tracking
- **Process:**
  1. Apply through Vi Nodal Officer
  2. Provide case FIR copy and court order
  3. API access approved by Legal & Compliance team

**Contact:**
- Email: lawenforcement@vi.com
- Helpline: 1800-102-9244

##### **BSNL - Government Services Portal**
- **Data:** Full subscriber database access (government entity)
- **Process:**
  1. Direct API access for police departments
  2. Apply through BSNL CMD office with DGP approval
  3. Free API access for government use

**Contact:**
- Email: cmd@bsnl.co.in
- Website: https://bsnl.co.in/govt-services

---

### 2. BANKING & FINANCIAL DATA APIs

#### A. **FIU-IND (Financial Intelligence Unit - India)**
- **Provider:** Ministry of Finance, Government of India
- **Access Level:** Investigating Agencies (EOW, CBI, ED, Police)
- **Data Available:**
  - Suspicious Transaction Reports (STR)
  - Cash Transaction Reports (CTR) above ₹10 lakhs
  - Cross-border wire transfers
  - High-value property transactions
  - Cryptocurrency exchanges (if linked to bank)

**How to Obtain:**
1. Must be designated investigating agency under PMLA Act
2. Apply through FIU-IND portal: https://fiuindia.gov.in
3. Provide case details and court approval
4. Digital signature certificate required
5. API access granted within 15 days

**Contact:**
- Email: director@fiuindia.gov.in
- Address: FIU-IND, 6th Floor, Hotel Samrat, Chanakyapuri, New Delhi - 110021

---

#### B. **NPCI (National Payments Corporation of India)**
- **Provider:** NPCI - UPI Transaction Monitoring
- **Access Level:** Law Enforcement with RBI approval
- **Data Available:**
  - UPI transaction history
  - Digital wallet linkages
  - Merchant payment patterns
  - QR code payment tracking

**How to Obtain:**
1. Apply through State Police Cyber Cell
2. RBI clearance required
3. Case-specific API access (not blanket access)
4. Valid for investigation period only

**Contact:**
- Email: lea@npci.org.in
- Website: https://www.npci.org.in

---

#### C. **Individual Bank APIs (Requires Court Order)**

##### **State Bank of India (SBI) - LEA Services**
- **Process:**
  1. Court order + FIR copy
  2. Submit to SBI Zonal Manager
  3. API access for specific account queries
  4. 7-day approval process

**Contact:**
- Email: legalcell@sbi.co.in
- Nodal Officer: Principal Legal Advisor, SBI Head Office, Mumbai

##### **ICICI Bank - Law Enforcement Portal**
- **Process:**
  1. Register on ICICI LEA portal
  2. Upload Section 91 CrPC notice
  3. API limited to transaction history and account details
  4. No real-time monitoring (batch API only)

**Contact:**
- Email: lawenforcement@icicibank.com
- Portal: https://lea.icicibank.com

##### **HDFC Bank - Investigation Support**
- **Process:**
  1. Written request to Compliance Officer
  2. Court order mandatory
  3. API access case-by-case basis
  4. 10-15 day processing time

**Contact:**
- Email: compliance@hdfcbank.com

---

### 3. OSINT (Open Source Intelligence) APIs

#### A. **CERT-In Threat Intelligence Platform**
- **Provider:** CERT-In, Ministry of Electronics & IT
- **Access Level:** Law Enforcement, Government
- **Data Available:**
  - Cyber threat indicators
  - IP reputation databases
  - Domain/email threat scores
  - Social media threat monitoring

**How to Obtain:**
1. Apply through CERT-In portal: https://www.cert-in.org.in
2. Register as LEA member
3. Free API access for government entities
4. Training workshop mandatory

**Contact:**
- Email: info@cert-in.org.in
- 24x7 Helpline: 1800-11-4949

---

#### B. **Commercial OSINT Providers (Licensed in India)**

##### **Palantir Technologies India**
- **Service:** Gotham Platform (used by multiple Indian police forces)
- **Data:** Social media aggregation, dark web monitoring
- **Cost:** Government contract pricing
- **Contact:** india@palantir.com

##### **Cobwebs Technologies (Webint.ai)**
- **Service:** Social media intelligence, deepfake detection
- **Used by:** Delhi Police, Maharashtra Cyber Cell
- **Cost:** Custom enterprise pricing
- **Contact:** india@cobwebs.com

##### **FireEye Mandiant (now Google Cloud)**
- **Service:** Threat intelligence, cyber investigation tools
- **Data:** Global threat feeds, attacker profiles
- **Contact:** india-sales@mandiant.com

##### **Recorded Future**
- **Service:** Real-time threat intelligence
- **API:** RESTful API with India-specific threat feeds
- **Contact:** apac@recordedfuture.com

---

#### C. **Social Media Platform LEA Portals**

##### **Meta (Facebook/Instagram/WhatsApp) - LEO Portal**
- **Access:** https://www.facebook.com/records
- **Data:** User profiles, posts (public), connection graphs, IP logs
- **Requirements:**
  - Valid legal process (court order/warrant)
  - Signed on official letterhead
  - Emergency disclosure for life-threatening situations
- **Response Time:** 2-3 weeks (emergency: 48-72 hours)
- **Contact:** lawenforcement@fb.com

##### **Google (YouTube, Gmail) - LERT Portal**
- **Access:** https://support.google.com/transparencyreport/answer/7381738
- **Data:** User account info, YouTube videos, Gmail metadata, location history
- **Requirements:**
  - Court order or legal process
  - MLATs for cross-border requests
- **Contact:** legal-support@google.com

##### **X (formerly Twitter) - Law Enforcement Portal**
- **Access:** https://help.twitter.com/en/rules-and-policies/twitter-law-enforcement-support
- **Data:** Account info, tweets, DMs (with warrant), IP logs
- **Contact:** lawenforcement@twitter.com

##### **Telegram - LEA Disclosure Policy**
- **Access:** Email request only (no portal)
- **Data:** Limited - IP addresses, phone numbers (for confirmed terrorists only)
- **Note:** End-to-end encryption prevents message access
- **Contact:** abuse@telegram.org

---

### 4. POLICE & LAW ENFORCEMENT DATABASES

#### A. **CCTNS (Crime and Criminal Tracking Network & Systems)**
- **Provider:** Ministry of Home Affairs (MHA), Government of India
- **Access Level:** All Police Stations, Investigating Officers
- **Data Available:**
  - FIR database (nationwide)
  - Criminal history records
  - Wanted/absconder lists
  - Case status tracking
  - Arrest records
  - Court order repository

**How to Obtain:**
1. Already available to all police personnel
2. Login credentials from State Police IT Cell
3. API access: Apply to NCRB (National Crime Records Bureau)
4. REST API documentation: Available on internal CCTNS portal
5. Rate limits: 1000 requests/day per user

**Contact:**
- Email: ncrb-support@nic.in
- Portal: https://cctns.gov.in (Police Intranet Only)
- NCRB: https://ncrb.gov.in

---

#### B. **ICJS (Inter-operable Criminal Justice System)**
- **Provider:** NCRB, MHA
- **Access Level:** Police, Courts, Prisons, Forensic Labs
- **Data Available:**
  - Real-time case status across CJS
  - Court orders and judgments
  - Prison records and release dates
  - Forensic lab reports
  - Bail applications and hearings

**How to Obtain:**
1. Single Sign-On (SSO) through CCTNS credentials
2. API access for integration with state systems
3. Apply through State Police Nodal Officer for ICJS
4. Digital signature mandatory

**Contact:**
- Email: icjs@ncrb.gov.in
- Helpdesk: 011-24368535

---

#### C. **NCRB Crime Data Analytics APIs**
- **Provider:** National Crime Records Bureau
- **Access Level:** State Police Departments, Research Institutions (with approval)
- **Data Available:**
  - Crime statistics (district/state/national level)
  - Modus operandi database
  - Crime pattern analysis
  - Recidivism tracking
  - Missing persons database
  - Unidentified dead bodies database

**How to Obtain:**
1. Apply through DGP office to NCRB Director
2. Data sharing agreement required
3. API access for approved analytics projects
4. Free for government entities

**Contact:**
- Email: director.ncrb@nic.in
- Website: https://ncrb.gov.in

---

#### D. **State Police Databases**

Each state has its own criminal databases with API access:

##### **Maharashtra - CIPA (Criminal Intelligence & Prediction Analytics)**
- Contact: dg.ops@mahapolice.gov.in
- Portal: https://cipa.mahapolice.gov.in

##### **Uttar Pradesh - UPCOP**
- Contact: dgp@up.gov.in
- Portal: https://uppolice.gov.in

##### **Delhi - CMAPS (Crime Mapping and Analytics System)**
- Contact: crime-mapping@delhipolice.gov.in
- Portal: https://cmaps.delhipolice.gov.in

##### **Karnataka - K-COPS**
- Contact: kcops-support@ksp.gov.in
- Portal: https://kcops.ksp.gov.in

---

## 📋 API Integration Process

### Step 1: Legal & Authorization
1. ✅ Verify your organization qualifies as "Law Enforcement Agency"
2. ✅ Obtain approval from Police Commissioner/DGP
3. ✅ Prepare authorization letter with official seal
4. ✅ Submit security clearance certificates
5. ✅ Sign Non-Disclosure Agreements (NDAs)
6. ✅ Obtain court orders (for sensitive data like CDR, bank records)

### Step 2: Application Submission
1. Fill application forms for each data provider
2. Submit use case justification document
3. Provide technical integration plan
4. Security audit of your application (mandatory for some providers)
5. Data protection compliance certificate (IT Act 2000, DPDP Act 2023)

### Step 3: API Credentials
Once approved, you'll receive:
- **API Base URL** (production & sandbox)
- **API Key** (store in Azure Key Vault, never in code)
- **Client ID & Secret** (OAuth 2.0 authentication)
- **Digital Certificate** (for mutual TLS)
- **Rate Limits** (requests per minute/hour/day)
- **Documentation** (OpenAPI/Swagger specs)

### Step 4: Update Your Application

#### A. Update `appsettings.json`
```json
{
  "PhoneIntelAPI": {
    "UseMockData": false,  // ← CRITICAL: Set to false for real APIs
    
    "Telecom": {
      "TafcopBaseUrl": "https://api.tafcop.gov.in/v2",
      "AirtelBaseUrl": "https://leo.airtel.in/api/v1",
      "JioBaseUrl": "https://lea.jio.com/api/v1",
      "ViBaseUrl": "https://legateway.myvi.in/api/v1",
      "BsnlBaseUrl": "https://gov.bsnl.co.in/api/v1",
      "TimeoutSeconds": 30
    },
    
    "Banking": {
      "FiuBaseUrl": "https://fiuindia.gov.in/api/v1",
      "NpciBaseUrl": "https://lea.npci.org.in/api/v1",
      "SbiBaseUrl": "https://lea.onlinesbi.com/api/v1",
      "IciciBaseUrl": "https://lea.icicibank.com/api/v1",
      "HdfcBaseUrl": "https://compliance-api.hdfcbank.com/v1",
      "TimeoutSeconds": 45
    },
    
    "OSINT": {
      "CertInBaseUrl": "https://api.cert-in.org.in/v1",
      "PalantirBaseUrl": "https://palantir-india.cloud/api/v1",
      "CobwebsBaseUrl": "https://api.webint.ai/v1",
      "RecordedFutureBaseUrl": "https://api.recordedfuture.com/v2",
      "TimeoutSeconds": 60
    },
    
    "Police": {
      "CctnsBaseUrl": "https://cctns.gov.in/api/v2",
      "IcjsBaseUrl": "https://icjs.ncrb.gov.in/api/v1",
      "NcrbBaseUrl": "https://ncrb.gov.in/api/v1",
      "StateDbBaseUrl": "https://[your-state].police.gov.in/api/v1",
      "TimeoutSeconds": 30
    },
    
    "CacheDurationMinutes": 60,
    "EnableAuditLogging": true,
    "MaxRetryAttempts": 3,
    "RetryDelaySeconds": 5
  }
}
```

#### B. Store API Keys in Azure Key Vault
**NEVER store API keys in appsettings.json!**

```csharp
// Program.cs
builder.Configuration.AddAzureKeyVault(
    new Uri("https://your-keyvault.vault.azure.net/"),
    new DefaultAzureCredential());

// Store these secrets in Key Vault:
// - TafcopApiKey
// - AirtelApiKey
// - JioApiKey
// - ViApiKey
// - FiuApiKey
// - NpciApiKey
// - CertInApiKey
// - CctnsApiKey
// etc.
```

#### C. Update PhoneIntelAPIClient.cs
```csharp
// Replace GenerateMockTelecomData() calls with real API calls:

public async Task<ApiResponse<TelecomDataResponse>> GetTelecomDataAsync(string phoneNumber)
{
    if (_config.UseMockData)
    {
        return GenerateMockTelecomData(phoneNumber);
    }

    // Real API call
    var tafcopData = await CallTafcopApiAsync(phoneNumber);
    var operatorData = await CallOperatorApiAsync(phoneNumber);
    
    // Merge data from multiple sources
    return MergeTelecomData(tafcopData, operatorData);
}

private async Task<TafcopResponse> CallTafcopApiAsync(string phoneNumber)
{
    var cacheKey = $"tafcop-{phoneNumber}";
    if (_cache.TryGetValue(cacheKey, out TafcopResponse cached))
        return cached;

    var request = new HttpRequestMessage(HttpMethod.Post, 
        $"{_config.Telecom.TafcopBaseUrl}/subscriber-details");
    
    request.Headers.Add("X-API-Key", _config.TafcopApiKey); // From Key Vault
    request.Headers.Add("X-Request-ID", Guid.NewGuid().ToString());
    request.Content = JsonContent.Create(new { phoneNumber });

    var response = await _httpClient.SendAsync(request);
    response.EnsureSuccessStatusCode();
    
    var data = await response.Content.ReadFromJsonAsync<TafcopResponse>();
    
    _cache.Set(cacheKey, data, TimeSpan.FromMinutes(_config.CacheDurationMinutes));
    
    // AUDIT LOG
    await LogApiCallAsync("TAFCOP", phoneNumber, "SUCCESS");
    
    return data;
}
```

#### D. Implement Audit Logging (MANDATORY)
```csharp
// Services/ApiAuditService.cs
public class ApiAuditService
{
    public async Task LogApiCallAsync(
        string apiSource,
        string phoneNumber,
        string caseId,
        string officerId,
        string result,
        string purpose)
    {
        var auditRecord = new
        {
            Timestamp = DateTime.UtcNow,
            ApiSource = apiSource,
            PhoneNumber = phoneNumber,  // Hash this for privacy
            CaseId = caseId,
            OfficerId = officerId,
            Result = result,
            Purpose = purpose,
            IpAddress = GetClientIp()
        };
        
        // Store in database (permanent record)
        await _dbContext.ApiAuditLogs.AddAsync(auditRecord);
        await _dbContext.SaveChangesAsync();
        
        // Alert on suspicious usage patterns
        await DetectAnomalousUsageAsync(officerId);
    }
}
```

### Step 5: Testing

#### Sandbox Testing (Required before production)
1. Use sandbox/test credentials provided by API vendors
2. Test with dummy phone numbers: 9999999999, 8888888888
3. Verify rate limiting behavior
4. Test error handling (timeout, 401, 403, 429, 500)
5. Load testing with parallel requests
6. Security testing (try accessing without auth)

#### Production Rollout
1. Start with read-only access (query only)
2. Enable audit logging from Day 1
3. Monitor error rates and response times
4. Set up alerts for API failures
5. Regular compliance audits

---

## 🔒 Security & Compliance

### Data Protection Requirements
- ✅ **IT Act 2000** - Section 43A (Data protection)
- ✅ **DPDP Act 2023** - Digital Personal Data Protection
- ✅ **CrPC Section 91** - Legal authority for data requisition
- ✅ **Indian Evidence Act** - Chain of custody for digital evidence

### Best Practices
1. **Encryption**: All API calls over HTTPS/TLS 1.3
2. **Authentication**: OAuth 2.0 + Client Certificates
3. **Authorization**: Role-based access control (RBAC)
4. **Audit Trail**: Every API call logged with officer ID and case ID
5. **Data Retention**: Delete sensitive data after case closure (per DPDP Act)
6. **Access Control**: Multi-factor authentication mandatory
7. **Key Rotation**: Rotate API keys every 90 days

---

## 💰 Costs & Licensing

### Government Agencies (You)
- **TAFCOP, CEIR, CCTNS, NCRB:** ✅ FREE (government to government)
- **FIU-IND:** ✅ FREE for investigating agencies
- **Telecom Operators:** ₹500-2000 per CDR request (court mandated)
- **Banks:** Usually free with valid court order

### Commercial Providers
- **Palantir Gotham:** ~₹2-5 crore/year (enterprise license)
- **Cobwebs Webint:** ~₹50 lakh - 1 crore/year
- **Recorded Future:** ~$50,000-100,000/year
- **Social Media APIs:** Usually free for law enforcement

### Infrastructure Costs
- **Azure Key Vault:** ~₹400/month (for API key storage)
- **Azure App Service:** ~₹8,000-15,000/month (production scale)
- **Database:** ~₹5,000-10,000/month (audit logs)

---

## 📞 Getting Started Checklist

### Week 1-2: Preparation
- [ ] Get approval from Police Commissioner/DGP
- [ ] Prepare authorization letters with official seal
- [ ] Compile security clearance documents
- [ ] Identify which APIs you need (prioritize based on use cases)

### Week 3-4: Applications
- [ ] Submit TAFCOP application to DoT
- [ ] Apply for CCTNS API access via NCRB
- [ ] Register with telecom operator LEA portals
- [ ] Apply for FIU-IND access (if handling financial crimes)

### Week 5-8: Integration
- [ ] Receive API credentials
- [ ] Set up Azure Key Vault for secure storage
- [ ] Update appsettings.json with real endpoints
- [ ] Modify PhoneIntelAPIClient.cs
- [ ] Implement audit logging
- [ ] Test in sandbox environment

### Week 9-10: Testing
- [ ] Functional testing with test data
- [ ] Security audit by IT Cell
- [ ] Load testing
- [ ] Compliance review

### Week 11-12: Production
- [ ] Go-live with real APIs
- [ ] Monitor for first 2 weeks intensively
- [ ] Gather feedback from investigators
- [ ] Optimize based on usage patterns

---

## 📚 Additional Resources

### Official Documentation
- **TAFCOP:** https://tafcop.dgt.gov.in/docs
- **CCTNS:** https://cctns.gov.in/docs (Police Intranet Only)
- **FIU-IND:** https://fiuindia.gov.in/guidelines
- **CERT-In:** https://www.cert-in.org.in/PDF/LEA_API_Guide.pdf

### Training Programs
- **NCRB Training:** https://ncrb.gov.in/en/training
- **BPR&D Cyber Training:** https://bprd.nic.in/cyber-courses
- **MHA Capacity Building:** https://mha.gov.in/capacity-building

### Legal References
- **IT Act 2000:** https://www.meity.gov.in/content/information-technology-act
- **CrPC Section 91:** https://legislative.gov.in (Criminal Procedure Code)
- **DPDP Act 2023:** https://www.meity.gov.in/dpdp-act

---

## ⚠️ Important Notes

### Legal Compliance
1. **Always obtain proper legal authority** before making API calls
2. Every CDR request REQUIRES court order (Section 5(2) Telegraph Act)
3. Bank data requires Section 91 CrPC notice + court approval
4. Social media data: Valid legal process mandatory
5. Document the legal basis for EVERY API call in audit logs

### Ethical Usage
1. Use APIs only for legitimate investigations
2. Do NOT query personal contacts/family without case linkage
3. Protect data privacy - share on need-to-know basis only
4. Regular compliance audits to prevent misuse

### Data Handling
1. Never export sensitive data to personal devices
2. Delete data after case closure (per retention policy)
3. Encrypt at rest and in transit
4. Secure backups with encryption

---

## 🆘 Support Contacts

### Technical Issues
- **Your State Police IT Cell:** [Add your state IT cell contact]
- **NCRB Helpdesk:** ncrb-support@nic.in, 011-24368535
- **DoT TAFCOP:** tafcop-support@dot.gov.in
- **FIU-IND:** director@fiuindia.gov.in

### Legal Queries
- **Police Legal Cell:** [Your department legal advisor]
- **MHA Legal Section:** 011-23092736

---

## Document Control

**Version:** 1.0  
**Created:** February 12, 2026  
**Owner:** Jaswant B  
**Classification:** For Official Use Only  
**Review Date:** May 12, 2026 (Quarterly Review)

---

**End of Document**

*This guide is subject to change as API providers update their policies and procedures. Always refer to the official documentation of each service provider.*

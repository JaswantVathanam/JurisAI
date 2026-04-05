# Quick Reference: Priority APIs for Police Departments
## Essential Data Sources for Phone Intelligence System

**Target Audience:** Investigating Officers, Cyber Cell, IT Department  
**Last Updated:** February 12, 2026

---

## 🎯 START HERE - Top 5 Priority APIs

### 1. TAFCOP (Telecom Analytics) - ⭐ HIGHEST PRIORITY
**Provider:** Department of Telecommunications, Government of India  
**Why Essential:** Complete subscriber database, SIM registration, linkage with multiple numbers  
**Data:** Name, address, ID proof, all SIM cards linked to Aadhaar  
**Access:** FREE for law enforcement  
**Apply:** https://tafcop.dgt.gov.in  
**Contact:** tafcop-support@dot.gov.in  
**Timeline:** 30-45 days approval  
**Requirements:** Authorization letter from Police Commissioner/DGP

---

### 2. CCTNS (Crime Tracking) - ⭐ HIGHEST PRIORITY
**Provider:** National Crime Records Bureau (NCRB), MHA  
**Why Essential:** Already integrated with all police stations, FIR database, criminal history  
**Data:** Nationwide FIR records, arrest history, court orders, case status  
**Access:** FREE - Most officers already have login  
**Apply:** Your State Police IT Cell for API access  
**Contact:** ncrb-support@nic.in, 011-24368535  
**Timeline:** 7-15 days for API credentials  
**Requirements:** CCTNS login + API access approval from State Nodal Officer

---

### 3. Individual Telecom Operators (Airtel/Jio/Vi) - ⭐ HIGH PRIORITY
**Why Essential:** Real-time CDR data, tower dumps, location tracking  
**Data:** Call detail records, SMS logs, location history, IMEI data  
**Cost:** ₹500-2000 per CDR request (court mandated)  

**Airtel LEO Portal**
- Email: nodalofficer.leo@airtel.com
- Portal: https://leo.airtel.in
- Requirements: Court order + FIR copy

**Jio LEA Portal**
- Email: lea.support@jio.com
- Portal: https://lea.jio.com
- Requirements: Register → Upload authorization → Court order for CDR

**Vi LE Gateway**
- Email: lawenforcement@vi.com
- Requirements: Nodal Officer approval + Court order

**BSNL Government Portal**
- Email: cmd@bsnl.co.in
- FREE for police departments (government entity)

---

### 4. FIU-IND (Financial Intelligence) - ⭐ MEDIUM-HIGH PRIORITY
**Provider:** Ministry of Finance, Government of India  
**Why Essential:** Suspicious transaction reports, money laundering detection  
**Data:** STR (Suspicious Transaction Reports), CTR (₹10L+ cash transactions), wire transfers  
**Access:** FREE for investigating agencies under PMLA Act  
**Apply:** https://fiuindia.gov.in  
**Contact:** director@fiuindia.gov.in  
**Timeline:** 15 days approval  
**Requirements:** Must be designated investigating agency, case details + court approval

---

### 5. CEIR (Equipment Identity Register) - ⭐ MEDIUM PRIORITY
**Provider:** Department of Telecommunications  
**Why Essential:** Track stolen phones, IMEI blacklisting, multi-SIM detection per IMEI  
**Data:** IMEI status, stolen/lost device tracking, SIM-to-device history  
**Access:** FREE for law enforcement  
**Apply:** https://ceir.gov.in  
**Contact:** ceir-support@dot.gov.in, Helpline: 14422  
**Timeline:** 20-30 days  
**Requirements:** Police Cybercrime Cell sponsorship + MOI approval + mandatory training

---

## 📞 EMERGENCY FAST-TRACK CONTACTS

### Life-Threatening Emergency / Terrorism
- **Meta Emergency Disclosure:** lawenforcement@fb.com (48-72 hours)
- **Google Emergency:** legal-support@google.com (mention "emergency")
- **Telecom Operators:** 24x7 nodal officers available
- **CERT-In:** 1800-11-4949 (24x7 cyber threat support)

### Standard Requests
- **NCRB Helpdesk:** 011-24368535 (9 AM - 6 PM weekdays)
- **DoT TAFCOP:** tafcop-support@dot.gov.in
- **FIU-IND:** director@fiuindia.gov.in

---

## 📋 Quick Setup Checklist

### Week 1: Preparation
- [ ] Get approval letter from Police Commissioner/DGP (official seal required)
- [ ] Prepare security clearance documents (for all IT team members)
- [ ] Identify officer who will be API administrator (will receive credentials)

### Week 2: Most Important Applications
- [ ] **TAFCOP** - Apply for telecom subscriber database access
- [ ] **CCTNS API** - Request API credentials from State IT Cell (if not already available)
- [ ] **Airtel LEO** - Register on portal and submit authorization
- [ ] **Jio LEA** - Register and upload authorization documents

### Week 3: Secondary Applications
- [ ] **CEIR** - Apply through Cybercrime Cell
- [ ] **FIU-IND** - Apply if handling economic offenses/financial crimes
- [ ] **Vi LE Gateway** - Contact nodal officer
- [ ] **BSNL** - Submit request (fastest approval for government)

### Week 4: While Waiting for Approvals
- [ ] Set up Azure Key Vault for secure API key storage
- [ ] Prepare audit logging database (mandatory for compliance)
- [ ] Security audit of your application by IT Cell
- [ ] Train investigating officers on new system

---

## 💡 Smart Strategy: Start with What's Already Available

### Option 1: Use CCTNS First (You likely already have access!)
Most police officers already have CCTNS login credentials. Your State IT Cell can provide API access within 1-2 weeks.

**What you get:**
- Criminal history database
- FIR records nationwide
- Arrest records
- Court orders
- Case status tracking

**Integration:** Easy - REST API with JSON responses

---

### Option 2: BSNL Government Services (Fastest telecom access)
BSNL provides FREE direct access to government entities without lengthy approval.

**What you get:**
- Subscriber information
- CDR (with court order)
- Location tracking
- IMEI data

**Timeline:** 7-10 days  
**Cost:** FREE

---

### Option 3: State Police Database APIs
Check if your state has its own criminal database API:

- **Maharashtra:** CIPA (Criminal Intelligence & Prediction Analytics)
- **Delhi:** CMAPS (Crime Mapping and Analytics System)
- **Karnataka:** K-COPS
- **Uttar Pradesh:** UPCOP

Contact your State Police IT Cell - these are usually available within 5-7 days.

---

## 🔒 Legal Requirements Quick Reference

### Always Required:
✅ Official letterhead with seal  
✅ Authorization from Police Commissioner/DGP  
✅ Valid FIR copy (for specific case queries)  
✅ Officer ID and designation  
✅ Purpose of data requisition  

### Court Order Required For:
⚖️ Call Detail Records (CDR)  
⚖️ SMS logs  
⚖️ Bank transaction details  
⚖️ Historical location data (tower dumps)  
⚖️ WhatsApp/encrypted messaging (even metadata)  

**Legal Basis:** 
- CrPC Section 91 (Order to produce documents)
- Section 5(2) of Telegraph Act (CDR)
- Section 69 of IT Act (Decryption/monitoring)

### No Court Order Needed For:
✅ Subscriber name and address (from TAFCOP)  
✅ Number of SIM cards linked to Aadhaar  
✅ Criminal history check (CCTNS)  
✅ IMEI status (CEIR)  
✅ Public social media posts (OSINT)  

---

## 💰 Budget Planning

### FREE Government Services (No Cost):
- TAFCOP
- CEIR
- CCTNS API access
- ICJS
- NCRB databases
- CERT-In threat intelligence
- FIU-IND access
- BSNL services (for government)

### Paid Services (Budget Required):
- **Telecom CDR Requests:** ₹500-2000 per request
- **Commercial OSINT Tools:** ₹50 lakh - 5 crore/year (optional, not essential)
- **Azure Infrastructure:** ₹15,000-25,000/month
- **API Key Vault:** ₹400/month

**Total Essential Budget:** ~₹1-2 lakh/year (mostly infrastructure + occasional CDR requests)

---

## 🚀 Recommended Implementation Phases

### Phase 1 (Month 1-2): Foundation
1. ✅ Get CCTNS API access (criminal database)
2. ✅ Apply for TAFCOP (subscriber info)
3. ✅ Register with BSNL government portal
4. ✅ Set up Azure Key Vault
5. ✅ Implement audit logging

**Result:** Can query criminal history + basic subscriber info

---

### Phase 2 (Month 3-4): Telecom Integration
1. ✅ Get credentials from Airtel, Jio, Vi
2. ✅ CEIR access approved
3. ✅ Update application with real APIs
4. ✅ Sandbox testing

**Result:** Can request CDR data with court orders

---

### Phase 3 (Month 5-6): Financial Intelligence
1. ✅ FIU-IND access (if applicable)
2. ✅ Bank LEA portal registrations
3. ✅ NPCI integration

**Result:** Can track suspicious financial transactions

---

### Phase 4 (Month 7+): Advanced Features
1. ✅ Commercial OSINT tools (optional)
2. ✅ Social media LEA portals
3. ✅ State database integrations
4. ✅ ML-based risk scoring

---

## 📞 Who to Contact First

### Your First Call: State Police IT Cell
**Ask for:**
- CCTNS API credentials (if you don't have them)
- State criminal database API access
- Help with TAFCOP application
- Authorization letter template
- Security clearance process

**Most Common Contact Patterns:**
- IT Cell → NCRB → TAFCOP approval
- IT Cell → Telecom Nodal Officers → Operator APIs
- IT Cell → MHA → FIU-IND access

---

## ⚠️ Common Mistakes to Avoid

❌ **Applying to all APIs at once** (overwhelming, hard to manage)  
✅ **Start with CCTNS and TAFCOP** (most essential)

❌ **Storing API keys in code or config files**  
✅ **Always use Azure Key Vault**

❌ **Skipping audit logging** (legal compliance violation)  
✅ **Log every API call from Day 1**

❌ **Not obtaining proper court orders for CDR**  
✅ **Section 91 CrPC + Magistrate approval mandatory**

❌ **Using personal email for applications**  
✅ **Only official police email addresses accepted**

---

## 📊 Expected Timeline

```
Week 1-2:  Approvals & Documentation
Week 3-4:  Submit applications (TAFCOP, CCTNS API, telecom operators)
Week 5-8:  Receive credentials & begin integration
Week 9-10: Testing phase
Week 11:   Production deployment
Week 12:   Training & rollout to investigators
```

**Total Time:** 3 months for full integration

---

## 🎓 Training Resources

### Mandatory Training (Before API Access):
1. **NCRB CCTNS Training:** https://ncrb.gov.in/en/training
2. **CEIR User Training:** Mandatory before credentials issued
3. **Cyber Forensics Basics:** BPR&D online courses

### Recommended:
- **IT Act 2000:** Understanding legal provisions
- **Digital Evidence Handling:** Chain of custody
- **Data Privacy:** DPDP Act 2023 compliance

---

## 📱 Mobile App Considerations

If you plan to provide mobile access to investigators:

⚠️ **Security Requirements:**
- No API keys stored on mobile devices
- All calls routed through your secure server
- Additional authentication layer (OTP/biometric)
- Screen recording/screenshot prevention
- Remote wipe capability

---

## ✅ Success Metrics

After 3 months of API integration, you should achieve:

- ✅ 90%+ reduction in manual data collection time
- ✅ Real-time subscriber info (TAFCOP): <5 seconds
- ✅ Criminal history check (CCTNS): <3 seconds
- ✅ CDR request turnaround: 24-48 hours (was 7-10 days)
- ✅ 100% audit trail compliance
- ✅ Zero unauthorized access incidents

---

## 📄 Document Checklist for Applications

Have these ready before applying:

- [ ] Official authorization letter (Police Commissioner/DGP signature)
- [ ] Officer ID cards (all team members)
- [ ] Security clearance certificates
- [ ] Organization details (police station/department)
- [ ] Use case document (why you need the API)
- [ ] Technical architecture diagram
- [ ] Data protection compliance certificate
- [ ] Non-disclosure agreements (signed)
- [ ] FIR copy (for case-specific requests)

---

## 🆘 If You Get Stuck

### Application Rejected?
**Common reasons:**
1. Incomplete documentation
2. Missing officer signatures
3. Insufficient justification
4. Security clearance pending

**Solution:** Contact the API provider's nodal officer, ask for specific missing items

### API Not Working?
**Check:**
1. API key correctly stored in Azure Key Vault?
2. Request format matches documentation?
3. Rate limits not exceeded?
4. Valid authentication headers?

**Debug:** Enable detailed logging, check HTTP response codes

### Legal Questions?
**Contact:** Your department's legal cell or Public Prosecutor

---

## 📞 Emergency Escalation Path

If urgent API access needed for active investigation:

1. **Your Superintendent of Police** (immediate supervisor)
2. **DGP/Commissioner IT Cell** (escalation point)
3. **MHA Cyber Division** (for national-level cases)
4. **Direct contacts at NCRB/DoT** (for critical cases)

---

**Document End**

**Quick Start**: Call your **State Police IT Cell** tomorrow morning → Ask for CCTNS API access + help with TAFCOP application → You'll be live with 2 essential APIs within 2-3 weeks!

---

**Version:** 1.0  
**Created:** February 12, 2026  
**Owner:** Jaswant B  
**For Official Use Only**

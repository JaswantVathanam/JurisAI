using AILegalAsst.Models;
using System.Text;
using System.Text.Json;

namespace AILegalAsst.Services;

/// <summary>
/// Service for generating legal notices for cybercrime and criminal cases
/// </summary>
public class LegalNoticeService
{
    private readonly CaseService _caseService;
    private readonly ILogger<LegalNoticeService> _logger;
    private readonly AzureAgentService _agentService;
    private readonly string _dataFilePath;
    private NoticesData _noticesData;
    private int _nextNoticeNumber = 1;

    // Pre-defined recipients for common organizations
    private readonly List<NoticeRecipient> _predefinedRecipients;

    public LegalNoticeService(CaseService caseService, ILogger<LegalNoticeService> logger, AzureAgentService agentService)
    {
        _caseService = caseService;
        _logger = logger;
        _agentService = agentService;
        _dataFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "legal_notices.json");
        _noticesData = LoadNotices();
        _predefinedRecipients = InitializePredefinedRecipients();
    }

    #region Data Persistence

    private NoticesData LoadNotices()
    {
        try
        {
            if (File.Exists(_dataFilePath))
            {
                var json = File.ReadAllText(_dataFilePath);
                var data = JsonSerializer.Deserialize<NoticesData>(json);
                if (data != null)
                {
                    _nextNoticeNumber = data.NextNoticeNumber;
                    return data;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading legal notices data");
        }
        return new NoticesData();
    }

    private void SaveNotices()
    {
        try
        {
            var directory = Path.GetDirectoryName(_dataFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            _noticesData.NextNoticeNumber = _nextNoticeNumber;
            var json = JsonSerializer.Serialize(_noticesData, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_dataFilePath, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving legal notices data");
        }
    }

    #endregion

    #region Pre-defined Recipients

    private List<NoticeRecipient> InitializePredefinedRecipients()
    {
        return new List<NoticeRecipient>
        {
            // Major Banks
            new NoticeRecipient { Name = "Nodal Officer", Designation = "Cyber Cell Nodal Officer", Organization = "State Bank of India", Address = "SBI Corporate Centre, Mumbai", Category = "Bank" },
            new NoticeRecipient { Name = "Nodal Officer", Designation = "Cyber Fraud Cell", Organization = "HDFC Bank", Address = "HDFC House, Mumbai", Category = "Bank" },
            new NoticeRecipient { Name = "Nodal Officer", Designation = "Fraud Investigation Unit", Organization = "ICICI Bank", Address = "ICICI Bank Towers, Mumbai", Category = "Bank" },
            new NoticeRecipient { Name = "Nodal Officer", Designation = "Cyber Cell", Organization = "Axis Bank", Address = "Axis House, Mumbai", Category = "Bank" },
            new NoticeRecipient { Name = "Nodal Officer", Designation = "Fraud Control", Organization = "Punjab National Bank", Address = "PNB Head Office, New Delhi", Category = "Bank" },
            new NoticeRecipient { Name = "Nodal Officer", Designation = "Cyber Security", Organization = "Bank of Baroda", Address = "Baroda Corporate Centre, Mumbai", Category = "Bank" },
            new NoticeRecipient { Name = "Nodal Officer", Designation = "Fraud Management", Organization = "Kotak Mahindra Bank", Address = "Kotak Towers, Mumbai", Category = "Bank" },
            new NoticeRecipient { Name = "Nodal Officer", Designation = "Security Operations", Organization = "Yes Bank", Address = "Yes Bank House, Mumbai", Category = "Bank" },
            new NoticeRecipient { Name = "Nodal Officer", Designation = "Cyber Cell", Organization = "IndusInd Bank", Address = "IndusInd House, Mumbai", Category = "Bank" },
            new NoticeRecipient { Name = "Nodal Officer", Designation = "Fraud Prevention", Organization = "Union Bank of India", Address = "Union Bank Bhavan, Mumbai", Category = "Bank" },

            // Payment Platforms
            new NoticeRecipient { Name = "Nodal Officer", Designation = "Law Enforcement Response", Organization = "Paytm Payments Bank", Address = "One97 Communications, Noida", Category = "Payment" },
            new NoticeRecipient { Name = "Nodal Officer", Designation = "Trust & Safety", Organization = "PhonePe", Address = "PhonePe Office, Bangalore", Category = "Payment" },
            new NoticeRecipient { Name = "Nodal Officer", Designation = "Legal & Compliance", Organization = "Google Pay (NPCI)", Address = "Google India, Hyderabad", Category = "Payment" },
            new NoticeRecipient { Name = "Nodal Officer", Designation = "Fraud Operations", Organization = "Amazon Pay", Address = "Amazon India, Bangalore", Category = "Payment" },
            new NoticeRecipient { Name = "Nodal Officer", Designation = "Risk & Compliance", Organization = "MobiKwik", Address = "MobiKwik Office, Gurugram", Category = "Payment" },
            new NoticeRecipient { Name = "Nodal Officer", Designation = "Legal Affairs", Organization = "NPCI (UPI)", Address = "NPCI Office, Mumbai", Category = "Payment" },

            // Telecom Operators
            new NoticeRecipient { Name = "Nodal Officer", Designation = "Security & Law Enforcement", Organization = "Jio (Reliance)", Address = "Reliance Corporate Park, Navi Mumbai", Category = "Telecom" },
            new NoticeRecipient { Name = "Nodal Officer", Designation = "Legal & Regulatory", Organization = "Airtel", Address = "Bharti Crescent, Gurugram", Category = "Telecom" },
            new NoticeRecipient { Name = "Nodal Officer", Designation = "Corporate Security", Organization = "Vodafone Idea (Vi)", Address = "Vi Corporate Office, Mumbai", Category = "Telecom" },
            new NoticeRecipient { Name = "Nodal Officer", Designation = "Lawful Interception", Organization = "BSNL", Address = "BSNL Corporate Office, New Delhi", Category = "Telecom" },

            // Social Media Platforms
            new NoticeRecipient { Name = "Law Enforcement Response Team", Designation = "Legal", Organization = "Meta (Facebook/Instagram/WhatsApp)", Address = "Meta Platforms, Menlo Park, CA, USA", Category = "SocialMedia" },
            new NoticeRecipient { Name = "Legal Policy", Designation = "Law Enforcement", Organization = "X (Twitter)", Address = "X Corp, San Francisco, CA, USA", Category = "SocialMedia" },
            new NoticeRecipient { Name = "Trust & Safety", Designation = "Legal Operations", Organization = "Google (YouTube)", Address = "Google LLC, Mountain View, CA, USA", Category = "SocialMedia" },
            new NoticeRecipient { Name = "Law Enforcement", Designation = "Trust & Safety", Organization = "Telegram", Address = "Telegram FZ-LLC, Dubai, UAE", Category = "SocialMedia" },
            new NoticeRecipient { Name = "Safety Team", Designation = "Legal", Organization = "Snapchat", Address = "Snap Inc, Santa Monica, CA, USA", Category = "SocialMedia" },
            new NoticeRecipient { Name = "Legal Response", Designation = "Trust & Safety", Organization = "LinkedIn", Address = "LinkedIn Corporation, Sunnyvale, CA, USA", Category = "SocialMedia" },

            // E-commerce
            new NoticeRecipient { Name = "Nodal Officer", Designation = "Legal & Compliance", Organization = "Amazon India", Address = "Amazon Development Centre, Bangalore", Category = "Ecommerce" },
            new NoticeRecipient { Name = "Nodal Officer", Designation = "Trust & Safety", Organization = "Flipkart", Address = "Flipkart Campus, Bangalore", Category = "Ecommerce" },
            new NoticeRecipient { Name = "Legal Team", Designation = "Compliance", Organization = "Meesho", Address = "Meesho Office, Bangalore", Category = "Ecommerce" },

            // Internet Service Providers
            new NoticeRecipient { Name = "Nodal Officer", Designation = "Security Operations", Organization = "ACT Fibernet", Address = "Atria Convergence, Bangalore", Category = "ISP" },
            new NoticeRecipient { Name = "Nodal Officer", Designation = "Legal", Organization = "Hathway", Address = "Hathway Cable, Mumbai", Category = "ISP" },
        };
    }

    public List<NoticeRecipient> GetRecipientsByCategory(string category)
    {
        return _predefinedRecipients.Where(r => r.Category == category).ToList();
    }

    public List<string> GetRecipientCategories()
    {
        return _predefinedRecipients.Select(r => r.Category).Distinct().ToList();
    }

    public List<NoticeRecipient> GetAllRecipients() => _predefinedRecipients;

    #endregion

    #region Notice Generation

    public async Task<LegalNotice> GenerateNoticeAsync(LegalNoticeRequest request, User officer)
    {
        var caseData = await _caseService.GetCaseByIdAsync(request.CaseId);
        if (caseData == null)
        {
            throw new ArgumentException("Case not found");
        }

        var notice = new LegalNotice
        {
            Id = _noticesData.Notices.Count + 1,
            NoticeNumber = GenerateNoticeNumber(request.Type),
            Type = request.Type,
            CaseId = request.CaseId,
            CaseNumber = caseData.CaseNumber,
            FIRNumber = caseData.FIRNumber ?? "Pending",
            GeneratedDate = DateTime.Now,
            GeneratedBy = officer.Name,
            SenderName = officer.Name,
            SenderDesignation = "Police Officer", // Can be enhanced with officer rank
            SenderStation = "Cyber Crime Police Station", // Can be configured
            SenderContact = officer.Email,
            AccountNumbers = request.AccountNumbers,
            PhoneNumbers = request.PhoneNumbers,
            UPIIds = request.UPIIds,
            SocialMediaHandles = request.SocialMediaHandles,
            IPAddresses = request.IPAddresses,
            FromDate = request.FromDate,
            ToDate = request.ToDate
        };

        // Set recipient
        if (request.CustomRecipient != null)
        {
            notice.RecipientName = request.CustomRecipient.Name;
            notice.RecipientDesignation = request.CustomRecipient.Designation;
            notice.RecipientOrganization = request.CustomRecipient.Organization;
            notice.RecipientAddress = request.CustomRecipient.Address;
        }

        // Generate notice content based on type
        var (subject, body) = await GenerateNoticeContentAsync(notice, caseData, request.AdditionalDetails);
        notice.Subject = subject;
        notice.Body = body;

        // Save notice
        _noticesData.Notices.Add(notice);
        SaveNotices();

        _logger.LogInformation("Generated {Type} notice {NoticeNumber} for case {CaseNumber}", 
            request.Type, notice.NoticeNumber, caseData.CaseNumber);

        return notice;
    }

    private string GenerateNoticeNumber(LegalNoticeType type)
    {
        var prefix = type switch
        {
            LegalNoticeType.BankAccountFreeze => "BAF",
            LegalNoticeType.CDRRequest => "CDR",
            LegalNoticeType.SocialMediaTakedown => "SMT",
            LegalNoticeType.UPIWalletFreeze => "UPI",
            LegalNoticeType.WitnessSummons => "WIT",
            LegalNoticeType.CourtFilingCover => "CRT",
            LegalNoticeType.VictimStatusUpdate => "VSU",
            LegalNoticeType.IPAddressRequest => "IPR",
            LegalNoticeType.MerchantDetailsRequest => "MDR",
            _ => "GEN"
        };

        return $"{prefix}/{DateTime.Now:yyyy}/{_nextNoticeNumber++:D4}";
    }

    private async Task<(string subject, string body)> GenerateNoticeContentAsync(LegalNotice notice, Case caseData, string? additionalDetails)
    {
        // Try AI-powered notice generation
        if (_agentService.IsReady)
        {
            try
            {
                var prompt = "ROLE: You are a legal aid assistant integrated into an official Indian legal assistance platform " +
                    "used by citizens, lawyers, and law enforcement officers. Your purpose is to help law enforcement " +
                    "generate official legal notices for cybercrime and criminal investigations. This is lawful legal " +
                    "assistance — not harmful content.\n\n" +
                    $"TASK: Draft a formal legal notice for Indian law enforcement with the following details:\n" +
                    $"Notice Type: {notice.Type}\n" +
                    $"Case Number: {caseData.CaseNumber}\n" +
                    $"FIR Number: {notice.FIRNumber}\n" +
                    $"Sender: {notice.SenderName}, {notice.SenderDesignation}, {notice.SenderStation}\n" +
                    $"Recipient: {notice.RecipientName}, {notice.RecipientOrganization}\n" +
                    $"Account Numbers: {string.Join(", ", notice.AccountNumbers ?? new List<string>())}\n" +
                    $"Phone Numbers: {string.Join(", ", notice.PhoneNumbers ?? new List<string>())}\n" +
                    $"UPI IDs: {string.Join(", ", notice.UPIIds ?? new List<string>())}\n" +
                    $"Date Range: {notice.FromDate:dd/MM/yyyy} to {notice.ToDate:dd/MM/yyyy}\n" +
                    $"Additional Details: {additionalDetails ?? "None"}\n\n" +
                    "Format response as:\n---SUBJECT---\n[subject line]\n---BODY---\n[full formal notice body with proper legal language, relevant Indian law sections, and formal salutations]";

                var context = "You are an Indian legal notice drafting expert working within an authorized law enforcement platform. " +
                    "Generate formal, legally compliant notices under BNSS, IT Act 2000, IPC/BNS, and CrPC. Use proper legal terminology and formatting.";

                var response = await _agentService.SendMessageAsync(prompt, context);
                if (response.Success && !string.IsNullOrWhiteSpace(response.Message))
                {
                    var parts = response.Message.Split("---BODY---", StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2)
                    {
                        var subject = parts[0].Replace("---SUBJECT---", "").Trim();
                        var body = parts[1].Trim();
                        if (subject.Length > 10 && body.Length > 50)
                            return (subject, body);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AI-powered notice generation failed, using templates");
            }
        }

        // Fallback: Template-based notice generation
        return notice.Type switch
        {
            LegalNoticeType.BankAccountFreeze => GenerateBankFreezeNotice(notice, caseData, additionalDetails),
            LegalNoticeType.CDRRequest => GenerateCDRRequestNotice(notice, caseData, additionalDetails),
            LegalNoticeType.SocialMediaTakedown => GenerateSocialMediaTakedownNotice(notice, caseData, additionalDetails),
            LegalNoticeType.UPIWalletFreeze => GenerateUPIFreezeNotice(notice, caseData, additionalDetails),
            LegalNoticeType.WitnessSummons => GenerateWitnessSummonsNotice(notice, caseData, additionalDetails),
            LegalNoticeType.IPAddressRequest => GenerateIPAddressRequestNotice(notice, caseData, additionalDetails),
            LegalNoticeType.MerchantDetailsRequest => GenerateMerchantDetailsNotice(notice, caseData, additionalDetails),
            LegalNoticeType.VictimStatusUpdate => GenerateVictimStatusNotice(notice, caseData, additionalDetails),
            _ => GenerateGenericNotice(notice, caseData, additionalDetails)
        };
    }

    #endregion

    #region Notice Templates

    private (string, string) GenerateBankFreezeNotice(LegalNotice notice, Case caseData, string? additionalDetails)
    {
        var subject = $"Request for Immediate Freezing of Bank Account(s) - Case No. {caseData.CaseNumber}";
        
        var sb = new StringBuilder();
        sb.AppendLine("Sir/Madam,");
        sb.AppendLine();
        sb.AppendLine($"Sub: Request for immediate freezing of bank account(s) in connection with {(caseData.IsCybercrime ? "Cybercrime" : "Criminal")} Case No. {caseData.CaseNumber}");
        sb.AppendLine();
        sb.AppendLine($"Ref: FIR No. {notice.FIRNumber} dated {caseData.FiledDate:dd/MM/yyyy}");
        sb.AppendLine();
        sb.AppendLine("I am directed to inform you that the above-mentioned case is registered at this Police Station under the following sections:");
        sb.AppendLine();
        
        if (caseData.Sections?.Any() == true)
        {
            sb.AppendLine($"**Sections:** {string.Join(", ", caseData.Sections)}");
        }
        else
        {
            sb.AppendLine("**Sections:** Under Investigation");
        }
        
        sb.AppendLine();
        sb.AppendLine("**Brief Facts of the Case:**");
        sb.AppendLine(caseData.Description);
        sb.AppendLine();
        sb.AppendLine("During the course of investigation, the following bank account(s) have been identified as being used for receiving/transferring the proceeds of crime:");
        sb.AppendLine();
        
        foreach (var account in notice.AccountNumbers)
        {
            sb.AppendLine($"• Account Number: **{account}**");
        }
        
        sb.AppendLine();
        sb.AppendLine("You are hereby requested to:");
        sb.AppendLine();
        sb.AppendLine("1. **IMMEDIATELY FREEZE** the above-mentioned account(s) and put a lien on the available balance.");
        sb.AppendLine("2. Provide the following information within **72 hours**:");
        sb.AppendLine("   - Account holder's name, address, and KYC documents");
        sb.AppendLine("   - Account opening date and branch details");
        sb.AppendLine("   - Account statement from the date of opening till date");
        sb.AppendLine("   - Current available balance");
        sb.AppendLine("   - Details of all linked accounts/cards/UPI IDs");
        sb.AppendLine("   - IP addresses used for net banking transactions");
        sb.AppendLine();
        sb.AppendLine("3. Do not allow any debit transactions from the account(s) until further orders.");
        sb.AppendLine();
        sb.AppendLine("This request is made under Section 91 CrPC (now Section 94 BNSS, 2023) and RBI Master Circular on Fraud Classification.");
        sb.AppendLine();
        
        if (!string.IsNullOrEmpty(additionalDetails))
        {
            sb.AppendLine("**Additional Information:**");
            sb.AppendLine(additionalDetails);
            sb.AppendLine();
        }
        
        sb.AppendLine("Your immediate action in this matter is solicited as the investigation is time-sensitive.");
        sb.AppendLine();
        sb.AppendLine("Kindly acknowledge receipt of this letter and confirm the action taken.");

        return (subject, sb.ToString());
    }

    private (string, string) GenerateCDRRequestNotice(LegalNotice notice, Case caseData, string? additionalDetails)
    {
        var subject = $"Request for Call Detail Records (CDR) / IPDR - Case No. {caseData.CaseNumber}";
        
        var sb = new StringBuilder();
        sb.AppendLine("Sir/Madam,");
        sb.AppendLine();
        sb.AppendLine($"Sub: Request for Call Detail Records (CDR) and Internet Protocol Detail Records (IPDR)");
        sb.AppendLine();
        sb.AppendLine($"Ref: FIR No. {notice.FIRNumber} dated {caseData.FiledDate:dd/MM/yyyy}");
        sb.AppendLine();
        sb.AppendLine($"In connection with the investigation of the above-mentioned {(caseData.IsCybercrime ? "Cybercrime" : "Criminal")} case, you are requested to provide the Call Detail Records (CDR) and Internet Protocol Detail Records (IPDR) for the following mobile number(s):");
        sb.AppendLine();
        
        foreach (var phone in notice.PhoneNumbers)
        {
            sb.AppendLine($"• Mobile Number: **{phone}**");
        }
        
        sb.AppendLine();
        sb.AppendLine("**Period Required:**");
        sb.AppendLine($"From: {notice.FromDate?.ToString("dd/MM/yyyy") ?? caseData.FiledDate.AddMonths(-3).ToString("dd/MM/yyyy")}");
        sb.AppendLine($"To: {notice.ToDate?.ToString("dd/MM/yyyy") ?? DateTime.Now.ToString("dd/MM/yyyy")}");
        sb.AppendLine();
        sb.AppendLine("**Information Required:**");
        sb.AppendLine();
        sb.AppendLine("1. **CDR Details:**");
        sb.AppendLine("   - Complete incoming and outgoing call records");
        sb.AppendLine("   - SMS details (incoming and outgoing)");
        sb.AppendLine("   - Cell tower location details with lat/long coordinates");
        sb.AppendLine("   - IMEI number(s) used with the SIM");
        sb.AppendLine();
        sb.AppendLine("2. **Subscriber Details:**");
        sb.AppendLine("   - Customer Application Form (CAF)");
        sb.AppendLine("   - KYC documents submitted");
        sb.AppendLine("   - Activation date and dealer details");
        sb.AppendLine("   - Alternate contact numbers provided");
        sb.AppendLine();
        sb.AppendLine("3. **IPDR Details (if data services used):**");
        sb.AppendLine("   - Data usage logs with IP addresses");
        sb.AppendLine("   - Timestamps of internet sessions");
        sb.AppendLine();
        sb.AppendLine("4. **Recharge Details:**");
        sb.AppendLine("   - All recharge transactions with mode of payment");
        sb.AppendLine("   - Retailer details for physical recharges");
        sb.AppendLine();
        sb.AppendLine("This request is made under Section 91 CrPC (now Section 94 BNSS, 2023) and IT Act, 2000.");
        sb.AppendLine();
        
        if (!string.IsNullOrEmpty(additionalDetails))
        {
            sb.AppendLine("**Additional Information:**");
            sb.AppendLine(additionalDetails);
            sb.AppendLine();
        }
        
        sb.AppendLine("Please provide the information in **Excel/CSV format** within **7 working days**.");
        sb.AppendLine();
        sb.AppendLine("Your cooperation in this investigation is appreciated.");

        return (subject, sb.ToString());
    }

    private (string, string) GenerateSocialMediaTakedownNotice(LegalNotice notice, Case caseData, string? additionalDetails)
    {
        var subject = $"Request for Account Information and Content Takedown - Case No. {caseData.CaseNumber}";
        
        var sb = new StringBuilder();
        sb.AppendLine("To,");
        sb.AppendLine("The Law Enforcement Response Team,");
        sb.AppendLine();
        sb.AppendLine("Sir/Madam,");
        sb.AppendLine();
        sb.AppendLine($"Sub: Request for disclosure of account information and takedown of illegal content");
        sb.AppendLine();
        sb.AppendLine($"Ref: Case No. {caseData.CaseNumber}, FIR No. {notice.FIRNumber}");
        sb.AppendLine();
        sb.AppendLine("We are investigating a cybercrime case registered at our Police Station. The following social media account(s)/content have been identified as being used for illegal activities:");
        sb.AppendLine();
        
        foreach (var handle in notice.SocialMediaHandles)
        {
            sb.AppendLine($"• Account/URL: **{handle}**");
        }
        
        sb.AppendLine();
        sb.AppendLine("**Nature of Offence:**");
        sb.AppendLine(caseData.Description);
        sb.AppendLine();
        sb.AppendLine("**Applicable Laws:**");
        if (caseData.Sections?.Any() == true)
        {
            sb.AppendLine($"{string.Join(", ", caseData.Sections)}");
        }
        sb.AppendLine("Information Technology Act, 2000 - Sections 66, 66C, 66D, 67");
        sb.AppendLine();
        sb.AppendLine("**We Request the Following:**");
        sb.AppendLine();
        sb.AppendLine("1. **Account Information:**");
        sb.AppendLine("   - Registration details (email, phone number used)");
        sb.AppendLine("   - IP addresses used for account creation and access");
        sb.AppendLine("   - Login history with timestamps and locations");
        sb.AppendLine("   - Linked accounts or pages");
        sb.AppendLine();
        sb.AppendLine("2. **Content Preservation:**");
        sb.AppendLine("   - Preserve all data associated with the account(s)");
        sb.AppendLine("   - Messages, posts, and media shared");
        sb.AppendLine();
        sb.AppendLine("3. **Content Takedown:**");
        sb.AppendLine("   - Remove/disable the account(s) as they are being used for criminal activities");
        sb.AppendLine();
        
        if (!string.IsNullOrEmpty(additionalDetails))
        {
            sb.AppendLine("**Additional Information:**");
            sb.AppendLine(additionalDetails);
            sb.AppendLine();
        }
        
        sb.AppendLine("This is an official law enforcement request. Please expedite the response as the investigation is ongoing.");
        sb.AppendLine();
        sb.AppendLine("For verification, please contact:");
        sb.AppendLine($"Officer: {notice.SenderName}");
        sb.AppendLine($"Email: {notice.SenderContact}");

        return (subject, sb.ToString());
    }

    private (string, string) GenerateUPIFreezeNotice(LegalNotice notice, Case caseData, string? additionalDetails)
    {
        var subject = $"Request for Freezing UPI ID(s) / Digital Wallet(s) - Case No. {caseData.CaseNumber}";
        
        var sb = new StringBuilder();
        sb.AppendLine("Sir/Madam,");
        sb.AppendLine();
        sb.AppendLine($"Sub: Urgent request for freezing UPI ID(s) and Digital Wallet(s) - Cybercrime Investigation");
        sb.AppendLine();
        sb.AppendLine($"Ref: FIR No. {notice.FIRNumber} dated {caseData.FiledDate:dd/MM/yyyy}");
        sb.AppendLine();
        sb.AppendLine("During the investigation of the above cybercrime case, the following UPI ID(s)/Digital Wallet(s) have been identified as being used for fraudulent transactions:");
        sb.AppendLine();
        
        foreach (var upi in notice.UPIIds)
        {
            sb.AppendLine($"• UPI ID: **{upi}**");
        }
        
        sb.AppendLine();
        sb.AppendLine("**Brief Facts:**");
        sb.AppendLine(caseData.Description);
        sb.AppendLine();
        sb.AppendLine("**Immediate Action Required:**");
        sb.AppendLine();
        sb.AppendLine("1. **FREEZE** the UPI ID(s)/Wallet(s) immediately to prevent further transactions");
        sb.AppendLine("2. Provide KYC details of the account holder(s)");
        sb.AppendLine("3. Provide complete transaction history");
        sb.AppendLine("4. Details of linked bank accounts");
        sb.AppendLine("5. Device information used for transactions");
        sb.AppendLine("6. Merchant details if business account");
        sb.AppendLine();
        sb.AppendLine("This request is made under Section 91 CrPC / Section 94 BNSS and NPCI guidelines for fraud reporting.");
        sb.AppendLine();
        
        if (!string.IsNullOrEmpty(additionalDetails))
        {
            sb.AppendLine("**Additional Information:**");
            sb.AppendLine(additionalDetails);
            sb.AppendLine();
        }
        
        sb.AppendLine("Time is of essence. Please act immediately to prevent dissipation of funds.");

        return (subject, sb.ToString());
    }

    private (string, string) GenerateWitnessSummonsNotice(LegalNotice notice, Case caseData, string? additionalDetails)
    {
        var subject = $"Summons to Appear as Witness - Case No. {caseData.CaseNumber}";
        
        var sb = new StringBuilder();
        sb.AppendLine("**SUMMONS UNDER SECTION 160 CrPC / SECTION 175 BNSS, 2023**");
        sb.AppendLine();
        sb.AppendLine($"Case No.: {caseData.CaseNumber}");
        sb.AppendLine($"FIR No.: {notice.FIRNumber}");
        sb.AppendLine();
        sb.AppendLine("To,");
        sb.AppendLine($"{notice.RecipientName}");
        sb.AppendLine($"{notice.RecipientAddress}");
        sb.AppendLine();
        sb.AppendLine("Sir/Madam,");
        sb.AppendLine();
        sb.AppendLine("WHEREAS a case is pending investigation at this Police Station;");
        sb.AppendLine();
        sb.AppendLine("AND WHEREAS your presence is required for recording your statement as a witness in connection with the said case;");
        sb.AppendLine();
        sb.AppendLine("YOU ARE HEREBY SUMMONED to appear before the undersigned at:");
        sb.AppendLine();
        sb.AppendLine($"**Place:** {notice.SenderStation}");
        sb.AppendLine($"**Date:** ________________");
        sb.AppendLine($"**Time:** ________________");
        sb.AppendLine();
        sb.AppendLine("Please bring the following documents (if any):");
        sb.AppendLine("- Any documents/evidence related to the case");
        sb.AppendLine("- Valid ID proof");
        sb.AppendLine();
        sb.AppendLine("Failure to comply with this summons without lawful excuse shall render you liable for action under Section 174 IPC / Section 229 BNS, 2023.");
        sb.AppendLine();
        
        if (!string.IsNullOrEmpty(additionalDetails))
        {
            sb.AppendLine("**Note:**");
            sb.AppendLine(additionalDetails);
            sb.AppendLine();
        }

        return (subject, sb.ToString());
    }

    private (string, string) GenerateIPAddressRequestNotice(LegalNotice notice, Case caseData, string? additionalDetails)
    {
        var subject = $"Request for IP Address Details - Case No. {caseData.CaseNumber}";
        
        var sb = new StringBuilder();
        sb.AppendLine("Sir/Madam,");
        sb.AppendLine();
        sb.AppendLine($"Sub: Request for subscriber details against IP Address(es) - Cybercrime Investigation");
        sb.AppendLine();
        sb.AppendLine($"Ref: FIR No. {notice.FIRNumber} dated {caseData.FiledDate:dd/MM/yyyy}");
        sb.AppendLine();
        sb.AppendLine("In connection with the investigation of the above cybercrime case, you are requested to provide subscriber details for the following IP Address(es):");
        sb.AppendLine();
        
        foreach (var ip in notice.IPAddresses)
        {
            sb.AppendLine($"• IP Address: **{ip}**");
        }
        
        if (notice.FromDate.HasValue || notice.ToDate.HasValue)
        {
            sb.AppendLine();
            sb.AppendLine($"**Timeframe:** {notice.FromDate?.ToString("dd/MM/yyyy HH:mm")} to {notice.ToDate?.ToString("dd/MM/yyyy HH:mm")}");
        }
        
        sb.AppendLine();
        sb.AppendLine("**Information Required:**");
        sb.AppendLine();
        sb.AppendLine("1. Subscriber name and complete address");
        sb.AppendLine("2. Contact details (phone, email)");
        sb.AppendLine("3. KYC documents");
        sb.AppendLine("4. MAC address of the device used");
        sb.AppendLine("5. Connection type (Broadband/Mobile/Leased Line)");
        sb.AppendLine("6. Session logs for the specified period");
        sb.AppendLine();
        sb.AppendLine("This request is made under Section 91 CrPC / Section 94 BNSS and Section 69 of IT Act, 2000.");
        sb.AppendLine();
        
        if (!string.IsNullOrEmpty(additionalDetails))
        {
            sb.AppendLine("**Additional Information:**");
            sb.AppendLine(additionalDetails);
            sb.AppendLine();
        }
        
        sb.AppendLine("Please provide the information within **7 working days**.");

        return (subject, sb.ToString());
    }

    private (string, string) GenerateMerchantDetailsNotice(LegalNotice notice, Case caseData, string? additionalDetails)
    {
        var subject = $"Request for Merchant/Seller Details - Case No. {caseData.CaseNumber}";
        
        var sb = new StringBuilder();
        sb.AppendLine("Sir/Madam,");
        sb.AppendLine();
        sb.AppendLine($"Sub: Request for Merchant/Seller details in connection with online fraud case");
        sb.AppendLine();
        sb.AppendLine($"Ref: FIR No. {notice.FIRNumber} dated {caseData.FiledDate:dd/MM/yyyy}");
        sb.AppendLine();
        sb.AppendLine("We are investigating an online fraud case where the victim was cheated through your platform. Please provide the following details of the seller/merchant involved:");
        sb.AppendLine();
        
        if (notice.SocialMediaHandles.Any())
        {
            sb.AppendLine("**Merchant/Seller Account(s):**");
            foreach (var handle in notice.SocialMediaHandles)
            {
                sb.AppendLine($"• {handle}");
            }
        }
        
        sb.AppendLine();
        sb.AppendLine("**Information Required:**");
        sb.AppendLine();
        sb.AppendLine("1. Merchant registration details and KYC documents");
        sb.AppendLine("2. Bank account details for settlements");
        sb.AppendLine("3. Contact details (phone, email, address)");
        sb.AppendLine("4. Transaction history with complainant");
        sb.AppendLine("5. IP addresses used for account access");
        sb.AppendLine("6. Order details and delivery information");
        sb.AppendLine("7. Grievance history against the merchant");
        sb.AppendLine();
        
        if (!string.IsNullOrEmpty(additionalDetails))
        {
            sb.AppendLine("**Additional Information:**");
            sb.AppendLine(additionalDetails);
            sb.AppendLine();
        }
        
        sb.AppendLine("Please suspend the merchant account pending investigation.");

        return (subject, sb.ToString());
    }

    private (string, string) GenerateVictimStatusNotice(LegalNotice notice, Case caseData, string? additionalDetails)
    {
        var subject = $"Status Update on Your Complaint - Case No. {caseData.CaseNumber}";
        
        var sb = new StringBuilder();
        sb.AppendLine("Dear Complainant,");
        sb.AppendLine();
        sb.AppendLine($"This is to update you on the status of your complaint registered with us.");
        sb.AppendLine();
        sb.AppendLine($"**Case Number:** {caseData.CaseNumber}");
        sb.AppendLine($"**FIR Number:** {notice.FIRNumber}");
        sb.AppendLine($"**Date of Filing:** {caseData.FiledDate:dd MMMM yyyy}");
        sb.AppendLine($"**Current Status:** {caseData.Status}");
        sb.AppendLine();
        sb.AppendLine("**Investigation Progress:**");
        sb.AppendLine();
        sb.AppendLine("The following actions have been taken in your case:");
        sb.AppendLine();
        sb.AppendLine("1. FIR has been registered and investigation initiated");
        sb.AppendLine("2. Notices sent to relevant banks/telecom/platforms");
        sb.AppendLine("3. Evidence collection in progress");
        sb.AppendLine();
        
        if (!string.IsNullOrEmpty(additionalDetails))
        {
            sb.AppendLine("**Latest Update:**");
            sb.AppendLine(additionalDetails);
            sb.AppendLine();
        }
        
        sb.AppendLine("**What You Should Do:**");
        sb.AppendLine("- Do not engage with the accused/scammers");
        sb.AppendLine("- Preserve all evidence (messages, screenshots)");
        sb.AppendLine("- Report any new contact from suspects immediately");
        sb.AppendLine();
        sb.AppendLine("For any queries, please contact:");
        sb.AppendLine($"Investigating Officer: {notice.SenderName}");
        sb.AppendLine($"Contact: {notice.SenderContact}");
        sb.AppendLine();
        sb.AppendLine("We assure you of our best efforts in resolving your case.");

        return (subject, sb.ToString());
    }

    private (string, string) GenerateGenericNotice(LegalNotice notice, Case caseData, string? additionalDetails)
    {
        var subject = $"Official Notice - Case No. {caseData.CaseNumber}";
        
        var sb = new StringBuilder();
        sb.AppendLine("Sir/Madam,");
        sb.AppendLine();
        sb.AppendLine($"Ref: Case No. {caseData.CaseNumber}, FIR No. {notice.FIRNumber}");
        sb.AppendLine();
        sb.AppendLine("This is an official communication regarding the above-referenced case.");
        sb.AppendLine();
        sb.AppendLine($"**Case Details:**");
        sb.AppendLine($"- Type: {caseData.Type}");
        sb.AppendLine($"- Filed Date: {caseData.FiledDate:dd/MM/yyyy}");
        sb.AppendLine($"- Status: {caseData.Status}");
        sb.AppendLine();
        
        if (!string.IsNullOrEmpty(additionalDetails))
        {
            sb.AppendLine(additionalDetails);
            sb.AppendLine();
        }
        
        sb.AppendLine("Your cooperation is requested in this matter.");

        return (subject, sb.ToString());
    }

    #endregion

    #region Notice Management

    public Task<List<LegalNotice>> GetNoticesByCaseAsync(int caseId)
    {
        var notices = _noticesData.Notices.Where(n => n.CaseId == caseId).ToList();
        return Task.FromResult(notices);
    }

    public Task<List<LegalNotice>> GetNoticesByOfficerAsync(string officerEmail)
    {
        var notices = _noticesData.Notices.Where(n => n.SenderContact == officerEmail).ToList();
        return Task.FromResult(notices);
    }

    public Task<LegalNotice?> GetNoticeByIdAsync(int id)
    {
        var notice = _noticesData.Notices.FirstOrDefault(n => n.Id == id);
        return Task.FromResult(notice);
    }

    public Task MarkNoticeSentAsync(int noticeId)
    {
        var notice = _noticesData.Notices.FirstOrDefault(n => n.Id == noticeId);
        if (notice != null)
        {
            notice.IsSent = true;
            notice.SentDate = DateTime.Now;
            SaveNotices();
        }
        return Task.CompletedTask;
    }

    public Task UpdateNoticeResponseAsync(int noticeId, string response)
    {
        var notice = _noticesData.Notices.FirstOrDefault(n => n.Id == noticeId);
        if (notice != null)
        {
            notice.ResponseReceived = response;
            SaveNotices();
        }
        return Task.CompletedTask;
    }

    public Task<List<LegalNotice>> GetAllNoticesAsync()
    {
        return Task.FromResult(_noticesData.Notices.OrderByDescending(n => n.GeneratedDate).ToList());
    }

    #endregion
}

#region Data Models

public class NoticesData
{
    public List<LegalNotice> Notices { get; set; } = new();
    public int NextNoticeNumber { get; set; } = 1;
}

#endregion

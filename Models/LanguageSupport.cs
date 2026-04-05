namespace AILegalAsst.Models;

/// <summary>
/// Language Support Model for Multi-Language Features
/// </summary>
public class LanguageSupport
{
    public static readonly Dictionary<string, LanguageInfo> SupportedLanguages = new()
    {
        { "en", new LanguageInfo { Code = "en", Name = "English", NativeName = "English", IsRTL = false, Icon = "🇬🇧" } },
        { "ta", new LanguageInfo { Code = "ta", Name = "Tamil", NativeName = "தமிழ்", IsRTL = false, Icon = "🇮🇳" } },
        { "hi", new LanguageInfo { Code = "hi", Name = "Hindi", NativeName = "हिन्दी", IsRTL = false, Icon = "🇮🇳" } },
        { "mr", new LanguageInfo { Code = "mr", Name = "Marathi", NativeName = "मराठी", IsRTL = false, Icon = "🇮🇳" } },
        { "te", new LanguageInfo { Code = "te", Name = "Telugu", NativeName = "తెలుగు", IsRTL = false, Icon = "🇮🇳" } },
        { "bn", new LanguageInfo { Code = "bn", Name = "Bengali", NativeName = "বাংলা", IsRTL = false, Icon = "🇮🇳" } },
        { "gu", new LanguageInfo { Code = "gu", Name = "Gujarati", NativeName = "ગુજરાતી", IsRTL = false, Icon = "🇮🇳" } },
        { "kn", new LanguageInfo { Code = "kn", Name = "Kannada", NativeName = "ಕನ್ನಡ", IsRTL = false, Icon = "🇮🇳" } },
        { "ml", new LanguageInfo { Code = "ml", Name = "Malayalam", NativeName = "മലയാളം", IsRTL = false, Icon = "🇮🇳" } },
        { "pa", new LanguageInfo { Code = "pa", Name = "Punjabi", NativeName = "ਪੰਜਾਬੀ", IsRTL = false, Icon = "🇮🇳" } },
        { "or", new LanguageInfo { Code = "or", Name = "Odia", NativeName = "ଓଡ଼ିଆ", IsRTL = false, Icon = "🇮🇳" } },
        { "ur", new LanguageInfo { Code = "ur", Name = "Urdu", NativeName = "اردو", IsRTL = true, Icon = "🇮🇳" } }
    };

    public static readonly Dictionary<string, string> IndianStates = new()
    {
        { "AN", "Andaman and Nicobar Islands" },
        { "AP", "Andhra Pradesh" },
        { "AR", "Arunachal Pradesh" },
        { "AS", "Assam" },
        { "BR", "Bihar" },
        { "CH", "Chandigarh" },
        { "CT", "Chhattisgarh" },
        { "DN", "Dadra and Nagar Haveli and Daman and Diu" },
        { "DL", "Delhi" },
        { "GA", "Goa" },
        { "GJ", "Gujarat" },
        { "HR", "Haryana" },
        { "HP", "Himachal Pradesh" },
        { "JK", "Jammu and Kashmir" },
        { "JH", "Jharkhand" },
        { "KA", "Karnataka" },
        { "KL", "Kerala" },
        { "LA", "Ladakh" },
        { "LD", "Lakshadweep" },
        { "MP", "Madhya Pradesh" },
        { "MH", "Maharashtra" },
        { "MN", "Manipur" },
        { "ML", "Meghalaya" },
        { "MZ", "Mizoram" },
        { "NL", "Nagaland" },
        { "OR", "Odisha" },
        { "PY", "Puducherry" },
        { "PB", "Punjab" },
        { "RJ", "Rajasthan" },
        { "SK", "Sikkim" },
        { "TN", "Tamil Nadu" },
        { "TG", "Telangana" },
        { "TR", "Tripura" },
        { "UP", "Uttar Pradesh" },
        { "UK", "Uttarakhand" },
        { "WB", "West Bengal" }
    };

    public static readonly Dictionary<string, List<string>> MajorBanks = new()
    {
        { "PSU", new List<string> {
            "State Bank of India (SBI)",
            "Punjab National Bank",
            "Bank of Baroda",
            "Canara Bank",
            "Union Bank of India",
            "Bank of India",
            "Indian Bank",
            "Central Bank of India",
            "Indian Overseas Bank",
            "UCO Bank",
            "Bank of Maharashtra",
            "Punjab & Sind Bank"
        }},
        { "Private", new List<string> {
            "HDFC Bank",
            "ICICI Bank",
            "Axis Bank",
            "Kotak Mahindra Bank",
            "IndusInd Bank",
            "Yes Bank",
            "IDBI Bank",
            "Federal Bank",
            "RBL Bank",
            "South Indian Bank",
            "Bandhan Bank",
            "IDFC First Bank"
        }},
        { "Payments", new List<string> {
            "Paytm Payments Bank",
            "Airtel Payments Bank",
            "India Post Payments Bank",
            "Fino Payments Bank",
            "Jio Payments Bank"
        }}
    };
}

public class LanguageInfo
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string NativeName { get; set; } = string.Empty;
    public bool IsRTL { get; set; } = false;
    public string Icon { get; set; } = string.Empty;
}

/// <summary>
/// Legal Sections Reference for Indian Laws
/// </summary>
public static class LegalSections
{
    public static readonly Dictionary<FIRCrimeType, List<LegalSection>> ApplicableSections = new()
    {
        { FIRCrimeType.CyberCrime, new List<LegalSection> {
            new() { Code = "IT Act Sec 66", Title = "Computer Related Offences", Description = "Punishment for computer related offences", Punishment = "Up to 3 years imprisonment and/or fine up to ₹5 lakh" },
            new() { Code = "IT Act Sec 66C", Title = "Identity Theft", Description = "Punishment for identity theft using electronic signature, password", Punishment = "Up to 3 years imprisonment and fine up to ₹1 lakh" },
            new() { Code = "IT Act Sec 66D", Title = "Cheating by Personation", Description = "Punishment for cheating by personation using computer resource", Punishment = "Up to 3 years imprisonment and fine up to ₹1 lakh" },
            new() { Code = "BNS Sec 318", Title = "Cheating (formerly IPC 420)", Description = "Cheating and dishonestly inducing delivery of property", Punishment = "Up to 7 years imprisonment and fine" },
            new() { Code = "BNS Sec 319", Title = "Cheating by Personation", Description = "Cheating by personation", Punishment = "Up to 5 years imprisonment and fine" }
        }},
        { FIRCrimeType.FinancialFraud, new List<LegalSection> {
            new() { Code = "BNS Sec 316", Title = "Criminal Breach of Trust", Description = "Criminal breach of trust", Punishment = "Up to 7 years imprisonment and fine" },
            new() { Code = "BNS Sec 318", Title = "Cheating", Description = "Cheating and dishonestly inducing delivery of property", Punishment = "Up to 7 years imprisonment and fine" },
            new() { Code = "BNS Sec 320", Title = "Dishonest Misappropriation", Description = "Dishonest misappropriation of property", Punishment = "Up to 2 years imprisonment and/or fine" },
            new() { Code = "BNS Sec 336", Title = "Forgery", Description = "Forgery for purpose of cheating", Punishment = "Up to 7 years imprisonment and fine" }
        }},
        { FIRCrimeType.Theft, new List<LegalSection> {
            new() { Code = "BNS Sec 303", Title = "Theft", Description = "Whoever commits theft", Punishment = "Up to 3 years imprisonment and/or fine" },
            new() { Code = "BNS Sec 305", Title = "Snatching", Description = "Theft by snatching", Punishment = "Up to 3 years imprisonment and fine" },
            new() { Code = "BNS Sec 309", Title = "Robbery", Description = "Robbery", Punishment = "Up to 10 years imprisonment and fine" }
        }},
        { FIRCrimeType.Assault, new List<LegalSection> {
            new() { Code = "BNS Sec 115", Title = "Voluntarily Causing Hurt", Description = "Whoever voluntarily causes hurt", Punishment = "Up to 1 year imprisonment and/or fine up to ₹10,000" },
            new() { Code = "BNS Sec 117", Title = "Grievous Hurt", Description = "Voluntarily causing grievous hurt", Punishment = "Up to 7 years imprisonment and fine" },
            new() { Code = "BNS Sec 131", Title = "Criminal Force", Description = "Using criminal force", Punishment = "Up to 3 months imprisonment and/or fine up to ₹1,000" }
        }},
        { FIRCrimeType.SexualHarassment, new List<LegalSection> {
            new() { Code = "BNS Sec 75", Title = "Sexual Harassment", Description = "Sexual harassment and punishment", Punishment = "Up to 3 years imprisonment and fine" },
            new() { Code = "BNS Sec 78", Title = "Stalking", Description = "Stalking", Punishment = "Up to 3 years imprisonment and fine (first offence)" },
            new() { Code = "BNS Sec 79", Title = "Word, Gesture or Act", Description = "Word, gesture or act intended to insult modesty", Punishment = "Up to 3 years imprisonment and fine" },
            new() { Code = "POSH Act 2013", Title = "Workplace Harassment", Description = "Sexual Harassment of Women at Workplace Act", Punishment = "As per employer's service rules" }
        }},
        { FIRCrimeType.DomesticViolence, new List<LegalSection> {
            new() { Code = "DV Act 2005", Title = "Domestic Violence", Description = "Protection of Women from Domestic Violence Act", Punishment = "Civil remedies + Up to 1 year imprisonment for breach" },
            new() { Code = "BNS Sec 85", Title = "Cruelty by Husband", Description = "Cruelty by husband or his relatives", Punishment = "Up to 3 years imprisonment and fine" },
            new() { Code = "Dowry Act Sec 3", Title = "Dowry Demand", Description = "Penalty for giving or taking dowry", Punishment = "Up to 5 years imprisonment and fine" }
        }},
        { FIRCrimeType.Extortion, new List<LegalSection> {
            new() { Code = "BNS Sec 308", Title = "Extortion", Description = "Putting person in fear to deliver property", Punishment = "Up to 3 years imprisonment and/or fine" },
            new() { Code = "BNS Sec 351", Title = "Criminal Intimidation", Description = "Criminal intimidation", Punishment = "Up to 2 years imprisonment and/or fine" },
            new() { Code = "IT Act Sec 66E", Title = "Privacy Violation", Description = "Punishment for violation of privacy", Punishment = "Up to 3 years imprisonment and/or fine up to ₹2 lakh" }
        }},
        { FIRCrimeType.IdentityTheft, new List<LegalSection> {
            new() { Code = "IT Act Sec 66C", Title = "Identity Theft", Description = "Fraudulently using electronic signature, password", Punishment = "Up to 3 years imprisonment and fine up to ₹1 lakh" },
            new() { Code = "Aadhaar Act Sec 35", Title = "Aadhaar Fraud", Description = "Offence of impersonation of Aadhaar holder", Punishment = "Up to 3 years imprisonment and/or fine up to ₹10,000" },
            new() { Code = "BNS Sec 336", Title = "Forgery", Description = "Making false document", Punishment = "Up to 2 years imprisonment and/or fine" }
        }},
        { FIRCrimeType.Defamation, new List<LegalSection> {
            new() { Code = "BNS Sec 356", Title = "Defamation", Description = "Making or publishing imputation concerning any person", Punishment = "Up to 2 years imprisonment and/or fine" },
            new() { Code = "IT Act Sec 66A", Title = "Offensive Messages", Description = "Sending offensive messages (struck down but reference)", Punishment = "Struck down by Supreme Court" }
        }},
        { FIRCrimeType.Stalking, new List<LegalSection> {
            new() { Code = "BNS Sec 78", Title = "Stalking", Description = "Following or contacting a person repeatedly", Punishment = "Up to 3 years imprisonment and fine (first offence), up to 5 years (subsequent)" },
            new() { Code = "IT Act Sec 67", Title = "Cyber Stalking", Description = "Publishing obscene material electronically", Punishment = "Up to 5 years imprisonment and fine up to ₹10 lakh" }
        }},
        { FIRCrimeType.Other, new List<LegalSection>() }
    };
}

public class LegalSection
{
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Punishment { get; set; } = string.Empty;
}

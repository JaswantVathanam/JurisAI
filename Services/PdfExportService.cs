using AILegalAsst.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AILegalAsst.Services;

/// <summary>
/// Service for generating PDF exports for FIRs, Legal Notices, and Cases
/// Uses QuestPDF library for professional document generation
/// </summary>
public class PdfExportService
{
    private readonly ILogger<PdfExportService> _logger;
    
    // Indian Government Document Colors
    private static readonly string NavyBlue = "#1a365d";
    private static readonly string DarkText = "#1a202c";
    
    public PdfExportService(ILogger<PdfExportService> logger)
    {
        _logger = logger;
        
        // Configure QuestPDF license (Community license is free)
        QuestPDF.Settings.License = LicenseType.Community;
    }
    
    #region FIR Draft Export
    
    /// <summary>
    /// Generate PDF for FIR Draft
    /// </summary>
    public byte[] GenerateFIRPdf(FIRDraft firDraft)
    {
        try
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(11).FontColor(DarkText));
                    
                    page.Header().Element(c => ComposeFIRHeader(c, firDraft));
                    page.Content().Element(c => ComposeFIRContent(c, firDraft));
                    page.Footer().Element(c => ComposeFooter(c, "FIR DRAFT - JurisAI"));
                });
            });
            
            return document.GeneratePdf();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating FIR PDF");
            throw;
        }
    }
    
    private void ComposeFIRHeader(IContainer container, FIRDraft fir)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("FIRST INFORMATION REPORT").Bold().FontSize(16).FontColor(NavyBlue);
                    c.Item().Text("(Under Section 154 Cr.P.C. / Section 173 BNSS)").FontSize(9);
                });
            });
            
            col.Item().PaddingVertical(10).LineHorizontal(1).LineColor(NavyBlue);
            
            col.Item().Row(row =>
            {
                row.RelativeItem().Text($"FIR ID: {fir.Id}").FontSize(10);
                row.RelativeItem().AlignRight().Text($"Date: {fir.CreatedAt:dd/MM/yyyy}").FontSize(10);
            });
        });
    }
    
    private void ComposeFIRContent(IContainer container, FIRDraft fir)
    {
        container.PaddingVertical(10).Column(col =>
        {
            // Complainant Section
            col.Item().Element(c => ComposeSectionHeader(c, "Complainant Details"));
            col.Item().Element(c => ComposeInfoGrid(c, new Dictionary<string, string>
            {
                { "Name", fir.ComplainantName },
                { "S/o, D/o, W/o", fir.FatherOrHusbandName },
                { "Address", $"{fir.Address}, {fir.City}, {fir.State} - {fir.PinCode}" },
                { "Phone", fir.PhoneNumber },
                { "Email", fir.Email }
            }));
            
            // Incident Section
            col.Item().Element(c => ComposeSectionHeader(c, "Incident Details"));
            col.Item().Element(c => ComposeInfoGrid(c, new Dictionary<string, string>
            {
                { "Date", fir.IncidentDate.ToString("dd MMMM yyyy") },
                { "Time", fir.IncidentTime != TimeSpan.Zero ? fir.IncidentTime.ToString(@"hh\:mm") : "Not specified" },
                { "Location", fir.IncidentLocation },
                { "Police Station", fir.PoliceStation },
                { "District", fir.District },
                { "Crime Type", fir.CrimeType.ToString() },
                { "Amount Lost", fir.AmountLost.HasValue ? $"₹{fir.AmountLost:N2}" : "N/A" }
            }));
            
            // Incident Description
            col.Item().Element(c => ComposeSectionHeader(c, "Statement"));
            col.Item().PaddingBottom(10).Border(1).BorderColor("#e2e8f0").Padding(10)
                .Text(fir.IncidentDescription).FontSize(10).LineHeight(1.5f);
            
            // Accused Section
            if (!string.IsNullOrEmpty(fir.AccusedName) || !string.IsNullOrEmpty(fir.AccusedDescription))
            {
                col.Item().Element(c => ComposeSectionHeader(c, "Accused Information"));
                col.Item().Element(c => ComposeInfoGrid(c, new Dictionary<string, string>
                {
                    { "Name", fir.AccusedName },
                    { "Description", fir.AccusedDescription },
                    { "Address", fir.AccusedAddress },
                    { "Phone", fir.AccusedPhone }
                }));
            }
            
            // Legal Sections (if available)
            if (fir.ApplicableSections.Any())
            {
                col.Item().Element(c => ComposeSectionHeader(c, "Applicable Legal Sections"));
                col.Item().PaddingBottom(10).Text(string.Join(", ", fir.ApplicableSections)).FontSize(10);
            }
            
            // AI Generated FIR (if available)
            if (!string.IsNullOrEmpty(fir.AIDraftedFIR))
            {
                col.Item().Element(c => ComposeSectionHeader(c, "AI-Generated FIR Draft"));
                col.Item().Border(1).BorderColor("#e2e8f0").Background("#f8fafc").Padding(15)
                    .Text(fir.AIDraftedFIR).FontSize(10).LineHeight(1.6f);
            }
            
            // Signature Section
            col.Item().PaddingTop(30).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("________________________");
                    c.Item().Text("Complainant's Signature").FontSize(9);
                    c.Item().Text($"Date: {DateTime.Now:dd/MM/yyyy}").FontSize(9);
                });
                row.RelativeItem().AlignRight().Column(c =>
                {
                    c.Item().Text("________________________");
                    c.Item().Text("Officer's Signature & Seal").FontSize(9);
                    c.Item().Text("Police Station Stamp").FontSize(9);
                });
            });
        });
    }
    
    #endregion
    
    #region Legal Notice Export
    
    /// <summary>
    /// Generate PDF for Legal Notice
    /// </summary>
    public byte[] GenerateLegalNoticePdf(LegalNotice notice)
    {
        try
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(11).FontColor(DarkText));
                    
                    page.Header().Element(c => ComposeNoticeHeader(c, notice));
                    page.Content().Element(c => ComposeNoticeContent(c, notice));
                    page.Footer().Element(c => ComposeFooter(c, "Official Legal Notice"));
                });
            });
            
            return document.GeneratePdf();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Legal Notice PDF");
            throw;
        }
    }
    
    private void ComposeNoticeHeader(IContainer container, LegalNotice notice)
    {
        container.Column(col =>
        {
            col.Item().AlignCenter().Text("CYBER CRIME POLICE STATION").Bold().FontSize(16).FontColor(NavyBlue);
            col.Item().AlignCenter().Text("State Police Department").FontSize(11);
            col.Item().PaddingVertical(10).LineHorizontal(2).LineColor(NavyBlue);
            
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text($"Notice No: {notice.NoticeNumber}").FontSize(10);
                    c.Item().Text($"Date: {notice.GeneratedDate:dd/MM/yyyy}").FontSize(10);
                });
                row.RelativeItem().AlignRight().Column(c =>
                {
                    c.Item().Text($"Case No: {notice.CaseNumber}").FontSize(10);
                    c.Item().Text($"FIR No: {notice.FIRNumber}").FontSize(10);
                });
            });
        });
    }
    
    private void ComposeNoticeContent(IContainer container, LegalNotice notice)
    {
        container.PaddingVertical(15).Column(col =>
        {
            // Notice Type Banner
            col.Item().Background("#edf2f7").Padding(10).AlignCenter()
                .Text(GetNoticeTitle(notice.Type)).Bold().FontSize(12).FontColor(NavyBlue);
            
            // Recipient
            col.Item().PaddingTop(15).Column(recipient =>
            {
                recipient.Item().Text("To,").Bold();
                recipient.Item().Text(notice.RecipientName).Bold();
                recipient.Item().Text(notice.RecipientDesignation);
                recipient.Item().Text(notice.RecipientOrganization);
                recipient.Item().Text(notice.RecipientAddress);
            });
            
            // Subject
            col.Item().PaddingTop(15).Text(text =>
            {
                text.Span("Subject: ").Bold();
                text.Span(notice.Subject);
            });
            
            // Reference (if any)
            if (!string.IsNullOrEmpty(notice.Reference))
            {
                col.Item().PaddingTop(5).Text(text =>
                {
                    text.Span("Reference: ").Bold();
                    text.Span(notice.Reference);
                });
            }
            
            // Body
            col.Item().PaddingTop(15).Text(notice.Body).LineHeight(1.6f);
            
            // Requested Data Details
            if (notice.AccountNumbers.Any() || notice.PhoneNumbers.Any() || 
                notice.UPIIds.Any() || notice.SocialMediaHandles.Any())
            {
                col.Item().PaddingTop(15).Element(c => ComposeSectionHeader(c, "Requested Information"));
                
                if (notice.AccountNumbers.Any())
                    col.Item().Text($"Account Numbers: {string.Join(", ", notice.AccountNumbers)}").FontSize(10);
                if (notice.PhoneNumbers.Any())
                    col.Item().Text($"Phone Numbers: {string.Join(", ", notice.PhoneNumbers)}").FontSize(10);
                if (notice.UPIIds.Any())
                    col.Item().Text($"UPI IDs: {string.Join(", ", notice.UPIIds)}").FontSize(10);
                if (notice.SocialMediaHandles.Any())
                    col.Item().Text($"Social Media: {string.Join(", ", notice.SocialMediaHandles)}").FontSize(10);
                
                if (notice.FromDate.HasValue && notice.ToDate.HasValue)
                    col.Item().Text($"Date Range: {notice.FromDate:dd/MM/yyyy} to {notice.ToDate:dd/MM/yyyy}").FontSize(10);
            }
            
            // Urgency Note
            col.Item().PaddingTop(20).Background("#fff3cd").Padding(10)
                .Text("⚠️ This is an official communication under the provisions of law. Non-compliance may lead to legal consequences.")
                .FontSize(10).FontColor("#856404");
            
            // Signature
            col.Item().PaddingTop(30).Column(sig =>
            {
                sig.Item().Text("Yours faithfully,").LineHeight(1.8f);
                sig.Item().PaddingTop(40).Text(notice.SenderName).Bold();
                sig.Item().Text(notice.SenderDesignation);
                sig.Item().Text(notice.SenderStation);
                sig.Item().Text($"Contact: {notice.SenderContact}");
            });
        });
    }
    
    private string GetNoticeTitle(LegalNoticeType type)
    {
        return type switch
        {
            LegalNoticeType.BankAccountFreeze => "NOTICE FOR BANK ACCOUNT FREEZE",
            LegalNoticeType.CDRRequest => "NOTICE FOR CALL DETAIL RECORDS",
            LegalNoticeType.SocialMediaTakedown => "NOTICE FOR CONTENT REMOVAL/DATA PRESERVATION",
            LegalNoticeType.UPIWalletFreeze => "NOTICE FOR UPI/WALLET FREEZE",
            LegalNoticeType.WitnessSummons => "WITNESS SUMMONS",
            LegalNoticeType.CourtFilingCover => "COURT FILING COVER LETTER",
            LegalNoticeType.VictimStatusUpdate => "VICTIM STATUS UPDATE",
            LegalNoticeType.IPAddressRequest => "NOTICE FOR IP ADDRESS LOGS",
            LegalNoticeType.MerchantDetailsRequest => "NOTICE FOR MERCHANT DETAILS",
            _ => "OFFICIAL NOTICE"
        };
    }
    
    #endregion
    
    #region Case Export
    
    /// <summary>
    /// Generate PDF for Case Details
    /// </summary>
    public byte[] GenerateCasePdf(Case caseDetails, List<CaseTimelineEvent>? timeline = null)
    {
        try
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(11).FontColor(DarkText));
                    
                    page.Header().Element(c => ComposeCaseHeader(c, caseDetails));
                    page.Content().Element(c => ComposeCaseContent(c, caseDetails, timeline));
                    page.Footer().Element(c => ComposeFooter(c, "Case Report - JurisAI"));
                });
            });
            
            return document.GeneratePdf();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Case PDF");
            throw;
        }
    }
    
    private void ComposeCaseHeader(IContainer container, Case caseDetails)
    {
        container.Column(col =>
        {
            col.Item().Text("CASE REPORT").Bold().FontSize(18).FontColor(NavyBlue);
            col.Item().PaddingVertical(8).LineHorizontal(2).LineColor(NavyBlue);
            
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text($"Case No: {caseDetails.CaseNumber}").FontSize(11).Bold();
                    c.Item().Text($"Filed: {caseDetails.FiledDate:dd MMMM yyyy}").FontSize(10);
                });
                row.RelativeItem().AlignRight().Column(c =>
                {
                    c.Item().Text($"Status: {caseDetails.Status}").FontSize(11).Bold();
                    c.Item().Text($"Type: {caseDetails.Type}").FontSize(10);
                });
            });
        });
    }
    
    private void ComposeCaseContent(IContainer container, Case caseDetails, List<CaseTimelineEvent>? timeline)
    {
        container.PaddingVertical(15).Column(col =>
        {
            // Title
            col.Item().Text(caseDetails.Title).Bold().FontSize(14);
            
            if (caseDetails.IsCybercrime)
            {
                col.Item().PaddingTop(5).Background("#fee2e2").Padding(5).Text("🛡️ CYBERCRIME CASE").FontSize(10).FontColor("#dc2626");
            }
            
            // Case Information
            col.Item().Element(c => ComposeSectionHeader(c, "Case Information"));
            col.Item().Element(c => ComposeInfoGrid(c, new Dictionary<string, string>
            {
                { "Case Number", caseDetails.CaseNumber },
                { "FIR Number", caseDetails.FIRNumber ?? "Pending" },
                { "Type", caseDetails.Type.ToString() },
                { "Status", caseDetails.Status.ToString() },
                { "Court", caseDetails.Court ?? "Not Assigned" },
                { "Filed Date", caseDetails.FiledDate.ToString("dd MMMM yyyy") }
            }));
            
            // Parties
            col.Item().Element(c => ComposeSectionHeader(c, "Parties Involved"));
            col.Item().Element(c => ComposeInfoGrid(c, new Dictionary<string, string>
            {
                { "Complainant", caseDetails.Complainant },
                { "Accused", caseDetails.Accused },
                { "Lawyer", caseDetails.AssignedLawyer ?? "Not Assigned" }
            }));
            
            // Description
            col.Item().Element(c => ComposeSectionHeader(c, "Case Description"));
            col.Item().PaddingBottom(10).Border(1).BorderColor("#e2e8f0").Padding(10)
                .Text(caseDetails.Description).FontSize(10).LineHeight(1.5f);
            
            // Timeline Events (if provided)
            if (timeline != null && timeline.Any())
            {
                col.Item().Element(c => ComposeSectionHeader(c, "Case Timeline"));
                
                foreach (var evt in timeline.OrderByDescending(e => e.EventDate).Take(10))
                {
                    col.Item().PaddingVertical(5).Row(row =>
                    {
                        row.ConstantItem(80).Text(evt.EventDate.ToString("dd/MM/yyyy")).FontSize(9).FontColor("#718096");
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text(evt.Title).Bold().FontSize(10);
                            c.Item().Text(evt.Description).FontSize(9).FontColor("#4a5568");
                        });
                    });
                }
            }
            
            // Report Footer
            col.Item().PaddingTop(30).Text($"Report Generated: {DateTime.Now:dd MMMM yyyy, hh:mm tt}").FontSize(9).FontColor("#718096");
        });
    }
    
    #endregion
    
    #region Shared Components
    
    private void ComposeSectionHeader(IContainer container, string title)
    {
        container.PaddingVertical(10).Column(col =>
        {
            col.Item().Text(title).Bold().FontSize(12).FontColor(NavyBlue);
            col.Item().LineHorizontal(1).LineColor("#cbd5e0");
        });
    }
    
    private void ComposeInfoGrid(IContainer container, Dictionary<string, string> items)
    {
        container.PaddingBottom(10).Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.ConstantColumn(120);
                cols.RelativeColumn();
            });
            
            foreach (var item in items.Where(i => !string.IsNullOrEmpty(i.Value)))
            {
                table.Cell().PaddingVertical(3).Text($"{item.Key}:").Bold().FontSize(10);
                table.Cell().PaddingVertical(3).Text(item.Value).FontSize(10);
            }
        });
    }
    
    private void ComposeFooter(IContainer container, string documentType)
    {
        container.Column(col =>
        {
            col.Item().LineHorizontal(1).LineColor("#e2e8f0");
            col.Item().PaddingTop(5).Row(row =>
            {
                row.RelativeItem().Text(documentType).FontSize(8).FontColor("#718096");
                row.RelativeItem().AlignCenter().Text("JurisAI").FontSize(8).FontColor("#718096");
                row.RelativeItem().AlignRight().Text($"Generated: {DateTime.Now:dd/MM/yyyy}").FontSize(8).FontColor("#718096");
            });
        });
    }
    
    #endregion
}

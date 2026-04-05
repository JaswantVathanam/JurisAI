using Microsoft.AspNetCore.Mvc;
using AILegalAsst.Models;
using AILegalAsst.Services;

namespace AILegalAsst.Controllers;

/// <summary>
/// API Controller for generating and downloading PDF documents
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PdfExportController : ControllerBase
{
    private readonly PdfExportService _pdfService;
    private readonly CaseService _caseService;
    private readonly LegalNoticeService _noticeService;
    private readonly CaseTimelineService _timelineService;
    private readonly ILogger<PdfExportController> _logger;

    public PdfExportController(
        PdfExportService pdfService,
        CaseService caseService,
        LegalNoticeService noticeService,
        CaseTimelineService timelineService,
        ILogger<PdfExportController> logger)
    {
        _pdfService = pdfService;
        _caseService = caseService;
        _noticeService = noticeService;
        _timelineService = timelineService;
        _logger = logger;
    }

    /// <summary>
    /// Generate PDF for FIR Draft
    /// </summary>
    [HttpPost("fir")]
    public IActionResult ExportFIR([FromBody] FIRDraft firDraft)
    {
        try
        {
            if (firDraft == null)
                return BadRequest("FIR draft data is required");

            var pdfBytes = _pdfService.GenerateFIRPdf(firDraft);
            var filename = $"FIR_Draft_{firDraft.Id}_{DateTime.Now:yyyyMMdd}.pdf";
            
            return File(pdfBytes, "application/pdf", filename);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating FIR PDF");
            return StatusCode(500, "Error generating PDF");
        }
    }

    /// <summary>
    /// Generate PDF for Legal Notice
    /// </summary>
    [HttpPost("notice")]
    public IActionResult ExportNotice([FromBody] LegalNotice notice)
    {
        try
        {
            if (notice == null)
                return BadRequest("Legal notice data is required");

            var pdfBytes = _pdfService.GenerateLegalNoticePdf(notice);
            var filename = $"Notice_{notice.NoticeNumber}_{DateTime.Now:yyyyMMdd}.pdf";
            
            return File(pdfBytes, "application/pdf", filename);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Legal Notice PDF");
            return StatusCode(500, "Error generating PDF");
        }
    }

    /// <summary>
    /// Generate PDF for Case Details (by Case ID)
    /// </summary>
    [HttpGet("case/{caseId:int}")]
    public async Task<IActionResult> ExportCase(int caseId)
    {
        try
        {
            var caseDetails = await _caseService.GetCaseByIdAsync(caseId);
            if (caseDetails == null)
                return NotFound($"Case with ID {caseId} not found");

            // Get timeline events for the case
            var timeline = _timelineService.GetTimelineForCase(caseId);
            
            var pdfBytes = _pdfService.GenerateCasePdf(caseDetails, timeline);
            var filename = $"Case_{caseDetails.CaseNumber}_{DateTime.Now:yyyyMMdd}.pdf";
            
            return File(pdfBytes, "application/pdf", filename);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Case PDF for Case ID: {CaseId}", caseId);
            return StatusCode(500, "Error generating PDF");
        }
    }

    /// <summary>
    /// Generate PDF for saved Legal Notice (by Notice ID)
    /// </summary>
    [HttpGet("notice/{noticeId:int}")]
    public async Task<IActionResult> ExportSavedNotice(int noticeId)
    {
        try
        {
            var notices = await _noticeService.GetAllNoticesAsync();
            var notice = notices.FirstOrDefault(n => n.Id == noticeId);
            
            if (notice == null)
                return NotFound($"Notice with ID {noticeId} not found");

            var pdfBytes = _pdfService.GenerateLegalNoticePdf(notice);
            var filename = $"Notice_{notice.NoticeNumber}_{DateTime.Now:yyyyMMdd}.pdf";
            
            return File(pdfBytes, "application/pdf", filename);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Notice PDF for Notice ID: {NoticeId}", noticeId);
            return StatusCode(500, "Error generating PDF");
        }
    }
}

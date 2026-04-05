namespace AILegalAsst.Models;

public class VerificationLog
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int AdminId { get; set; }
    public string AdminName { get; set; } = string.Empty;
    public VerificationStatus OldStatus { get; set; }
    public VerificationStatus NewStatus { get; set; }
    public string? AdminNotes { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime ActionDate { get; set; }
    public string ActionType { get; set; } = string.Empty; // "Approved", "Rejected", "RequestedInfo"
}

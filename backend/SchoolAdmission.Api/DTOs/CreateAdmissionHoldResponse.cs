namespace SchoolAdmission.Api.DTOs;

public class AdmissionHoldResponse
{
    public Guid HoldId => Id;
    public Guid Id { get; set; }
    public Guid ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string StudentId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public DateTime? PaidAt { get; set; }
}
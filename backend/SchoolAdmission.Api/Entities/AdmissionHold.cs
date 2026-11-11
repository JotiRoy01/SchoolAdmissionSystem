
namespace SchoolAdmission.Api.Entities;

public class AdmissionHold
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ClassId { get; set; }

    public Guid StudentId { get; set; }

    public AdmissionHoldStatus Status { get; set; }
        = AdmissionHoldStatus.HELD;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAt { get; set; }

    public DateTime? ConfirmedAt { get; set; }

    public AdmissionPaymentStatus PaymentStatus { get; set; }
        = AdmissionPaymentStatus.PENDING;

    public DateTime? PaidAt { get; set; }

    // Navigation properties
    public AdmissionClass Class { get; set; } = null!;

    public Student Student { get; set; } = null!;
}

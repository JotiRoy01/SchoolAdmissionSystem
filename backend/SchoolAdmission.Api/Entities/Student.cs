
namespace SchoolAdmission.Api.Entities;

public class Student
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Bangladesh 17-digit birth registration number
    public string BirthRegistrationNumber { get; set; }
        = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property: one student can have multiple holds
    public ICollection<AdmissionHold> Holds { get; set; }
        = new List<AdmissionHold>();
}

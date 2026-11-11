
namespace SchoolAdmission.Api.Entities;

public class AdmissionClass
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Example: Class 6 - Morning
    public string Name { get; set; } = string.Empty;

    // Maximum number of students who can be admitted
    public int TotalSeats { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property: one class can have many admission holds
    public ICollection<AdmissionHold> Holds { get; set; }
        = new List<AdmissionHold>();
}

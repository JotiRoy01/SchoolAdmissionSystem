using System.ComponentModel.DataAnnotations;

namespace SchoolAdmission.Api.DTOs;

public class CreateAdmissionClassRequest
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Range(1, 10000)]
    public int TotalSeats { get; set; }
}
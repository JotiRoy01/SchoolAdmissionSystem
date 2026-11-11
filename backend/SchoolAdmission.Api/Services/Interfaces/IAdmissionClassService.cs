using SchoolAdmission.Api.DTOs;

namespace SchoolAdmission.Api.Services.Interfaces;

public interface IAdmissionClassService
{
    Task<ServiceResult<AdmissionClassResponse>> CreateClassAsync(
        CreateAdmissionClassRequest request);

    Task<ServiceResult<List<AdmissionClassResponse>>> GetAllClassesAsync();

    Task<ServiceResult<AdmissionClassResponse>> GetClassByIdAsync(
        Guid classId);

    Task<ServiceResult<bool>> DeleteClassAsync(Guid classId);
}
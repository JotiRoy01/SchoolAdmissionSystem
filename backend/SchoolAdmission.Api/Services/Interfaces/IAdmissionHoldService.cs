using SchoolAdmission.Api.DTOs;
using SchoolAdmission.Api.Services;

namespace SchoolAdmission.Api.Services.Interfaces;

public interface IAdmissionHoldService
{
    Task<ServiceResult<AdmissionHoldResponse>> CreateHoldAsync(
        Guid classId,
        string? studentId);

    Task<ServiceResult<AdmissionHoldResponse>> GetHoldByIdAsync(
        Guid holdId);

    Task<ServiceResult<AdmissionHoldResponse>> ConfirmHoldAsync(
        Guid holdId,
        string? studentId);

    // Task<ServiceResult<AdmissionHoldResponse>> PayAdmissionFeeAsync(
    //     Guid holdId,
    //     string? studentId);

    Task<ServiceResult<AdmissionHoldResponse>> CancelHoldAsync(
        Guid holdId,
        string? studentId);

    Task<ServiceResult<List<AdmissionHoldResponse>>> GetMyHoldsAsync(
        string? studentId);
    
    Task<int> ExpireOverdueHoldsAsync(
    CancellationToken cancellationToken = default);
}
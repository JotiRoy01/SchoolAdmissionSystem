using Microsoft.AspNetCore.Mvc;
using SchoolAdmission.Api.DTOs;
using SchoolAdmission.Api.Services;
using SchoolAdmission.Api.Services.Interfaces;

namespace SchoolAdmission.Api.Controllers;

[ApiController]
[Route("")]
public class AdmissionHoldController : ControllerBase
{
    private readonly IAdmissionHoldService _holdService;

    public AdmissionHoldController(
        IAdmissionHoldService holdService)
    {
        _holdService = holdService;
    }

    private IActionResult HandleResult<T>(ServiceResult<T> result)
    {
        if (result.IsSuccess)
        {
            return StatusCode(result.StatusCode, result.Data);
        }

        return StatusCode(
            result.StatusCode,
            new ApiErrorResponse
            {
                Message = result.Error ?? "An unexpected error occurred."
            });
    }

    // POST: /api/classes/{classId}/holds
    [HttpPost("classes/{classId:guid}/holds")]
    public async Task<IActionResult> CreateHold(
        Guid classId,
        [FromHeader(Name = "X-Student-Id")] string? studentId)
    {
        var result = await _holdService.CreateHoldAsync(
            classId,
            studentId);

        return HandleResult(result);
    }

    // GET: /api/holds/{holdId}
    [HttpGet("holds/{holdId:guid}")]
    public async Task<IActionResult> GetHoldById(Guid holdId)
    {
        var result = await _holdService.GetHoldByIdAsync(holdId);

        return HandleResult(result);
    }

    // POST: /api/holds/{holdId}/confirm
    [HttpPost("holds/{holdId:guid}/confirm")]
    public async Task<IActionResult> ConfirmHold(
        Guid holdId,
        [FromHeader(Name = "X-Student-Id")] string? studentId)
    {
        var result = await _holdService.ConfirmHoldAsync(
            holdId,
            studentId);

        return HandleResult(result);
    }

    // [HttpPost("holds/{holdId:guid}/payment")]
    // public async Task<IActionResult> PayAdmissionFee(
    //     Guid holdId,
    //     [FromHeader(Name = "X-Student-Id")] string? studentId)
    // {
    //     var result = await _holdService.PayAdmissionFeeAsync(
    //         holdId,
    //         studentId);

    //     return HandleResult(result);
    // }

    // DELETE: /api/holds/{holdId}
    [HttpDelete("holds/{holdId:guid}")]
    public async Task<IActionResult> CancelHold(
        Guid holdId,
        [FromHeader(Name = "X-Student-Id")] string? studentId)
    {
        var result = await _holdService.CancelHoldAsync(
            holdId,
            studentId);

        return result.IsSuccess
            ? NoContent()
            : HandleResult(result);
    }

    // GET: /api/me/holds
    [HttpGet("me/holds")]
    public async Task<IActionResult> GetMyHolds(
        [FromHeader(Name = "X-Student-Id")] string? studentId)
    {
        var result = await _holdService.GetMyHoldsAsync(studentId);

        return HandleResult(result);
    }
}
using Microsoft.AspNetCore.Mvc;
using SchoolAdmission.Api.DTOs;
using SchoolAdmission.Api.Services;
using SchoolAdmission.Api.Services.Interfaces;

namespace SchoolAdmission.Api.Controllers;

[ApiController]
[Route("classes")]
public class AdmissionClassController : ControllerBase
{
    private readonly IAdmissionClassService _classService;

    public AdmissionClassController(
        IAdmissionClassService classService)
    {
        _classService = classService;
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

    // POST: /api/classes
    [HttpPost]
    public async Task<IActionResult> CreateClass(
        [FromBody] CreateAdmissionClassRequest request)
    {
        var result = await _classService.CreateClassAsync(request);

        return HandleResult(result);
    }

    // GET: /api/classes
    [HttpGet]
    public async Task<IActionResult> GetAllClasses()
    {
        var result = await _classService.GetAllClassesAsync();

        return HandleResult(result);
    }

    // GET: /api/classes/{classId}
    [HttpGet("{classId:guid}")]
    public async Task<IActionResult> GetClassById(Guid classId)
    {
        var result = await _classService.GetClassByIdAsync(classId);

        return HandleResult(result);
    }

    [HttpDelete("{classId:guid}")]
    public async Task<IActionResult> DeleteClass(Guid classId)
    {
        var result = await _classService.DeleteClassAsync(classId);

        return result.IsSuccess
            ? NoContent()
            : HandleResult(result);
    }
}
namespace SchoolAdmission.Api.DTOs;

public class ApiErrorResponse
{
    public string Message { get; set; } = string.Empty;

    public List<string> Errors { get; set; } = new();
}
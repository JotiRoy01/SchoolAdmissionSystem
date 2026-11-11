namespace SchoolAdmission.Api.Services.Validation;

public static class StudentIdValidator
{
    public static bool IsValid(string? studentId)
    {
        return studentId is { Length: 17 } &&
               studentId.All(character =>
                   character >= '0' && character <= '9');
    }
}
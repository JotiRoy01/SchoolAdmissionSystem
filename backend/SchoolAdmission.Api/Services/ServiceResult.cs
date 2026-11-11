namespace SchoolAdmission.Api.Services;

public class ServiceResult<T>
{
    public bool IsSuccess { get; private set; }
    public int StatusCode { get; private set; }
    public T? Data { get; private set; }
    public string? Error { get; private set; }

    private ServiceResult() { }

    public static ServiceResult<T> Success(
        T data,
        int statusCode = 200)
    {
        return new ServiceResult<T>
        {
            IsSuccess = true,
            StatusCode = statusCode,
            Data = data
        };
    }

    public static ServiceResult<T> Failure(
        string error,
        int statusCode)
    {
        return new ServiceResult<T>
        {
            IsSuccess = false,
            StatusCode = statusCode,
            Error = error
        };
    }
}
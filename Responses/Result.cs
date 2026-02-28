using PaymentService.Enums;

namespace PaymentService.Responses;

public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Data { get; }
    public string? Error { get; }
    public ErrorType? ErrorType { get; }

    private Result(bool isSuccess, T? data, string? error, ErrorType? errorType)
    {
        IsSuccess = isSuccess;
        Data = data;
        Error = error;
        ErrorType = errorType;
    }

    public static Result<T> Ok(T data) => new(true, data, null, null);

    public static Result<T> Fail(string error, ErrorType type) => new(false, default, error, type);
}

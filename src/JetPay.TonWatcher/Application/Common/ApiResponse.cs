namespace JetPay.TonWatcher.Application.Common;

public record ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? ErrorMessage { get; init; }

    public static ApiResponse<T> SuccessResult(T data)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data
        };
    }

    public static ApiResponse<T> FailureResult(string errorMessage)
    {
        return new ApiResponse<T>
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }
}

public record ApiResponse
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }

    public static ApiResponse SuccessResult()
    {
        return new ApiResponse { Success = true };
    }

    public static ApiResponse FailureResult(string errorMessage)
    {
        return new ApiResponse
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }
}
namespace KiranaHub.CleanArchitecture.Application.Common;

// =========================================================================================
// [TOPIC]: Railway-Oriented Programming (Result<T> Pattern)
// [PURPOSE]: Explicit outcome representation without throwing exceptions for normal control flow.
// =========================================================================================

// [KYA KARTA HAI]: Generic Result wrapper jo success value ya error message hold karta hai.
// [KYU ZAROORI HAI]: Avoids expensive exception throwing for predictable domain failures.
public class Result<T>
{
    // [KYA KARTA HAI]: Operation success boolean flag.
    public bool IsSuccess { get; }

    // [KYA KARTA HAI]: Operation failure boolean flag.
    public bool IsFailure => !IsSuccess;

    // [KYA KARTA HAI]: Success payload data.
    public T? Value { get; }

    // [KYA KARTA HAI]: Failure error message string.
    public string? Error { get; }

    // [KYA KARTA HAI]: Private constructor enforcing factory creation.
    private Result(bool isSuccess, T? value, string? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    // [KYA KARTA HAI]: Successful result factory.
    public static Result<T> Success(T value) => new(true, value, null);

    // [KYA KARTA HAI]: Failure result factory.
    public static Result<T> Failure(string error) => new(false, default, error);
}


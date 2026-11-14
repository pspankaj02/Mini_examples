namespace KiranaHub.BasicsAndOops.Generics;

// =========================================================================================
// [GENERIC CLASS DECLARATION]:
// [KYA KARTA HAI]: Type-safe generic response envelope `ApiResponse<T>` jahan `T` actual data payload hai.
// [GENERIC CONSTRAINT]: `where T : class` ensure karta hai ki `T` reference type ho (value types not allowed).
// [KYU ZAROORI HAI]: Reusability - Product, Order, Customer sabhi ke liye alag alag response class likhne se bachata hai.
// =========================================================================================
public class ApiResponse<T> where T : class
{
    // [PROPERTY]: Operation success status
    public bool IsSuccess { get; init; }

    // [PROPERTY]: Human-readable message
    public string Message { get; init; } = string.Empty;

    // [GENERIC DATA PROPERTY]: Nullable type-safe payload
    public T? Data { get; init; }

    // [PROPERTY]: UTC Timestamp of response generation
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    // [STATIC FACTORY METHOD - SUCCESS]:
    // [KYA KARTA HAI]: Success response create karne ka standardized factory method.
    public static ApiResponse<T> Success(T data, string message = "Operation completed successfully.")
    {
        // [OBJECT INITIALIZER RETURN]:
        return new ApiResponse<T>
        {
            IsSuccess = true,
            Message = message,
            Data = data
        };
    }

    // [STATIC FACTORY METHOD - FAILURE]:
    // [KYA KARTA HAI]: Failure response create karne ka standardized factory method with null Data.
    public static ApiResponse<T> Failure(string errorMessage)
    {
        // [OBJECT INITIALIZER RETURN]:
        return new ApiResponse<T>
        {
            IsSuccess = false,
            Message = errorMessage,
            Data = null
        };
    }
}

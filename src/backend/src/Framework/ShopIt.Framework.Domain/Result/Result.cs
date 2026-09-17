namespace ShopIt.Framework.Domain.Result;

public readonly struct Result<TValue, TError>
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public TValue Value { get; }
    public TError Error { get; }

    private Result(TValue value)
    {
        IsSuccess = true;
        Value = value;
        Error = default!;
    }

    private Result(TError error)
    {
        IsSuccess = false;
        Value = default!;
        Error = error;
    }

    // Factory methods
    public static Result<TValue, TError> Success(TValue value) => new(value);
    public static Result<TValue, TError> Failure(TError error) => new(error);

    // Implicit conversions for clean syntax
    public static implicit operator Result<TValue, TError>(TValue value) => Success(value);
    public static implicit operator Result<TValue, TError>(TError error) => Failure(error);
}

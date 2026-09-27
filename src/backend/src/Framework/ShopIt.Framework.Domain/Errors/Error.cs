namespace ShopIt.Framework.Domain.Errors;

/// <summary>
/// Represents a domain error with a unique code, descriptive message, category, and optional metadata.
/// </summary>
public record Error
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);
    public static readonly Error NullValue = new("General.NullValue", "The specified result value is null.", ErrorType.Failure);

    public string Code { get; }
    public string Description { get; }
    public ErrorType Type { get; }
    public IReadOnlyDictionary<string, object?>? Metadata { get; }

    public Error(
        string code,
        string description,
        ErrorType type = ErrorType.Failure,
        IReadOnlyDictionary<string, object?>? metadata = null)
    {
        Code = code;
        Description = description;
        Type = type;
        Metadata = metadata;
    }

    /// <summary>
    /// Creates a generic failure domain error.
    /// </summary>
    public static Error Failure(
        string code = "General.Failure",
        string description = "A failure occurred.",
        IReadOnlyDictionary<string, object?>? metadata = null) =>
        new(code, description, ErrorType.Failure, metadata);

    /// <summary>
    /// Creates an unexpected domain error.
    /// </summary>
    public static Error Unexpected(
        string code = "General.Unexpected",
        string description = "An unexpected error occurred.",
        IReadOnlyDictionary<string, object?>? metadata = null) =>
        new(code, description, ErrorType.Unexpected, metadata);

    /// <summary>
    /// Creates a validation domain error.
    /// </summary>
    public static Error Validation(
        string code = "General.Validation",
        string description = "A validation error occurred.",
        IReadOnlyDictionary<string, object?>? metadata = null) =>
        new(code, description, ErrorType.Validation, metadata);

    /// <summary>
    /// Creates a conflict domain error (e.g. duplicate resource or concurrency issue).
    /// </summary>
    public static Error Conflict(
        string code = "General.Conflict",
        string description = "A conflict occurred.",
        IReadOnlyDictionary<string, object?>? metadata = null) =>
        new(code, description, ErrorType.Conflict, metadata);

    /// <summary>
    /// Creates a not found domain error.
    /// </summary>
    public static Error NotFound(
        string code = "General.NotFound",
        string description = "A 'Not Found' error occurred.",
        IReadOnlyDictionary<string, object?>? metadata = null) =>
        new(code, description, ErrorType.NotFound, metadata);

    /// <summary>
    /// Creates an unauthorized domain error.
    /// </summary>
    public static Error Unauthorized(
        string code = "General.Unauthorized",
        string description = "An unauthorized error occurred.",
        IReadOnlyDictionary<string, object?>? metadata = null) =>
        new(code, description, ErrorType.Unauthorized, metadata);

    /// <summary>
    /// Creates a forbidden domain error.
    /// </summary>
    public static Error Forbidden(
        string code = "General.Forbidden",
        string description = "A forbidden error occurred.",
        IReadOnlyDictionary<string, object?>? metadata = null) =>
        new(code, description, ErrorType.Forbidden, metadata);

    /// <summary>
    /// Creates a custom domain error with explicit type.
    /// </summary>
    public static Error Custom(
        string code,
        string description,
        ErrorType type,
        IReadOnlyDictionary<string, object?>? metadata = null) =>
        new(code, description, type, metadata);
}

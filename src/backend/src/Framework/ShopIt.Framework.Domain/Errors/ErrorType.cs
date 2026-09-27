namespace ShopIt.Framework.Domain.Errors;

/// <summary>
/// Specifies the category or classification of a domain error.
/// </summary>
public enum ErrorType
{
    /// <summary>
    /// A general failure that cannot be categorized.
    /// </summary>
    Failure = 0,

    /// <summary>
    /// An unexpected or exceptional failure.
    /// </summary>
    Unexpected = 1,

    /// <summary>
    /// Validation error when inputs or invariants violate domain rules.
    /// </summary>
    Validation = 2,

    /// <summary>
    /// Conflict error, e.g., concurrency violation or duplicate unique constraint.
    /// </summary>
    Conflict = 3,

    /// <summary>
    /// Resource or entity not found.
    /// </summary>
    NotFound = 4,

    /// <summary>
    /// Operation rejected due to missing or invalid authentication.
    /// </summary>
    Unauthorized = 5,

    /// <summary>
    /// Operation rejected due to insufficient permissions.
    /// </summary>
    Forbidden = 6
}

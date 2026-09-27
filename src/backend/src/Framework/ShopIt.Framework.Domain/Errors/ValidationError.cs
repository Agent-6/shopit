namespace ShopIt.Framework.Domain.Errors;

/// <summary>
/// Represents a domain error composed of one or more individual validation failures.
/// </summary>
public sealed record ValidationError : Error
{
    public IReadOnlyList<Error> Errors { get; }

    public ValidationError(IReadOnlyList<Error> errors)
        : base("General.Validation", "One or more validation errors occurred.", ErrorType.Validation)
    {
        Errors = errors ?? [];
    }

    public ValidationError(string code, string description, IReadOnlyList<Error> errors)
        : base(code, description, ErrorType.Validation)
    {
        Errors = errors ?? [];
    }

    /// <summary>
    /// Creates a ValidationError from an array of individual errors.
    /// </summary>
    public static ValidationError From(params Error[] errors) => new(errors);

    /// <summary>
    /// Creates a ValidationError from a sequence of individual errors.
    /// </summary>
    public static ValidationError From(IEnumerable<Error> errors) => new(errors.ToList());
}

using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Permissions;

namespace ShopIt.Identity.Domain.Users;

/// <summary>
/// The expected failures of user operations, as values (#32).
/// </summary>
/// <remarks>
/// A missing user, an unauthenticated principal, a role that cannot be assigned on this side, and the
/// validation failures ASP.NET Identity reports through <c>IdentityResult</c> are outcomes a caller can
/// act on. They belong in a <c>Result</c> whose category the presentation layer turns into a status code.
/// </remarks>
public static class UserErrors
{
    /// <summary>The user does not exist.</summary>
    public static Error NotFound(Guid userId) =>
        new("User.NotFound", $"A user with the id '{userId}' was not found.", ErrorType.NotFound);

    /// <summary>The request has no authenticated user (e.g. a client-credentials token).</summary>
    public static Error Unauthenticated() =>
        new("User.Unauthenticated", "The request has no authenticated user.", ErrorType.Unauthorized);

    /// <summary>A role cannot be assigned to a user on the other side of the tenancy boundary.</summary>
    public static Error RoleNotAvailableOnSide(
        string roleName,
        PermissionMultiTenancySide roleSide,
        PermissionMultiTenancySide userSide) =>
        new(
            "User.RoleNotAvailableOnSide",
            $"Role '{roleName}' is only available on the {roleSide} side and cannot be assigned to a " +
            $"user in the {userSide} side.",
            ErrorType.Validation);

    /// <summary>Validation failures reported by ASP.NET Identity, keeping every message.</summary>
    /// <param name="code">Prefix for the individual error codes, e.g. <c>User.Create</c>.</param>
    /// <param name="failures">The descriptions Identity reported.</param>
    public static Error Invalid(string code, IEnumerable<string> failures) =>
        ValidationError.From(failures.Select((description, index) =>
            new Error($"{code}.{index}", description, ErrorType.Validation)));
}

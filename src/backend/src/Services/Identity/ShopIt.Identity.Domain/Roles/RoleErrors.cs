using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Permissions;

namespace ShopIt.Identity.Domain.Roles;

/// <summary>
/// The expected failures of role operations, as values (#32).
/// </summary>
/// <remarks>
/// Not-found, a role that cannot be deleted because it is still assigned, a side mismatch, and the
/// validation failures ASP.NET Identity reports through <c>IdentityResult</c> are all outcomes a caller
/// can act on. They belong in a <c>Result</c> with a category the presentation layer turns into a status
/// code, not in an exception that reaches the client as a 500.
/// </remarks>
public static class RoleErrors
{
    /// <summary>The role does not exist.</summary>
    public static Error NotFound(Guid roleId) =>
        new("Role.NotFound", $"A role with the id '{roleId}' was not found.", ErrorType.NotFound);

    /// <summary>The caller's tenancy side cannot own a role of the requested side.</summary>
    public static Error NotAvailableOnSide(string name, PermissionMultiTenancySide currentSide) =>
        new(
            "Role.NotAvailableOnSide",
            $"Role '{name}' is not available on the {currentSide} side; choose " +
            $"'{PermissionMultiTenancySide.Both}' or '{currentSide}'.",
            ErrorType.Validation);

    /// <summary>The role is still assigned to users and cannot be deleted.</summary>
    public static Error InUse(string? name, long assignedUsers) =>
        new(
            "Role.InUse",
            $"Role '{name}' is still assigned to {assignedUsers} user(s) and cannot be deleted. " +
            "Unassign the role first.",
            ErrorType.Conflict);

    /// <summary>Validation failures reported by ASP.NET Identity, e.g. a duplicate name.</summary>
    /// <param name="code">Prefix for the individual error codes, e.g. <c>Role.Create</c>.</param>
    /// <param name="failures">The descriptions Identity reported.</param>
    public static Error Invalid(string code, IEnumerable<string> failures) =>
        ValidationError.From(failures.Select((description, index) =>
            new Error($"{code}.{index}", description, ErrorType.Validation)));

    /// <summary>A single validation failure with a message this service owns.</summary>
    public static Error Invalid(string code, string description) =>
        new(code, description, ErrorType.Validation);
}

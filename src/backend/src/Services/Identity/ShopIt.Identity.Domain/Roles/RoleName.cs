namespace ShopIt.Identity.Domain.Roles;

/// <summary>
/// Value object representing a role name/key (e.g. <c>Admin</c>). Used in place of raw
/// strings so role identities are typed, compared by value, and self-documenting.
/// Implicitly converts to <see cref="string"/> at the Identity/UserManager boundary.
/// </summary>
public record RoleName
{
    public string Value { get; }

    public RoleName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Role name cannot be empty or whitespace.", nameof(value));

        Value = value;
    }

    public static implicit operator string(RoleName name) => name.Value;

    public override string ToString() => Value;
}

namespace ShopIt.Framework.Domain.Permissions;

/// <summary>
/// Value object representing a permission key (e.g. <c>user.create</c>). Used in place of
/// raw strings so permission identities are typed, compared by value, and self-documenting.
/// Implicitly converts to <see cref="string"/> for the claim/API boundary.
/// </summary>
public record PermissionName
{
    public string Value { get; }

    public PermissionName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Permission name cannot be empty or whitespace.", nameof(value));

        Value = value;
    }

    public static implicit operator string(PermissionName name) => name.Value;

    public override string ToString() => Value;
}

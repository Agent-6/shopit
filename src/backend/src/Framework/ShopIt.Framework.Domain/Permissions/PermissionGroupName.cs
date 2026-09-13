namespace ShopIt.Framework.Domain.Permissions;

/// <summary>
/// Value object representing a permission group key (e.g. <c>UserManagement</c>). Used in
/// place of raw strings so groups are typed and compared by value.
/// </summary>
public record PermissionGroupName
{
    public string Value { get; }

    public PermissionGroupName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Permission group name cannot be empty or whitespace.", nameof(value));

        Value = value;
    }

    public static implicit operator string(PermissionGroupName name) => name.Value;

    public override string ToString() => Value;
}

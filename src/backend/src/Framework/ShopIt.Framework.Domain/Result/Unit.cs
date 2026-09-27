namespace ShopIt.Framework.Domain.Result;

/// <summary>
/// Represents a type with a single value, used to represent void operations in generic Result types.
/// </summary>
public readonly struct Unit : IEquatable<Unit>, IComparable<Unit>, IComparable
{
    public static readonly Unit Value = new();

    public bool Equals(Unit other) => true;

    public override bool Equals(object? obj) => obj is Unit;

    public override int GetHashCode() => 0;

    public override string ToString() => "()";

    public int CompareTo(Unit other) => 0;

    public int CompareTo(object? obj) => 0;

    public static bool operator ==(Unit left, Unit right) => true;

    public static bool operator !=(Unit left, Unit right) => false;
}

namespace HEAL.HeuristicLib.Genotypes.SymbolicExpressions;

public sealed class Series<T> : IEquatable<Series<T>>
{
    private readonly T[] values;
    private readonly int hashCode;

    private Series(T[] values, bool takeOwnership, string? name)
    {
        this.values = takeOwnership ? values : values.ToArray();
        Name = name;
        hashCode = CalculateHashCode();
    }

    public string? Name { get; }
    public int Count => values.Length;
    public ReadOnlySpan<T> Values => values;

    public static Series<T> Create(IEnumerable<T> values, string? name = null) =>
        new(values.ToArray(), takeOwnership: true, name);

    public static Series<T> FromOwnedArray(T[] values, string? name = null) =>
        new(values, takeOwnership: true, name);

    public bool Equals(Series<T>? other)
    {
        return other is not null
               && (ReferenceEquals(this, other)
                   || (StringComparer.Ordinal.Equals(Name, other.Name)
                       && values.SequenceEqual(other.values)));
    }

    public override bool Equals(object? obj) => obj is Series<T> other && Equals(other);

    public override int GetHashCode() => hashCode;

    public static bool operator ==(Series<T>? left, Series<T>? right) => Equals(left, right);

    public static bool operator !=(Series<T>? left, Series<T>? right) => !Equals(left, right);

    private int CalculateHashCode()
    {
        var hash = new HashCode();
        hash.Add(Name, StringComparer.Ordinal);
        foreach (var value in values)
        {
            hash.Add(value);
        }

        return hash.ToHashCode();
    }
}

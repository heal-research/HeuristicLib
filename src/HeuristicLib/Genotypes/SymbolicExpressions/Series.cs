namespace HEAL.HeuristicLib.Genotypes.SymbolicExpressions;

public sealed class Series<T> : IEquatable<Series<T>>
{
    private readonly T[] values;
    private readonly int hashCode;

    private Series(T[] values, bool takeOwnership)
    {
        this.values = takeOwnership ? values : values.ToArray();
        hashCode = CalculateHashCode();
    }

    public int Count => values.Length;
    public ReadOnlySpan<T> Values => values;

    public static Series<T> Create(IEnumerable<T> values) =>
      new(values.ToArray(), takeOwnership: true);

    public static Series<T> FromOwnedArray(T[] values) =>
      new(values, takeOwnership: true);

    public bool Equals(Series<T>? other)
    {
        return other is not null
               && (ReferenceEquals(this, other)
                   || values.SequenceEqual(other.values));
    }

    public override bool Equals(object? obj) => obj is Series<T> other && Equals(other);

    public override int GetHashCode() => hashCode;

    public static bool operator ==(Series<T>? left, Series<T>? right) => Equals(left, right);

    public static bool operator !=(Series<T>? left, Series<T>? right) => !Equals(left, right);

    private int CalculateHashCode()
    {
        var hash = new HashCode();
        foreach (var value in values)
        {
            hash.Add(value);
        }

        return hash.ToHashCode();
    }
}

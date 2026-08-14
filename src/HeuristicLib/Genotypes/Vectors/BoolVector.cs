using System.Runtime.CompilerServices;

namespace HEAL.HeuristicLib.Genotypes.Vectors;

[CollectionBuilder(typeof(BoolVectorBuilder), nameof(BoolVectorBuilder.Create))]
public sealed class BoolVector : Vector<bool>, IEquatable<BoolVector>
{
    public BoolVector(params ImmutableArray<bool> elements)
        : base(elements) { }

    public BoolVector(IEnumerable<bool> elements)
        : base(elements) { }

    public static implicit operator BoolVector(bool value) => new(value);

    public static BoolVector Create(params ImmutableArray<bool> elements) => new(elements);

    public static BoolVector Create(IEnumerable<bool> elements) => new(elements);

    /// <summary>
    /// Creates a vector backed by <paramref name="elements"/> without copying it.
    /// The caller transfers ownership of the array and must not mutate it after this method returns.
    /// </summary>
    public static BoolVector FromOwnedArray(bool[] elements) => new(TakeOwnership(elements));

    public static BoolVector And(BoolVector a, BoolVector b)
    {
        var length = BroadcastLength(a, b);
        var result = new bool[length];

        for (var i = 0; i < length; i++)
        {
            var aValue = a.Count == 1 ? a[0] : a[i];
            var bValue = b.Count == 1 ? b[0] : b[i];
            result[i] = aValue && bValue;
        }

        return FromOwnedArray(result);
    }

    public static BoolVector Or(BoolVector a, BoolVector b)
    {
        var length = BroadcastLength(a, b);
        var result = new bool[length];

        for (var i = 0; i < length; i++)
        {
            var aValue = a.Count == 1 ? a[0] : a[i];
            var bValue = b.Count == 1 ? b[0] : b[i];
            result[i] = aValue || bValue;
        }

        return FromOwnedArray(result);
    }

    public static BoolVector Xor(BoolVector a, BoolVector b)
    {
        var length = BroadcastLength(a, b);
        var result = new bool[length];

        for (var i = 0; i < length; i++)
        {
            var aValue = a.Count == 1 ? a[0] : a[i];
            var bValue = b.Count == 1 ? b[0] : b[i];
            result[i] = aValue ^ bValue;
        }

        return FromOwnedArray(result);
    }

    public static BoolVector Not(BoolVector a)
    {
        var result = new bool[a.Count];

        for (var i = 0; i < a.Count; i++)
        {
            result[i] = !a[i];
        }

        return FromOwnedArray(result);
    }

    public static BoolVector operator &(BoolVector a, BoolVector b) => And(a, b);
    public static BoolVector operator |(BoolVector a, BoolVector b) => Or(a, b);
    public static BoolVector operator ^(BoolVector a, BoolVector b) => Xor(a, b);
    public static BoolVector operator !(BoolVector a) => Not(a);

    public bool All() => !Elements.AsSpan().Contains(false);

    public bool Any() => Elements.AsSpan().Contains(true);

    public int TrueCount() => Elements.AsSpan().Count(true);

    public bool Equals(BoolVector? other)
    {
        if (other is null)
            return false;

        return ReferenceEquals(this, other) || HasSameElements(other);
    }

    public override bool Equals(object? obj) => obj is BoolVector other && Equals(other);

    public override int GetHashCode() => GetElementsHashCode();

    public static bool operator ==(BoolVector? a, BoolVector? b)
    {
        if (ReferenceEquals(a, b))
            return true;

        if (a is null || b is null)
            return false;

        return a.Equals(b);
    }

    public static bool operator !=(BoolVector? a, BoolVector? b) => !(a == b);
}

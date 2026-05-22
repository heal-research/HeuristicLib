using System.Collections;
using System.Runtime.CompilerServices;

namespace HEAL.HeuristicLib.Genotypes.Vectors;

[CollectionBuilder(typeof(BoolVectorBuilder), nameof(BoolVectorBuilder.Create))]
public sealed class BoolVector : IReadOnlyList<bool>, IEquatable<BoolVector>
{
    private readonly bool[] elements;

    public BoolVector(params IEnumerable<bool> elements) : this(elements.ToArray(), takeOwnership: true) { }

    private BoolVector(bool[] elements, bool takeOwnership)
      => this.elements = takeOwnership ? elements : elements.ToArray();

    public static implicit operator BoolVector(bool value) => new(value);

    public static BoolVector Create(params bool[] elements) => new(elements, takeOwnership: false);

    public static BoolVector Create(IEnumerable<bool> elements) => new(elements);

    /// <summary>
    /// Creates a vector backed by <paramref name="elements"/> without copying it.
    /// The caller transfers ownership of the array and must not mutate it after this method returns.
    /// </summary>
    public static BoolVector FromOwnedArray(bool[] elements) => new(elements, takeOwnership: true);

    public bool this[int index] => elements[index];

    public IEnumerator<bool> GetEnumerator() => ((IEnumerable<bool>)elements).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => elements.GetEnumerator();

    public int Count => elements.Length;

    public bool Contains(bool value) => elements.Contains(value);

    public static bool AreCompatible(BoolVector a, BoolVector b) => a.Count == b.Count || a.Count == 1 || b.Count == 1;

    public static int BroadcastLength(BoolVector a, BoolVector b) => Math.Max(a.Count, b.Count);

    public static BoolVector And(BoolVector a, BoolVector b)
    {
        if (!AreCompatible(a, b))
        {
            throw new ArgumentException("Vectors must be compatible for logical operations");
        }

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
        if (!AreCompatible(a, b))
        {
            throw new ArgumentException("Vectors must be compatible for logical operations");
        }

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
        if (!AreCompatible(a, b))
        {
            throw new ArgumentException("Vectors must be compatible for logical operations");
        }

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

    public bool All() => elements.All(x => x);

    public bool Any() => elements.Any(x => x);

    public int TrueCount() => elements.Count(x => x);

    public bool Equals(BoolVector? other)
    {
        if (other is null)
        {
            return false;
        }

        return ReferenceEquals(this, other)
               || elements.SequenceEqual(other.elements);
    }

    public override bool Equals(object? obj) => obj is BoolVector other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var element in elements)
        {
            hash.Add(element);
        }

        return hash.ToHashCode();
    }

    public static bool operator ==(BoolVector? a, BoolVector? b)
    {
        if (ReferenceEquals(a, b))
        {
            return true;
        }

        if (a is null || b is null)
        {
            return false;
        }

        return a.Equals(b);
    }

    public static bool operator !=(BoolVector? a, BoolVector? b) => !(a == b);

    public override string ToString() => $"[{string.Join(", ", elements.Select(b => b ? "True" : "False"))}]";
}

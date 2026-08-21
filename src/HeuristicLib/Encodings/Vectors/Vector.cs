using System.Runtime.InteropServices;

namespace HEAL.HeuristicLib.Encodings.Vectors;

public abstract class Vector
{
    public abstract int Count { get; }

    public static bool AreBroadcastable(Vector a, Vector b) => a.Count == b.Count || a.Count == 1 || b.Count == 1;

    public static bool AreBroadcastable(Vector vector, params ReadOnlySpan<Vector> others) =>
        TryGetBroadcastLength(vector, others, out _);

    public static bool AreBroadcastableTo(int length, params ReadOnlySpan<Vector> vectors)
    {
        if (length < 0)
            return false;

        foreach (var vector in vectors)
        {
            if (vector.Count != 1 && vector.Count != length)
                return false;
        }

        return true;
    }

    public static int BroadcastLength(Vector a, Vector b)
    {
        if (!AreBroadcastable(a, b))
            throw new ArgumentException("Vectors must be broadcastable.");

        return a.Count == 1 ? b.Count : a.Count;
    }

    public static int BroadcastLength(Vector vector, params ReadOnlySpan<Vector> others)
    {
        return TryGetBroadcastLength(vector, others, out var length)
            ? length
            : throw new ArgumentException("Vectors must be broadcastable.");
    }

    public static bool TryGetBroadcastLength(Vector vector, ReadOnlySpan<Vector> others, out int length)
    {
        length = vector.Count;
        foreach (var other in others)
        {
            if (other.Count == 1)
                continue;
            if (length == 1)
                length = other.Count;
            else if (other.Count != length)
            {
                length = 0;
                return false;
            }
        }

        return true;
    }
}

public abstract class Vector<T> : Vector, IReadOnlyList<T>
{
    protected readonly ImmutableArray<T> Elements;

    protected Vector(params ImmutableArray<T> elements)
    {
        Elements = elements.IsDefault ? [] : elements;
    }

    protected Vector(IEnumerable<T> elements)
        : this(elements.ToImmutableArray())
    {
    }

    protected static ImmutableArray<T> TakeOwnership(T[] elements) =>
        ImmutableCollectionsMarshal.AsImmutableArray(elements);

    public sealed override int Count => Elements.Length;

    public T this[int index] => Elements[index];

    public T this[Index index] => Elements[index];

    public bool Contains(T value) => Elements.Contains(value);

    public bool All(Predicate<T> predicate)
    {
        foreach (var element in Elements)
        {
            if (!predicate(element))
                return false;
        }

        return true;
    }

    public ReadOnlySpan<T>.Enumerator GetEnumerator() => Elements.AsSpan().GetEnumerator();

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => ((IEnumerable<T>)Elements).GetEnumerator();

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => ((System.Collections.IEnumerable)Elements).GetEnumerator();

    protected bool HasSameElements(Vector<T> other) => Elements.AsSpan().SequenceEqual(other.Elements.AsSpan());

    protected int GetElementsHashCode()
    {
        var hash = new HashCode();
        foreach (var element in Elements)
        {
            hash.Add(element);
        }

        return hash.ToHashCode();
    }

    public override string ToString() => $"[{string.Join(", ", Elements)}]";
}

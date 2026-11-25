using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace HEAL.HeuristicLib.Collections;

/// <summary>
/// An immutable array with ordered structural equality. A type holding a <see cref="ValueArray{T}"/> compares its
/// elements rather than the underlying array reference, so records need no additional equality implementation.
/// </summary>
/// <remarks>
/// The default value is indistinguishable from an empty array: it has no elements, enumerates as empty, and compares
/// equal to an explicitly constructed empty array. The hash code is computed on demand and never cached.
/// </remarks>
[CollectionBuilder(typeof(ValueArray), nameof(ValueArray.Create))]
public readonly struct ValueArray<T> : IEquatable<ValueArray<T>>, IReadOnlyList<T>
{
    private readonly ImmutableArray<T> items;

    public ValueArray(ImmutableArray<T> items)
    {
        this.items = items;
    }

    public static ValueArray<T> Empty => new(ImmutableArray<T>.Empty);

    private ImmutableArray<T> Items => items.IsDefault ? ImmutableArray<T>.Empty : items;

    public int Count => Items.Length;

    public bool IsEmpty => Items.IsEmpty;

    public T this[int index] => Items[index];

    public T this[Index index] => Items[index];

    public ReadOnlySpan<T> AsSpan() => Items.AsSpan();

    public ImmutableArray<T> AsImmutableArray() => Items;

    public ImmutableArray<T>.Enumerator GetEnumerator() => Items.GetEnumerator();

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => ((IEnumerable<T>)Items).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Items).GetEnumerator();

    public static implicit operator ValueArray<T>(ImmutableArray<T> items) => new(items);

    public bool Equals(ValueArray<T> other) => AsSpan().SequenceEqual(other.AsSpan(), EqualityComparer<T>.Default);

    public override bool Equals(object? obj) => obj is ValueArray<T> other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        var span = AsSpan();
        hash.Add(span.Length);
        foreach (var item in span)
        {
            hash.Add(item);
        }

        return hash.ToHashCode();
    }

    public static bool operator ==(ValueArray<T> left, ValueArray<T> right) => left.Equals(right);

    public static bool operator !=(ValueArray<T> left, ValueArray<T> right) => !left.Equals(right);

    public override string ToString() => $"[{string.Join(", ", Items)}]";
}

public static class ValueArray
{
    /// <summary>
    /// Creates a value array from the given elements. An array or span binds to <paramref name="items"/> directly and
    /// supplies its elements; every other single argument, including a list, an immutable array or a string, becomes
    /// one element. Use <see cref="ValueArrayExtensions.ToValueArray{T}"/> to snapshot an arbitrary sequence.
    /// </summary>
    public static ValueArray<T> Create<T>(params ReadOnlySpan<T> items) => new(ImmutableArray.Create(items));

    /// <summary>
    /// Creates a value array backed by <paramref name="items"/> without copying it.
    /// The caller transfers ownership of the array and must not mutate it after this method returns.
    /// </summary>
    public static ValueArray<T> FromOwnedArray<T>(T[] items) => new(ImmutableCollectionsMarshal.AsImmutableArray(items));
}

public static class ValueArrayExtensions
{
    extension<T>(IEnumerable<T> items)
    {
        /// <summary>
        /// Snapshots <paramref name="items"/> into a value array. An input that is already a value array or an
        /// immutable array is not copied.
        /// </summary>
        public ValueArray<T> ToValueArray() =>
            items is ValueArray<T> valueArray ? valueArray : new(items.ToImmutableArray());
    }
}

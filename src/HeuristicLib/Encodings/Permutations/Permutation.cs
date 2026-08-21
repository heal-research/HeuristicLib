using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Encodings.Vectors;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Encodings.Permutations;

[CollectionBuilder(typeof(PermutationBuilder), nameof(PermutationBuilder.Create))]
public sealed class Permutation : Vector<int>, IEquatable<Permutation>
{
    public Permutation(params ImmutableArray<int> elements)
        : base(elements)
    {
        ValidatePermutation();
    }

    public Permutation(IEnumerable<int> elements)
        : base(elements)
    {
        ValidatePermutation();
    }

    public static Permutation Create(params ImmutableArray<int> elements) => new(elements);

    public static Permutation Create(IEnumerable<int> elements) => new(elements);

    /// <summary>
    /// Creates a permutation backed by <paramref name="elements"/> without copying it.
    /// The caller transfers ownership of the array and must not mutate it after this method returns.
    /// </summary>
    public static Permutation FromOwnedArray(int[] elements) => new(TakeOwnership(elements));

    private void ValidatePermutation()
    {
        if (!IsValidPermutation(Elements.AsSpan()))
            throw new ArgumentException("The provided elements do not form a valid permutation.");
    }

    private static bool IsValidPermutation(ReadOnlySpan<int> values)
    {
        Span<bool> seen = stackalloc bool[values.Length];
        seen.Clear();

        foreach (var value in values)
        {
            if (value < 0 || value >= values.Length || seen[value])
            {
                return false;
            }

            seen[value] = true;
        }

        return true;
    }

    public static Permutation CreateRandom(int length, IRandomNumberGenerator rng)
      => rng.NextPermutation(length);

    public static Permutation Range(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        var elements = new int[count];
        for (var i = 0; i < elements.Length; i++)
            elements[i] = i;

        return FromOwnedArray(elements);
    }

    public Permutation Swap(int index1, int index2)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index1);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index1, Count);
        ArgumentOutOfRangeException.ThrowIfNegative(index2);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index2, Count);

        if (index1 == index2)
        {
            return this;
        }

        var newElements = Elements.ToArray();
        (newElements[index1], newElements[index2]) = (newElements[index2], newElements[index1]);
        return FromOwnedArray(newElements);
    }

    public Permutation Invert(int start, int end)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(start);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(start, Count);
        ArgumentOutOfRangeException.ThrowIfLessThan(end, start);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(end, Count);

        if (start == end)
        {
            return this;
        }

        var newElements = Elements.ToArray();
        Array.Reverse(newElements, start, end - start + 1);
        return FromOwnedArray(newElements);
    }

    public bool Equals(Permutation? other) =>
        other is not null && (ReferenceEquals(this, other) || HasSameElements(other));

    public override bool Equals(object? obj) => obj is Permutation other && Equals(other);

    public override int GetHashCode() => GetElementsHashCode();

    public static bool operator ==(Permutation? a, Permutation? b) => Equals(a, b);
    public static bool operator !=(Permutation? a, Permutation? b) => !Equals(a, b);
}

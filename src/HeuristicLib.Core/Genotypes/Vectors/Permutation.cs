using System.Collections;
using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Genotypes.Vectors;

[CollectionBuilder(typeof(PermutationBuilder), nameof(PermutationBuilder.Create))]
public sealed class Permutation : IReadOnlyList<int>, IEquatable<Permutation>
{
  private readonly int[] elements;

  public Permutation(params IEnumerable<int> elements) : this(elements.ToArray(), takeOwnership: true) { }

  private Permutation(int[] elements, bool takeOwnership)
  {
    if (!IsValidPermutation(elements)) {
      throw new ArgumentException("The provided elements do not form a valid permutation.");
    }

    this.elements = takeOwnership ? elements : elements.ToArray();
  }

  public static Permutation Create(params int[] elements) => new(elements, takeOwnership: false);

  public static Permutation Create(IEnumerable<int> elements) => new(elements);

  /// <summary>
  /// Creates a permutation backed by <paramref name="elements"/> without copying it.
  /// The caller transfers ownership of the array and must not mutate it after this method returns.
  /// </summary>
  public static Permutation FromOwnedArray(int[] elements) => new(elements, takeOwnership: true);

  private static bool IsValidPermutation(ReadOnlySpan<int> values)
  {
    Span<bool> seen = stackalloc bool[values.Length];
    seen.Clear();

    foreach (var value in values) {
      if (value < 0 || value >= values.Length || seen[value]) {
        return false;
      }

      seen[value] = true;
    }

    return true;
  }

  public int this[int index] => elements[index];

  public int this[Index index] => elements[index];

  public IEnumerator<int> GetEnumerator() => ((IEnumerable<int>)elements).GetEnumerator();

  IEnumerator IEnumerable.GetEnumerator() => elements.GetEnumerator();

  public static Permutation CreateRandom(int length, IRandomNumberGenerator rng)
    => rng.NextPermutation(length);

  public static Permutation SwapRandomElements(Permutation permutation, IRandomNumberGenerator rng)
    => rng.Swap(permutation);

  public static Permutation Range(int count) => FromOwnedArray(Enumerable.Range(0, count).ToArray());

  public int Count => elements.Length;

  public bool Contains(int value) => elements.Contains(value);

  public bool Equals(Permutation? other) =>
    other is not null && elements.SequenceEqual(other.elements);

  public override bool Equals(object? obj) => obj is Permutation other && Equals(other);

  public override int GetHashCode()
  {
    var hash = new HashCode();
    foreach (var value in elements) {
      hash.Add(value);
    }

    return hash.ToHashCode();
  }

  public static bool operator ==(Permutation? a, Permutation? b) => Equals(a, b);
  public static bool operator !=(Permutation? a, Permutation? b) => !Equals(a, b);
}

using System.Collections;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Genotypes.Vectors;

public sealed class Permutation : IReadOnlyList<int>, IEquatable<Permutation>
{
  private readonly ReadOnlyMemory<int> memory;

  public Permutation(params IEnumerable<int> elements)
  {
    memory = elements.ToArray();
    if (!IsValidPermutation(memory.Span)) {
      throw new ArgumentException("The provided elements do not form a valid permutation.");
    }
  }

  private Permutation(ReadOnlyMemory<int> memory, bool takeOwnership)
  {
    if (!IsValidPermutation(memory.Span)) {
      throw new ArgumentException("The provided memory does not form a valid permutation.");
    }

    this.memory = takeOwnership ? memory : memory.ToArray();
  }

  public static Permutation FromMemory(ReadOnlyMemory<int> memory) => new(memory, true);

  public ReadOnlySpan<int> Span => memory.Span;

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

  public static implicit operator Permutation(int[] elements) => new(elements);

  public int this[int index] => Span[index];

  public int this[Index index] => Span[index];

  public IEnumerator<int> GetEnumerator() => new Enumerator(memory);

  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

  public struct Enumerator(ReadOnlyMemory<int> memory) : IEnumerator<int>
  {
    private int index = -1;

    public int Current => memory.Span[index];
    object IEnumerator.Current => Current;

    public bool MoveNext() => ++index < memory.Length;
    public void Reset() => index = -1;
    public void Dispose() { }
  }

  public static Permutation CreateRandom(int length, IRandomNumberGenerator rng)
    => rng.NextPermutation(length);

  public static Permutation SwapRandomElements(Permutation permutation, IRandomNumberGenerator rng)
    => rng.Swap(permutation);

  public static Permutation Range(int count) => FromMemory(Enumerable.Range(0, count).ToArray());

  public int Count => memory.Length;

  public bool Contains(int value) => memory.Span.Contains(value);

  public bool Equals(Permutation? other) =>
    other is not null && Span.SequenceEqual(other.Span);

  public override bool Equals(object? obj) => obj is Permutation other && Equals(other);

  public override int GetHashCode()
  {
    var hash = new HashCode();
    foreach (var value in Span) {
      hash.Add(value);
    }

    return hash.ToHashCode();
  }

  public static bool operator ==(Permutation? a, Permutation? b) => Equals(a, b);
  public static bool operator !=(Permutation? a, Permutation? b) => !Equals(a, b);
}

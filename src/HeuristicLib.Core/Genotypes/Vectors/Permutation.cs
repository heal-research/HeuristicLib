using System.Collections;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Genotypes.Vectors;

public sealed record Permutation : IReadOnlyList<int>
{
  private readonly ReadOnlyMemory<int> memory;

  public Permutation(IEnumerable<int> elements)
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

  public ReadOnlySpan<int> Span => memory.Span;

  public int this[Index index] => Span[index];

  public bool Equals(Permutation? other) => other != null && memory.Equals(other.memory) && memory.Span.SequenceEqual(other.memory.Span);

  public int this[int index] => Span[index];

  public IEnumerator<int> GetEnumerator() => new Enumerator(memory);

  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

  public int Count => memory.Length;

  public static Permutation FromMemory(ReadOnlyMemory<int> memory) => new(memory, true);

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

  public static implicit operator Permutation(int[] elements) => new((IEnumerable<int>)elements);

  public static Permutation CreateRandom(int length, IRandomNumberGenerator rng)
  {
    var elements = Enumerable.Range(0, length).ToArray();
    for (var i = elements.Length - 1; i > 0; i--) {
      var j = rng.Integer(i + 1);
      (elements[i], elements[j]) = (elements[j], elements[i]);
    }

    return FromMemory(elements);
  }

  public static Permutation SwapRandomElements(Permutation permutation, IRandomNumberGenerator rng)
  {
    var length = permutation.Count;
    var index1 = rng.Integer(length);
    var index2 = rng.Integer(length);

    var newElements = permutation.memory.ToArray();
    (newElements[index1], newElements[index2]) = (newElements[index2], newElements[index1]);

    return FromMemory(newElements);
  }

  public static Permutation Range(int count) => FromMemory(Enumerable.Range(0, count).ToArray());

  public bool Contains(int value) => Span.Contains(value);

  public override int GetHashCode() => memory.GetHashCode();

  public struct Enumerator(ReadOnlyMemory<int> memory) : IEnumerator<int>
  {
    private int index = -1;

    public int Current => memory.Span[index];
    object IEnumerator.Current => Current;

    public bool MoveNext() => ++index < memory.Length;
    public void Reset() => index = -1;
    public void Dispose() {}
  }
}

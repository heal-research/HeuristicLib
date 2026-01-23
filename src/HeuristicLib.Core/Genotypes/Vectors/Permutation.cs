using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Genotypes.Vectors;

public sealed record Permutation : IReadOnlyList<int> {
  private readonly ReadOnlyMemory<int> memory;

  public Permutation(IEnumerable<int> elements) {
    memory = elements.ToArray();
    if (!IsValidPermutation(memory.Span)) throw new ArgumentException("The provided elements do not form a valid permutation.");
  }

  private Permutation(ReadOnlyMemory<int> memory, bool takeOwnership) {
    if (!IsValidPermutation(memory.Span)) throw new ArgumentException("The provided memory does not form a valid permutation.");
    this.memory = takeOwnership ? memory : memory.ToArray();
  }

  public static Permutation FromMemory(ReadOnlyMemory<int> memory) {
    return new Permutation(memory, true);
  }

  public ReadOnlySpan<int> Span => memory.Span;

  private static bool IsValidPermutation(ReadOnlySpan<int> values) {
    Span<bool> seen = stackalloc bool[values.Length];
    seen.Clear();

    foreach (int value in values) {
      if (value < 0 || value >= values.Length || seen[value]) {
        return false;
      }

      seen[value] = true;
    }

    return true;
  }

  public static implicit operator Permutation(int[] elements) {
    return new Permutation((IEnumerable<int>)elements);
  }

  public int this[int index] => Span[index];

  public int this[Index index] => Span[index];

  public IEnumerator<int> GetEnumerator() => new Enumerator(memory);

  System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => this.GetEnumerator();

  public struct Enumerator(ReadOnlyMemory<int> memory) : IEnumerator<int> {
    private int index = -1;

    public int Current => memory.Span[index];
    object System.Collections.IEnumerator.Current => Current;

    public bool MoveNext() => ++index < memory.Length;
    public void Reset() => index = -1;
    public void Dispose() { }
  }

  public static Permutation CreateRandom(int length, IRandomNumberGenerator rng) {
    int[] elements = Enumerable.Range(0, length).ToArray();
    for (int i = elements.Length - 1; i > 0; i--) {
      int j = rng.NextInt(i + 1);
      (elements[i], elements[j]) = (elements[j], elements[i]);
    }

    return FromMemory(elements);
  }

  public static Permutation SwapRandomElements(Permutation permutation, IRandomNumberGenerator rng) {
    int length = permutation.Count;
    int index1 = rng.NextInt(length);
    int index2 = rng.NextInt(length);

    int[] newElements = permutation.memory.ToArray();
    (newElements[index1], newElements[index2]) = (newElements[index2], newElements[index1]);
    return FromMemory(newElements);
  }

  public static Permutation Range(int count) {
    return FromMemory(Enumerable.Range(0, count).ToArray());
  }

  public int Count => memory.Length;

  public bool Contains(int value) => memory.Span.Contains(value); 

  public bool Equals(Permutation? other) {
    return other != null && memory.Equals(other.memory) && Span.SequenceEqual(other.memory.Span);
  }

  public override int GetHashCode() {
    return memory.GetHashCode();
  }
}

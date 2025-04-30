using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Genotypes;

public sealed class Permutation : IReadOnlyList<int>, IEquatable<Permutation> {
  private readonly int[] elements;

  public Permutation(IEnumerable<int> elements) {
    this.elements = elements.ToArray();
  }
  
  public static implicit operator Permutation(int[] elements) => new(elements);

  public int this[int index] => elements[index];

  public int this[Index index] => elements[index];

  public IEnumerator<int> GetEnumerator() => ((IEnumerable<int>)elements).GetEnumerator();

  System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => elements.GetEnumerator();

  public static Permutation CreateRandom(int length, IRandomNumberGenerator rng) {
    int[] elements = Enumerable.Range(0, length).ToArray();
    for (int i = elements.Length - 1; i > 0; i--) {
      int j = rng.Integer(i + 1);
      (elements[i], elements[j]) = (elements[j], elements[i]);
    }
    return new Permutation(elements);
  }

  public static Permutation OrderCrossover(Permutation parent1, Permutation parent2, IRandomNumberGenerator rng) {
    int length = parent1.elements.Length;
    int start = rng.Integer(length);
    int end = rng.Integer(start, length);
    var childElements = new int[length];
    Array.Fill(childElements, -1);

    for (int i = start; i <= end; i++) {
      childElements[i] = parent1.elements[i];
    }

    int currentIndex = 0;
    for (int i = 0; i < length; i++) {
      if (!childElements.Contains(parent2.elements[i])) {
        while (childElements[currentIndex] != -1) {
          currentIndex++;
        }
        childElements[currentIndex] = parent2.elements[i];
      }
    }

    return new Permutation(childElements);
  }

  public static Permutation SwapRandomElements(Permutation permutation, IRandomNumberGenerator rng) {
    int length = permutation.elements.Length;
    int index1 = rng.Integer(length);
    int index2 = rng.Integer(length);
    var newElements = (int[])permutation.elements.Clone();
    (newElements[index1], newElements[index2]) = (newElements[index2], newElements[index1]);
    return new Permutation(newElements);
  }

  public static Permutation Range(int count) {
    return new Permutation(Enumerable.Range(0, count));
  }
  
  public int Count => elements.Length;

  public bool Contains(int value) => elements.Contains(value);

  public bool Equals(Permutation? other) {
    if (other is null) return false;
    if (ReferenceEquals(this, other)) return true;
    if (Count != other.Count) return false;
    return elements.SequenceEqual(other.elements);
  }

  public override bool Equals(object? obj) {
    if (obj is null) return false;
    if (ReferenceEquals(this, obj)) return true;
    return obj is Permutation other && Equals(other);
  }

  public override int GetHashCode() {
    var hashCode = new HashCode();
    foreach (var item in elements) {
      hashCode.Add(item);
    }
    return hashCode.ToHashCode();
  }

  public static bool operator ==(Permutation? left, Permutation? right) {
    if (left is null) return right is null;
    return left.Equals(right);
  }

  public static bool operator !=(Permutation? left, Permutation? right) {
    return !(left == right);
  }
}

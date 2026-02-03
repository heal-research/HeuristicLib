using System.Collections;

namespace HEAL.HeuristicLib.Genotypes.Vectors;

public sealed class IntegerVector(params IEnumerable<int> elements) : IReadOnlyList<int>, IEquatable<IntegerVector>
{
  private readonly int[] elements = elements.ToArray();

  public int this[Index index] => elements[index];

  public bool Equals(IntegerVector? other) => other is not null && (ReferenceEquals(this, other) || elements.SequenceEqual(other.elements));

  public int Count => elements.Length;

  public int this[int index] => elements[index];

  public IEnumerator<int> GetEnumerator() => ((IEnumerable<int>)elements).GetEnumerator();

  IEnumerator IEnumerable.GetEnumerator() => elements.GetEnumerator();

  public static implicit operator IntegerVector(int value) => new(value);

  public static implicit operator IntegerVector(int[] values) => new(values);

  public override bool Equals(object? obj) => obj is IntegerVector other && Equals(other);

  public override int GetHashCode()
  {
    var hash = new HashCode();
    foreach (var element in elements) {
      hash.Add(element);
    }

    return hash.ToHashCode();
  }

  public static implicit operator RealVector(IntegerVector integerVector) => new(integerVector.elements.Select(i => (double)i));
}
